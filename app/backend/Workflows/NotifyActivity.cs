using Dapr.Workflow;

namespace MinimalApi.Workflows;

internal sealed record Unit
{
    public static Unit Instance { get; } = new();
}

internal sealed class NotifyActivity : WorkflowActivity<string, Unit>
{
    private readonly ILogger<NotifyActivity> _logger;

    public NotifyActivity(ILogger<NotifyActivity> logger)
    {
        this._logger = logger;
    }

    public override Task<Unit> RunAsync(WorkflowActivityContext context, string input)
    {
        this._logger.LogInformation(input);

        return Task.FromResult(Unit.Instance);
    }
}
