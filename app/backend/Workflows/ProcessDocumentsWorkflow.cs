using Dapr.Workflow;

namespace MinimalApi.Workflows;

internal sealed record ProcessDocumentsRequest(string FileName, byte[] Document);

internal sealed class ProcessDocumentsWorkflow : Workflow<ProcessDocumentsRequest, Unit>
{
    public override async Task<Unit> RunAsync(WorkflowContext context, ProcessDocumentsRequest input)
    {
        await context.CallActivityAsync(nameof(NotifyActivity), $"Processing {input.FileName}...");

        var response = await context.CallActivityAsync<PaginateDocumentResponse>(nameof(PaginateDocumentActivity), input);

        var uploadActivities =
            response
                .Pages
                .Select(page => context.CallActivityAsync<UploadFileResponse>(
                    nameof(UploadFileActivity),
                    new UploadFileRequest(page.PageName, page.Page)))
                .ToArray();

        await Task.WhenAll(uploadActivities);

        await context.CallActivityAsync(nameof(NotifyActivity), "Processing complete.");

        return Unit.Instance;
    }
}
