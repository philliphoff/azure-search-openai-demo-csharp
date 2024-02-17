// Copyright (c) Microsoft. All rights reserved.

using Dapr.Workflow;
using MinimalApi.Workflows;

namespace MinimalApi.Services;

internal sealed class AzureBlobStorageService(DaprWorkflowClient workflowClient)
{
    internal static DefaultAzureCredential DefaultCredential { get; } = new();

    internal async Task<UploadDocumentsResponse> UploadFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            List<string> uploadedFiles = [];
            foreach (var file in files)
            {
                var fileName = file.FileName;

                await using var stream = file.OpenReadStream();

                var byteBuffer = new byte[stream.Length];
                using var buffer = new MemoryStream(byteBuffer);

                await stream.CopyToAsync(buffer, cancellationToken);

                var request = new ProcessDocumentsRequest(fileName, byteBuffer);

                var instanceId = Guid.NewGuid().ToString("N");

                await workflowClient.ScheduleNewWorkflowAsync(
                    nameof(ProcessDocumentsWorkflow),
                    instanceId,
                    request);

                var state = await workflowClient.WaitForWorkflowCompletionAsync(
                    instanceId,
                    // TODO: Can we get *only* the output (and not input)?
                    true,
                    cancellationToken);

                uploadedFiles.AddRange(state.ReadOutputAs<ProcessDocumentsResponse>()?.PageNames ?? Array.Empty<string>());
            }

            if (uploadedFiles.Count is 0)
            {
                return UploadDocumentsResponse.FromError("""
                    No files were uploaded. Either the files already exist or the files are not PDFs.
                    """);
            }

            return new UploadDocumentsResponse([.. uploadedFiles]);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return UploadDocumentsResponse.FromError(ex.ToString());
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}
