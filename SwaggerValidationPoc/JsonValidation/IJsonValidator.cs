using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SwaggerValidationPoc.JsonValidation
{
    public interface IJsonValidator
    {
        bool CanValidate(OpenApiSchema schema);

        bool Validate(OpenApiSchema schema, OpenApiDocument openApiDocument, JToken instance, out IEnumerable<string> errorMessages);
    }
}
