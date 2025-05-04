using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAnalyzerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly string endpoint = "https://neutronrecoginer.cognitiveservices.azure.com/"; // Replace with your endpoint
        private readonly string apiKey = "2j5rvtGs4MrtKUfQubwmhyd1Phe9HXsFcSW6BVg7yXK89BAQa1uJJQQJ99BDACZoyfiXJ3w3AAALACOG2HDn";     // Replace with your API key

        [HttpPost("analyze-document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeDocument([FromForm] IFormFile file, [FromQuery] string format = "json")
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            using var stream = file.OpenReadStream();
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            DocumentResultDto resultDto = null;
            string modelUsed = null;

            // Try analyzing with the ID Document model first
            using (var modelStream = new MemoryStream(fileBytes))
            {
                try
                {
                    var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-idDocument", modelStream);
                    var result = operation.Value;

                    if (result.Documents.FirstOrDefault()?.Fields.Count > 0)
                    {
                        resultDto = CreateDocumentResultDto("prebuilt-idDocument", result.Documents.First());
                        modelUsed = "prebuilt-idDocument";
                    }
                }
                catch
                {
                    // Ignore and try the next model
                }
            }

            // If ID Document didn't yield results, try the Invoice model
            if (resultDto == null)
            {
                using (var modelStream = new MemoryStream(fileBytes))
                {
                    try
                    {
                        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", modelStream);
                        var result = operation.Value;

                        if (result.Documents.FirstOrDefault()?.Fields.Count > 0)
                        {
                            resultDto = CreateDocumentResultDto("prebuilt-invoice", result.Documents.First(), result.Tables);
                            modelUsed = "prebuilt-invoice";
                        }
                    }
                    catch
                    {
                        // Ignore and try the next model
                    }
                }
            }

            // If Invoice didn't yield results, try the Receipt model
            if (resultDto == null)
            {
                using (var modelStream = new MemoryStream(fileBytes))
                {
                    try
                    {
                        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", modelStream);
                        var result = operation.Value;

                        if (result.Documents.FirstOrDefault()?.Fields.Count > 0)
                        {
                            resultDto = CreateDocumentResultDto("prebuilt-receipt", result.Documents.First(), result.Tables);
                            modelUsed = "prebuilt-receipt";
                        }
                    }
                    catch
                    {
                        // Ignore and try the next model
                    }
                }
            }

            // Finally, try the general Document model
            if (resultDto == null)
            {
                using (var modelStream = new MemoryStream(fileBytes))
                {
                    try
                    {
                        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", modelStream);
                        var result = operation.Value;

                        if (result.Documents.FirstOrDefault()?.Fields.Count > 0)
                        {
                            resultDto = CreateDocumentResultDto("prebuilt-document", result.Documents.First(), result.Tables);
                            modelUsed = "prebuilt-document";
                        }
                    }
                    catch
                    {
                        // Ignore if even the general model fails
                    }
                }
            }

            if (resultDto == null)
                return BadRequest("Could not extract any relevant data using the prebuilt models for ID documents, invoices, or receipts.");

            resultDto.ModelUsed = modelUsed; // Ensure the ModelUsed is correctly set

            // Support for XML/XAML or JSON
            if (format.ToLower() == "xaml" || format.ToLower() == "xml")
                return Ok(resultDto); // Will serialize as XML if Accept header or query is correct

            return new JsonResult(resultDto); // Default to JSON
        }

        private DocumentResultDto CreateDocumentResultDto(string modelName, AnalyzedDocument document, IReadOnlyList<DocumentTable> tables = null)
        {
            var resultDto = new DocumentResultDto
            {
                ModelUsed = modelName,
                DocumentType = document.DocumentType,
                Fields = new Dictionary<string, string>(),
                Items = new List<Dictionary<string, string>>()
            };

            foreach (var field in document.Fields)
            {
                resultDto.Fields[field.Key] = field.Value.Content?.Replace("\n", " ").Trim();
            }

            if (tables != null && tables.Count > 0)
            {
                var extractedTables = new List<Dictionary<string, string>>();
                foreach (var table in tables)
                {
                    foreach (var rowGroup in table.Cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key))
                    {
                        var row = new Dictionary<string, string>();
                        foreach (var cell in rowGroup.OrderBy(c => c.ColumnIndex))
                        {
                            row[$"Column{cell.ColumnIndex + 1}"] = cell.Content?.Replace("\n", " ").Trim();
                        }
                        extractedTables.Add(row);
                    }
                }
                resultDto.Items = extractedTables;
            }

            return resultDto;
        }
    }
}