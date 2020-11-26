using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SwaggerValidationPoc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProxyController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> Post(string destinationDomain, string swaggerJson, string destinationApi, string jsonPayloadPath)
        {
            using var httpClient = new HttpClient();

            // Need to be saved locally to improve performance
            var swaggerSchema = await httpClient.GetStringAsync($"{destinationDomain}{swaggerJson}");

            // Read V3 as YAML
            var openApiDocument = new OpenApiStringReader().Read(swaggerSchema, out var diagnostic);

            // Mock the payload
            var jsonString = System.IO.File.ReadAllText(jsonPayloadPath);

            // Mock Request
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{destinationDomain}{destinationApi}"),
                Method = HttpMethod.Post,
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };

            var validator = new RequestValidator(new[] { new JsonContentValidator() });
            validator.Validate(request, openApiDocument, destinationApi, OperationType.Post);

            return Ok();
        }
    }
}
