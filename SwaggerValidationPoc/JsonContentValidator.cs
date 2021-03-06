﻿using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using SwaggerValidationPoc.JsonValidation;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SwaggerValidationPoc
{
    public class JsonContentValidator : IContentValidator
    {
        private readonly JsonValidator _jsonValidator;

        public JsonContentValidator()
        {
            _jsonValidator = new JsonValidator();
        }

        public bool CanValidate(string mediaType)
        {
            return mediaType.Contains("json");
        }

        public void Validate(OpenApiMediaType mediaTypeSpec, OpenApiDocument openApiDocument, HttpContent content)
        {
            if (mediaTypeSpec?.Schema == null) return;

            var instance = JToken.Parse(content.ReadAsStringAsync().Result);
            if (!_jsonValidator.Validate(mediaTypeSpec.Schema, openApiDocument, instance, out IEnumerable<string> errorMessages))
                throw new ContentDoesNotMatchSpecException(string.Join(Environment.NewLine, errorMessages));
        }
    }
}
