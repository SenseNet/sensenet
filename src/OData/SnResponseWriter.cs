using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace SenseNet.OData
{
    internal class SnResponseWriter : StringWriter
    {
        private readonly HttpResponse _response;

        public SnResponseWriter(HttpResponse response)
        {
            _response = response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _response.WriteAsync(GetStringBuilder().ToString()).GetAwaiter().GetResult();
            base.Dispose(disposing);
        }
    }
}
