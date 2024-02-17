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
            List<string> workflowIds = [];

            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();

                var byteBuffer = new byte[stream.Length];

                await using var buffer = new MemoryStream(byteBuffer);

                await stream.CopyToAsync(buffer, cancellationToken);

                var request = new ProcessDocumentsRequest(file.FileName, byteBuffer);

                var instanceId = Guid.NewGuid().ToString("N");

                await workflowClient.ScheduleNewWorkflowAsync(
                    nameof(ProcessDocumentsWorkflow),
                    instanceId,
                    request);

                workflowIds.Add(instanceId);
            }

            var waitTasks =
                workflowIds
                    .Select(id => workflowClient.WaitForWorkflowCompletionAsync(id, /* TODO: Can we get *only* the output and not the input? */ true, cancellationToken))
                    .ToArray();

            var states = await Task.WhenAll(waitTasks);

            var uploadedFiles =
                states
                    .SelectMany(state => state.ReadOutputAs<ProcessDocumentsResponse>()?.PageNames ?? Array.Empty<string>())
                    .ToArray();

            if (uploadedFiles.Length is 0)
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
