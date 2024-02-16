using Dapr.Workflow;

namespace MinimalApi.Workflows;

internal sealed record PaginateDocumentRequest(string FileName, byte[] Document);

internal sealed record PaginatedPage(string PageName, byte[] Page);

internal sealed record PaginateDocumentResponse(PaginatedPage[] Pages);

internal sealed class PaginateDocumentActivity : WorkflowActivity<PaginateDocumentRequest, PaginateDocumentResponse>
{
    private readonly ILogger<PaginateDocumentActivity> _logger;

    public PaginateDocumentActivity(ILogger<PaginateDocumentActivity> logger)
    {
        this._logger = logger;
    }

    public override Task<PaginateDocumentResponse> RunAsync(WorkflowActivityContext context, PaginateDocumentRequest input)
    {
        this._logger.LogInformation("Paginating document...");

        using var bufferedStream = new MemoryStream(input.Document);
        using var documents = PdfReader.Open(bufferedStream, PdfDocumentOpenMode.Import);

        var pages = new List<PaginatedPage>();

        for (int i = 0; i < documents.PageCount; i++)
        {
            var documentName = BlobNameFromFilePage(input.FileName, i);

            var tempFileName = Path.GetTempFileName();

            using var document = new PdfDocument();
            
            document.AddPage(documents.Pages[i]);

            using var tempStream = new MemoryStream();

            document.Save(tempStream);

            //TODO: Can this be done without a copy?
            pages.Add(new PaginatedPage(documentName, tempStream.ToArray()));
        }

        this._logger.LogInformation("{Count} pages generated.", pages.Count);

        return Task.FromResult(new PaginateDocumentResponse(pages.ToArray()));
    }

    private static string BlobNameFromFilePage(string filename, int page = 0) =>
        Path.GetExtension(filename).ToLower() is ".pdf"
            ? $"{Path.GetFileNameWithoutExtension(filename)}-{page}.pdf"
            : Path.GetFileName(filename);
}