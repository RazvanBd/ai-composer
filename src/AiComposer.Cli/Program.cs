using AiComposer.Core.Orchestration;
using AiComposer.Core.State;
using AiComposer.Core.Telemetry;

const string usage =
    """
    Usage: ai-composer --artifacts <path> --output <path> [--state-db <path>] [--trace-log <path>]

    Options:
      --artifacts   Path to markdown artifacts directory (required)
      --output      Path to write generated context files  (required)
      --state-db    Path to SQLite state store             (default: .state/orchestrator.db)
      --trace-log   Path to JSONL trace log file           (default: .state/traces.jsonl)
    """;

string? artifactsDir = null;
string? outputDir = null;
var stateDb = Path.Combine(".state", "orchestrator.db");
var traceLog = Path.Combine(".state", "traces.jsonl");

for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--artifacts": artifactsDir = args[++i]; break;
        case "--output":    outputDir    = args[++i]; break;
        case "--state-db":  stateDb      = args[++i]; break;
        case "--trace-log": traceLog     = args[++i]; break;
    }
}

if (artifactsDir is null || outputDir is null)
{
    Console.Error.WriteLine(usage);
    return 1;
}

var orchestrator = new Orchestrator(
    new StateStore(Path.GetFullPath(stateDb)),
    new TraceLogger(Path.GetFullPath(traceLog)));

var tickets = orchestrator.ProcessTickets(
    Path.GetFullPath(artifactsDir),
    Path.GetFullPath(outputDir));

Console.WriteLine($"Processed {tickets.Count} ticket(s): {(tickets.Count > 0 ? string.Join(", ", tickets) : "none")}");
return 0;
