using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace AiComposer.Core.Agents;

/// <summary>OpenAI-format DeepSeek client used to execute ticket-scoped agents.</summary>
public sealed class DeepSeekChatCompletionClient : IAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly DeepSeekClientOptions _options;

    /// <summary>Creates a provider client with the supplied HTTP transport and settings.</summary>
    public DeepSeekChatCompletionClient(HttpClient httpClient, DeepSeekClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model;
        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
        var useStreaming = progress is not null;

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new DeepSeekChatRequest
            {
                Model = model,
                Temperature = _options.Temperature,
                Stream = useStreaming,
                StreamOptions = useStreaming ? new DeepSeekStreamOptions { IncludeUsage = true } : null,
                ResponseFormat = request.ResponseFormat == "json_object"
                    ? new DeepSeekResponseFormat { Type = "json_object" }
                    : null,
                Messages =
                [
                    new DeepSeekMessage { Role = "system", Content = request.SystemPrompt },
                    new DeepSeekMessage { Role = "user", Content = request.UserPrompt },
                ],
            }),
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(
            message,
            useStreaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"DeepSeek request failed ({(int)response.StatusCode}): {errorBody}");
        }

        return useStreaming
            ? await ReadStreamingResponseAsync(response, model, request, progress!, cancellationToken)
            : await ReadBufferedResponseAsync(response, model, request, cancellationToken);
    }

    private static async Task<AgentExecutionResult> ReadBufferedResponseAsync(
        HttpResponseMessage response,
        string requestedModel,
        AgentExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = System.Text.Json.JsonSerializer.Deserialize(
            responseBody,
            DeepSeekJsonContext.Default.DeepSeekChatResponse)
            ?? throw new InvalidOperationException("DeepSeek response body was empty.");

        var choice = payload.Choices.FirstOrDefault()
            ?? throw new InvalidOperationException("DeepSeek response contained no choices.");
        var content = choice.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("DeepSeek response did not contain message content.");

        var usage = payload.Usage ?? new DeepSeekUsage();
        return BuildResult(payload.Id ?? string.Empty, payload.Model ?? requestedModel, request.Role, content, choice.FinishReason ?? string.Empty, usage);
    }

    private static async Task<AgentExecutionResult> ReadStreamingResponseAsync(
        HttpResponseMessage response,
        string requestedModel,
        AgentExecutionRequest request,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var contentBuilder = new StringBuilder();
        string responseId = string.Empty;
        var model = requestedModel;
        var finishReason = string.Empty;
        var usage = new DeepSeekUsage();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var payload = line["data:".Length..].Trim();
            if (payload == "[DONE]")
                break;

            var chunk = System.Text.Json.JsonSerializer.Deserialize(
                payload,
                DeepSeekJsonContext.Default.DeepSeekChatStreamChunk);
            if (chunk is null)
                continue;

            responseId = string.IsNullOrWhiteSpace(chunk.Id) ? responseId : chunk.Id;
            model = string.IsNullOrWhiteSpace(chunk.Model) ? model : chunk.Model;
            if (chunk.Usage is not null)
                usage = chunk.Usage;

            var choice = chunk.Choices.FirstOrDefault();
            if (choice?.Delta?.ReasoningContent is { Length: > 0 } reasoning)
                progress.Report(reasoning);

            if (choice?.Delta?.Content is { Length: > 0 } content)
            {
                contentBuilder.Append(content);
                progress.Report(content);
            }

            if (!string.IsNullOrWhiteSpace(choice?.FinishReason))
                finishReason = choice.FinishReason;
        }

        var finalContent = contentBuilder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(finalContent))
            throw new InvalidOperationException("DeepSeek streamed response did not contain message content.");

        return BuildResult(responseId, model, request.Role, finalContent, finishReason, usage);
    }

    private static AgentExecutionResult BuildResult(
        string responseId,
        string model,
        string role,
        string content,
        string finishReason,
        DeepSeekUsage usage)
        => new()
        {
            Provider = "deepseek",
            ResponseId = responseId,
            Model = model,
            Role = role,
            Content = content,
            FinishReason = finishReason,
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            PromptCacheHitTokens = usage.PromptCacheHitTokens,
            PromptCacheMissTokens = usage.PromptCacheMissTokens > 0
                ? usage.PromptCacheMissTokens
                : Math.Max(usage.PromptTokens - usage.PromptCacheHitTokens, 0),
            CostUsd = CalculateCostUsd(model, usage),
        };

    private static double CalculateCostUsd(string model, DeepSeekUsage usage)
    {
        var pricing = GetPricing(model);
        var cacheHitTokens = usage.PromptCacheHitTokens;
        var cacheMissTokens = usage.PromptCacheMissTokens > 0
            ? usage.PromptCacheMissTokens
            : Math.Max(usage.PromptTokens - cacheHitTokens, 0);

        var inputCost = (cacheHitTokens / 1_000_000d) * pricing.InputCacheHitUsdPerMillion
            + (cacheMissTokens / 1_000_000d) * pricing.InputCacheMissUsdPerMillion;
        var outputCost = (usage.CompletionTokens / 1_000_000d) * pricing.OutputUsdPerMillion;
        return Math.Round(inputCost + outputCost, 8, MidpointRounding.AwayFromZero);
    }

    private static DeepSeekPricing GetPricing(string model) =>
        model switch
        {
            "deepseek-v4-pro" => new DeepSeekPricing(0.003625, 0.435, 0.87),
            "deepseek-reasoner" => new DeepSeekPricing(0.0028, 0.14, 0.28),
            "deepseek-chat" => new DeepSeekPricing(0.0028, 0.14, 0.28),
            _ => new DeepSeekPricing(0.0028, 0.14, 0.28),
        };

    private sealed record DeepSeekPricing(
        double InputCacheHitUsdPerMillion,
        double InputCacheMissUsdPerMillion,
        double OutputUsdPerMillion);

    private sealed class DeepSeekChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<DeepSeekMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("stream_options")]
        public DeepSeekStreamOptions? StreamOptions { get; set; }

        [JsonPropertyName("response_format")]
        public DeepSeekResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class DeepSeekMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class DeepSeekStreamOptions
    {
        [JsonPropertyName("include_usage")]
        public bool IncludeUsage { get; set; }
    }

    private sealed class DeepSeekResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
    }
}

[JsonSerializable(typeof(DeepSeekChatResponse))]
[JsonSerializable(typeof(DeepSeekChatStreamChunk))]
internal sealed partial class DeepSeekJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

internal sealed class DeepSeekChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<DeepSeekChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public DeepSeekUsage? Usage { get; set; }
}

internal sealed class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public DeepSeekResponseMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class DeepSeekResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class DeepSeekUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("prompt_cache_hit_tokens")]
    public int PromptCacheHitTokens { get; set; }

    [JsonPropertyName("prompt_cache_miss_tokens")]
    public int PromptCacheMissTokens { get; set; }
}

internal sealed class DeepSeekChatStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<DeepSeekStreamChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public DeepSeekUsage? Usage { get; set; }
}

internal sealed class DeepSeekStreamChoice
{
    [JsonPropertyName("delta")]
    public DeepSeekStreamDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class DeepSeekStreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }
}
