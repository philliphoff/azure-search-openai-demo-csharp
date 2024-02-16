using Dapr.Workflow;

namespace  MinimalApi.Workflows;

internal sealed record UploadFileRequest(string FileName, byte[] Page)
{
    public bool? Overwrite { get; init; }
}

internal sealed record UploadFileResponse(bool Success);

internal sealed class UploadFileActivity : WorkflowActivity<UploadFileRequest, UploadFileResponse>
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<UploadFileActivity> _logger;

    public UploadFileActivity(BlobContainerClient container, ILogger<UploadFileActivity> logger)
    {
        this._container = container;
        this._logger = logger;
    }

    public override async Task<UploadFileResponse> RunAsync(WorkflowActivityContext context, UploadFileRequest input)
    {
        this._logger.LogInformation("Uploading file {FileName}...", input.FileName);

        var blobClient = this._container.GetBlobClient(input.FileName);

        if (input.Overwrite != true && await blobClient.ExistsAsync())
        {
            this._logger.LogInformation("File already exists.");

            return new UploadFileResponse(false);
        }

        await using var tempStream = new MemoryStream(input.Page);

        await blobClient.UploadAsync(
            tempStream,
            new BlobHttpHeaders
            {
                ContentType = "application/pdf"
            });

        this._logger.LogInformation("File uploaded.");

        return new UploadFileResponse(true);
    }
}
