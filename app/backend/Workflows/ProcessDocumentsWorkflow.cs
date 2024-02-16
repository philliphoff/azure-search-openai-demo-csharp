using Dapr.Workflow;

namespace MinimalApi.Workflows;

internal sealed record ProcessDocumentsRequest(string Path);

internal sealed class ProcessDocumentsWorkflow : Workflow<ProcessDocumentsRequest, Unit>
{
    public override async Task<Unit> RunAsync(WorkflowContext context, ProcessDocumentsRequest input)
    {
        await context.CallActivityAsync(nameof(NotifyActivity), "Starting document processing...");

        await context.CallActivityAsync(nameof(NotifyActivity), "Document processing complete.");

        return Unit.Instance;
    }
}
