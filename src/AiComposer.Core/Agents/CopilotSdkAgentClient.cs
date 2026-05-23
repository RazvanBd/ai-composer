using System.Diagnostics;
using System.IO;
using System.Text;

namespace AiComposer.Core.Agents;

/// <summary>Copilot-backed agent client that delegates execution to the local Copilot CLI runtime.</summary>
public sealed class CopilotSdkAgentClient : IAgentClient
{
    private static readonly TimeSpan DefaultResponseTimeout = TimeSpan.FromMinutes(5);
    private readonly string _workingDirectory;

    /// <summary>Creates a Copilot-backed agent client for the given working directory.</summary>
    public CopilotSdkAgentClient(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc />
    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var combinedPrompt =
            $"""
            System instructions:
            {request.SystemPrompt}

            User instructions:
            {request.UserPrompt}
            """;

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(DefaultResponseTimeout);

        using var process = new Process
        {
            StartInfo = BuildStartInfo(
                model: string.IsNullOrWhiteSpace(request.Model) ? "gpt-5" : request.Model,
                prompt: combinedPrompt,
                streaming: progress is not null),
            EnableRaisingEvents = true,
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Failed to start the local `copilot` process.");

            process.StandardInput.Close();

            var stdoutTask = ReadStreamAsync(process.StandardOutput, stdout, progress, timeoutSource.Token);
            var stderrTask = ReadStreamAsync(process.StandardError, stderr, null, timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryTerminate(process);
                throw new InvalidOperationException(
                    $"Copilot CLI execution timed out after {DefaultResponseTimeout.TotalMinutes:0} minute(s).");
            }

            await Task.WhenAll(stdoutTask, stderrTask);

            var exitCode = process.ExitCode;
            var output = stdout.ToString().Trim();
            var error = stderr.ToString().Trim();

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(error)
                        ? $"Copilot CLI exited with code {exitCode}."
                        : $"Copilot CLI failed: {error}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(error)
                        ? "Copilot CLI completed without producing output."
                        : $"Copilot CLI completed without output. Stderr: {error}");
            }

            return new AgentExecutionResult
            {
                Provider = "copilot",
                ResponseId = Guid.NewGuid().ToString(),
                Model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-5" : request.Model,
                Role = request.Role,
                Content = output,
                FinishReason = "completed",
                PromptTokens = 0,
                CompletionTokens = 0,
                PromptCacheHitTokens = 0,
                PromptCacheMissTokens = 0,
                CostUsd = 0,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Copilot CLI execution was canceled.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Copilot execution failed. Make sure the local `copilot` CLI is installed and authenticated via `copilot login`.",
                ex);
        }
    }

    private ProcessStartInfo BuildStartInfo(string model, string prompt, bool streaming)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "copilot",
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(prompt);
        startInfo.ArgumentList.Add("--allow-all");
        startInfo.ArgumentList.Add("--model");
        startInfo.ArgumentList.Add(model);
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(_workingDirectory);
        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add("--no-ask-user");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");
        startInfo.ArgumentList.Add("--no-color");
        startInfo.ArgumentList.Add("--stream");
        startInfo.ArgumentList.Add(streaming ? "on" : "off");

        return startInfo;
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder buffer,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;

            buffer.AppendLine(line);
            progress?.Report(line + Environment.NewLine);
        }
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }
}
