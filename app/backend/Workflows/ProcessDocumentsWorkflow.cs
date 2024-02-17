using Dapr.Workflow;

namespace MinimalApi.Workflows;

internal sealed record ProcessDocumentsRequest(string FileName, byte[] Document);

internal sealed record ProcessDocumentsResponse(string[] PageNames);

internal sealed class ProcessDocumentsWorkflow : Workflow<ProcessDocumentsRequest, ProcessDocumentsResponse>
{
    public override async Task<ProcessDocumentsResponse> RunAsync(WorkflowContext context, ProcessDocumentsRequest input)
    {
        await context.CallActivityAsync(nameof(NotifyActivity), $"Processing {input.FileName}...");

        var response = await context.CallActivityAsync<PaginateDocumentResponse>(nameof(PaginateDocumentActivity), input);

        var uploadedPages =
            response
                .Pages
                .Select(
                    page => new
                    {
                        Page = page,
                        UploadTask = context.CallActivityAsync<UploadFileResponse>(nameof(UploadFileActivity), new UploadFileRequest(page.PageName, page.Page))
                    })
                .ToArray();

        await Task.WhenAll(uploadedPages.Select(a => a.UploadTask));

        await context.CallActivityAsync(nameof(NotifyActivity), "Processing complete.");

        return new ProcessDocumentsResponse(
            uploadedPages
                .Where(r => r.UploadTask.Result.Success)
                .Select(r => r.Page.PageName)
                .ToArray());
    }
}
