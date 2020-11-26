using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;

namespace SwaggerValidationPoc
{
    public class RequestValidator
    {
        private readonly IEnumerable<IContentValidator> _contentValidators;

        public RequestValidator (IEnumerable<IContentValidator> contentValidators)
        {
            _contentValidators = contentValidators;
        }

        public void Validate(HttpRequestMessage request, OpenApiDocument openApiDocument, string pathTemplate, OperationType operationType)
        {
            var operationSpec = openApiDocument.GetOperationByPathAndType(pathTemplate, operationType, out OpenApiPathItem pathSpec);

            // Convert to absolute Uri as a workaround to limitation with Uri class - i.e. most of it's methods are not supported for relative Uri's.
            var requestUri = new Uri(new Uri("http://tempuri.org"), request.RequestUri);

            if (!TryParsePathNameValues(pathTemplate, requestUri.AbsolutePath, out NameValueCollection pathNameValues))
                throw new RequestDoesNotMatchSpecException($"Request URI '{requestUri.AbsolutePath}' does not match specified template '{pathTemplate}'");

            if (request.Method != new HttpMethod(operationType.ToString()))
                throw new RequestDoesNotMatchSpecException($"Request method '{request.Method}' does not match specified operation type '{operationType}'");

            if (operationSpec.RequestBody != null)
                ValidateContent(operationSpec.RequestBody, openApiDocument, request.Content);
        }

        private bool TryParsePathNameValues(string pathTemplate, string requestUri, out NameValueCollection pathNameValues)
        {
            pathNameValues = new NameValueCollection();

            var templateMatcher = new TemplateMatcher(TemplateParser.Parse(pathTemplate), null);
            var routeValues = new RouteValueDictionary();
            if (!templateMatcher.TryMatch(new PathString(requestUri), routeValues))
                return false;

            foreach (var entry in routeValues)
            {
                pathNameValues.Add(entry.Key, entry.Value.ToString());
            }
            return true;
        }

        private void ValidateContent(OpenApiRequestBody requestBodySpec, OpenApiDocument openApiDocument, HttpContent content)
        {
            requestBodySpec = (requestBodySpec.Reference != null)
                ? (OpenApiRequestBody)openApiDocument.ResolveReference(requestBodySpec.Reference)
                : requestBodySpec;

            if (requestBodySpec.Required && content == null)
                throw new RequestDoesNotMatchSpecException("Required content is not present");

            if (content == null) return;

            if (!requestBodySpec.Content.TryGetValue(content.Headers.ContentType.MediaType, out OpenApiMediaType mediaTypeSpec))
                throw new RequestDoesNotMatchSpecException($"Content media type '{content.Headers.ContentType.MediaType}' is not specified");

            try
            {
                foreach (var contentValidator in _contentValidators)
                {
                    if (contentValidator.CanValidate(content.Headers.ContentType.MediaType))
                        contentValidator.Validate(mediaTypeSpec, openApiDocument, content);
                }
            }
            catch (ContentDoesNotMatchSpecException contentException)
            {
                throw new RequestDoesNotMatchSpecException($"Content does not match spec. {contentException.Message}");
            }
        }
    }
}
