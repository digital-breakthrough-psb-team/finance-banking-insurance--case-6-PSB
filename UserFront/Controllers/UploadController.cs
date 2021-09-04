using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserFront.Models;

namespace UserFront.Controllers{
  [ApiController]
    [Route("api/documents")]
    public class UploadController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Documents API is running...");
        }

        [HttpPost]
        [Route("upload-and-recognize")]
        public async Task<IActionResult> Upload(IFormFile file, string contextData)
        {
            var filePath = Path.GetTempFileName();
            var fileName = file.FileName;

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                Console.WriteLine($"filePath: {filePath}");
            }
            string recordId = await CreateDocumentRecord(filePath, fileName, contextData, "recognition");

            ProcessFile(recordId, contextData, filePath, fileName);

            return Ok(new {recordId = recordId});
        }

        [HttpGet]
        [Route("upload-and-recognize/checkStatus/{recordId}")]
        public async Task<IActionResult> checkProcessingStatus(string recordId){
            string filePath = GetDocumentRecordFilePath(recordId);
            byte[] encodedText;
            using (var sourceStream = new FileStream(filePath, FileMode.Open, 
                FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                encodedText = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(encodedText, 0, (int)sourceStream.Length);
                string fileContent = System.Text.Encoding.Unicode.GetString(encodedText);
                
                DocumentRecord record = JsonSerializer.Deserialize<DocumentRecord>(fileContent);
                return Ok(record);
            }
        }

        private async Task ProcessFile(string recordId, string contextData, string filePath, string fileName)
        {

            string cmdResult = null;
            try
            {
                 // run script
                cmdResult = await DoRecognition(filePath, fileName, contextData);

                // store script result in database
                if (cmdResult.Length == 0)
                {
                    await UpdateDocumentRecord(recordId, "unrecognized");
                }
                else if (cmdResult == "Error")
                {
                    await UpdateDocumentRecord(recordId, "error");
                }
                else
                {
                    await UpdateDocumentRecord(recordId, "done", cmdResult);
                    
                    await DoPushDocument(filePath, fileName, cmdResult, "1234567890", false);
                }

                
            }
            catch (System.Exception ex)
            {
                await UpdateDocumentRecord(recordId, "error");
                Console.Error.WriteLine($"Error on file proccessing: {ex}");
            }
            
            
        }

        private async Task DoPushDocument(string filePath, string fileName, string nomenclature, string contextData, bool unrecognized)
        {
            Console.WriteLine($"DoPushDocument: filePath: {filePath}, fileName: {fileName} ");
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.None,
                bufferSize: 4096, useAsync: true)
            )
            using (var client = new HttpClient())
            using (var formData = new MultipartFormDataContent()){
                HttpContent attachments = new StreamContent(sourceStream);
                string createRequestJson = JsonSerializer.Serialize(new {
                    documentNomenclatureId = nomenclature,
                    inn = contextData,
                    unrecognized = unrecognized
                });
                HttpContent createRequest = new StringContent(createRequestJson, Encoding.UTF8, "application/json");
                
                formData.Add(attachments, "attachments", fileName);
                formData.Add(createRequest, "createRequest");
                
                var request = new HttpRequestMessage() {
                    RequestUri = new Uri("http://elib-hackathon.psb.netintel.ru/elib/api/service/documents"),
                    Method = HttpMethod.Post,
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "ZWxpYi1zdXBlcnVzZXI6MTIz");
                request.Content=formData;

                try
                {
                     var resp = await client.SendAsync(request);

                     Console.WriteLine($"DoPushDocument: file pushing completed with status \"{resp.StatusCode}\"");
                }
                catch (System.Exception ex)
                {
                    Console.Error.WriteLine($"Error on DoPushDocument: {ex}");
                }
            }
        }

        private async Task UpdateDocumentRecord(string recordId, string status)
        {
            await UpdateDocumentRecord( recordId, status, null);
        }

        private async Task UpdateDocumentRecord(string recordId, string status, string nomenclatureId)
        {
            string documentRecordFilePath = GetDocumentRecordFilePath(recordId);
            byte[] encodedText;
            using (FileStream sourceStream = new FileStream(documentRecordFilePath,
                FileMode.Open, FileAccess.ReadWrite, FileShare.Read,
                bufferSize: 4096, useAsync: true)
            ){
                encodedText = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(encodedText, 0, (int)sourceStream.Length);
                string fileContent = System.Text.Encoding.Unicode.GetString(encodedText);
                
                DocumentRecord record = JsonSerializer.Deserialize<DocumentRecord>(fileContent);
                record.status = status;
                if(!(nomenclatureId is null)){
                    record.nomenclatureId = nomenclatureId;
                }

                fileContent = JsonSerializer.Serialize(record);
                encodedText = Encoding.Unicode.GetBytes(fileContent);

                sourceStream.SetLength(0);
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        private async Task<string> DoRecognition(string filePath, string fileName, string contextData)
        {
            Random rnd = new Random();
            await Task.Delay(rnd.Next(3, 5)*1000);
            string[] results = {"33a37ce4-c6a9-4dad-8424-707abd47c125", "Error", ""};
            // return results[rnd.Next(0, results.Length-1)];
            return results[0];
        }

        private async Task<string> CreateDocumentRecord(string filePath, string fileName, string contextData, string status)
        {  
            Console.WriteLine($"CreateDocumentRecord filePath: {filePath}"); 

            string recordId = Guid.NewGuid().ToString();
            string documentRecordFilePath = GetDocumentRecordFilePath(recordId);
            string fileContent = JsonSerializer.Serialize(new {
                inn = contextData,
                recordId = recordId,
                status = status,
                fileName = fileName,
                filePath = filePath
            });

            byte[] encodedText = Encoding.Unicode.GetBytes(fileContent);
            using (FileStream sourceStream = new FileStream(documentRecordFilePath,
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true)
            ){
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };

            return recordId;
            
        }

    private string GetDocumentRecordFilePath(string recordId)
    {
      return Path.Join(GetStorageFolder(), $"{recordId}.json");
    }

    private string GetStorageFolder()
    {
      return Environment.GetEnvironmentVariable("DOCUMENT_RECORDS_STORAGE");
    }
  }
}