using System.Net.Http;
using AiComposer.Core.Agents;
using AiComposer.Core.Orchestration;
using AiComposer.Core.State;
using AiComposer.Core.Telemetry;

const string usage =
    """
    Usage: ai-composer --artifacts <path> --output <path> [--state-db <path>] [--trace-log <path>]
                       [--provider <deepseek|copilot> --prompt <text> [--role <name>] [--deepseek-model <name>] [--copilot-model <name>] [--deepseek-base-url <url>] [--dry-run] [--live-output] [--apply-files <path>] [--allow-overwrite]]

    Options:
      --artifacts           Path to markdown artifacts directory (required)
      --output              Path to write generated context files  (required)
      --state-db            Path to SQLite state store             (default: .state/orchestrator.db)
      --trace-log           Path to JSONL trace log file           (default: .state/traces.jsonl)
      --provider            External provider to invoke (deepseek or copilot)
      --prompt              Prompt passed to the spawned agent
      --role                Logical agent role label               (default: coder)
      --deepseek-model      DeepSeek model name                    (default: deepseek-v4-flash)
      --copilot-model       Copilot SDK model name                 (default: gpt-5)
      --deepseek-base-url   DeepSeek OpenAI-format base URL        (default: https://api.deepseek.com)
      --dry-run             Write request metadata without calling the provider
      --live-output         Stream provider output to the terminal as it is generated
      --apply-files         Apply coder-generated text files inside the target workspace
      --allow-overwrite     Permit overwriting existing files inside the apply-files workspace

    Environment:
      DEEPSEEK_API_KEY      Required when --provider deepseek is used without --dry-run
      Copilot SDK auth      Requires local `copilot login` for --provider copilot
    """;

string? artifactsDir = null;
string? outputDir = null;
string? provider = null;
string? prompt = null;
var stateDb = Path.Combine(".state", "orchestrator.db");
var traceLog = Path.Combine(".state", "traces.jsonl");
var role = "coder";
var deepSeekModel = "deepseek-v4-flash";
var copilotModel = "gpt-5.4";
var deepSeekBaseUrl = "https://api.deepseek.com";
var dryRun = false;
var liveOutput = false;
string? applyFilesWorkspace = null;
var allowOverwrite = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--artifacts": artifactsDir = ReadValue(args, ref i); break;
        case "--output": outputDir = ReadValue(args, ref i); break;
        case "--state-db": stateDb = ReadValue(args, ref i); break;
        case "--trace-log": traceLog = ReadValue(args, ref i); break;
        case "--provider": provider = ReadValue(args, ref i); break;
        case "--prompt": prompt = ReadValue(args, ref i); break;
        case "--role": role = ReadValue(args, ref i); break;
        case "--deepseek-model": deepSeekModel = ReadValue(args, ref i); break;
        case "--copilot-model": copilotModel = ReadValue(args, ref i); break;
        case "--deepseek-base-url": deepSeekBaseUrl = ReadValue(args, ref i); break;
        case "--dry-run": dryRun = true; break;
        case "--live-output":
        case "--live":
            liveOutput = true;
            break;
        case "--apply-files": applyFilesWorkspace = ReadValue(args, ref i); break;
        case "--allow-overwrite": allowOverwrite = true; break;
        default:
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                Console.Error.WriteLine(usage);
                return 1;
            }
            break;
    }
}

if (artifactsDir is null || outputDir is null)
{
    Console.Error.WriteLine(usage);
    return 1;
}

if (provider is not null
    && !provider.Equals("deepseek", StringComparison.OrdinalIgnoreCase)
    && !provider.Equals("copilot", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unsupported provider: {provider}");
    Console.Error.WriteLine(usage);
    return 1;
}

if (provider is not null && string.IsNullOrWhiteSpace(prompt))
{
    Console.Error.WriteLine("--prompt is required when --provider is specified.");
    Console.Error.WriteLine(usage);
    return 1;
}

if (applyFilesWorkspace is not null && provider is null)
{
    Console.Error.WriteLine("--apply-files requires --provider.");
    Console.Error.WriteLine(usage);
    return 1;
}

if (applyFilesWorkspace is not null && !role.Equals("coder", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("--apply-files is currently supported only with --role coder.");
    Console.Error.WriteLine(usage);
    return 1;
}

if (applyFilesWorkspace is not null)
{
    var applyRoot = Path.GetFullPath(applyFilesWorkspace);
    var outputRoot = Path.GetFullPath(outputDir);
    if (applyRoot.StartsWith(outputRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || outputRoot.StartsWith(applyRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(applyRoot, outputRoot, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--apply-files workspace must be outside the orchestrator output directory.");
        return 1;
    }
}

using var stateStore = new StateStore(Path.GetFullPath(stateDb));
var orchestrator = new Orchestrator(
    stateStore,
    new TraceLogger(Path.GetFullPath(traceLog)));

using var cancellationSource = new CancellationTokenSource();
ConsoleCancelEventHandler? cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    IReadOnlyList<string> tickets;
    if (provider is null)
    {
        tickets = orchestrator.ProcessTickets(
            Path.GetFullPath(artifactsDir),
            Path.GetFullPath(outputDir));
    }
    else
    {
        IAgentClient? agentClient = null;
        HttpClient? httpClient = null;

        if (!dryRun && provider.Equals("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Error.WriteLine("DEEPSEEK_API_KEY is required when invoking the DeepSeek provider.");
                return 1;
            }

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3),
            };
            agentClient = new DeepSeekChatCompletionClient(
                httpClient,
                new DeepSeekClientOptions
                {
                    ApiKey = apiKey,
                    BaseUrl = deepSeekBaseUrl,
                    Model = deepSeekModel,
                });
        }
        else if (!dryRun && provider.Equals("copilot", StringComparison.OrdinalIgnoreCase))
        {
            agentClient = new CopilotSdkAgentClient(Directory.GetCurrentDirectory());
        }

        try
        {
            IProgress<string>? liveProgress = liveOutput
                ? new Progress<string>(chunk => Console.Write(chunk))
                : null;
            tickets = await orchestrator.ProcessTicketsAsync(
                Path.GetFullPath(artifactsDir),
                Path.GetFullPath(outputDir),
                new AgentExecutionOptions
                {
                    Provider = provider,
                    Model = provider.Equals("copilot", StringComparison.OrdinalIgnoreCase)
                        ? copilotModel
                        : deepSeekModel,
                    Role = role,
                    Prompt = prompt!,
                    DryRun = dryRun,
                    LiveOutput = liveOutput,
                    ApplyFilesWorkspace = applyFilesWorkspace is null ? null : Path.GetFullPath(applyFilesWorkspace),
                    AllowOverwrite = allowOverwrite,
                },
                agentClient,
                liveProgress,
                cancellationSource.Token);
        }
        finally
        {
            httpClient?.Dispose();
        }
    }

    Console.WriteLine($"Processed {tickets.Count} ticket(s): {(tickets.Count > 0 ? string.Join(", ", tickets) : "none")}");
    return 0;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

static string ReadValue(string[] args, ref int index)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"Missing value for option {args[index]}");

    return args[++index];
}
