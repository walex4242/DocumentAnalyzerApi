using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Mvc;


namespace DocumentAnalyzerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly string endpoint = "https://neutronrecoginer.cognitiveservices.azure.com/";
        private readonly string apiKey = "2j5rvtGs4MrtKUfQubwmhyd1Phe9HXsFcSW6BVg7yXK89BAQa1uJJQQJ99BDACZoyfiXJ3w3AAALACOG2HDn";

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

            var modelCandidates = new[]
            {
        "prebuilt-invoice",
        "prebuilt-receipt",
        "prebuilt-idDocument",
        "prebuilt-businessCard",
        "prebuilt-document"
    };

            DocumentResultDto resultDto = null;

            foreach (var model in modelCandidates)
            {
                using var modelStream = new MemoryStream(fileBytes);
                try
                {
                    var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, model, modelStream);
                    var result = operation.Value;

                    var doc = result.Documents.FirstOrDefault();
                    if (doc != null && doc.Fields.Count > 0)
                    {
                        resultDto = new DocumentResultDto
                        {
                            ModelUsed = model,
                            DocumentType = doc.DocumentType,
                            Fields = new Dictionary<string, string>(),
                            Items = new List<Dictionary<string, string>>()
                        };

                        foreach (var field in doc.Fields)
                        {
                            resultDto.Fields[field.Key] = field.Value.Content?.Replace("\n", " ").Trim();
                        }

                        if (result.Tables.Count > 0)
                        {
                            var tables = new List<Dictionary<string, string>>();
                            foreach (var table in result.Tables)
                            {
                                foreach (var rowGroup in table.Cells.GroupBy(c => c.RowIndex).OrderBy(g => g.Key))
                                {
                                    var row = new Dictionary<string, string>();
                                    foreach (var cell in rowGroup.OrderBy(c => c.ColumnIndex))
                                    {
                                        row[$"Column{cell.ColumnIndex + 1}"] = cell.Content?.Replace("\n", " ").Trim();
                                    }
                                    tables.Add(row);
                                }
                            }
                            resultDto.Items = tables;
                        }

                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (resultDto == null)
                return BadRequest("Could not extract any data using prebuilt models.");

            // Support for XML/XAML or JSON
            if (format.ToLower() == "xaml" || format.ToLower() == "xml")
                return Ok(resultDto); // Will serialize as XML if Accept header or query is correct

            return new JsonResult(resultDto); // Default to JSON
        }

    }

    public class DocumentResultDto
    {
        public string ModelUsed { get; set; }
        public string DocumentType { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public List<Dictionary<string, string>> Items { get; set; }
    }
}