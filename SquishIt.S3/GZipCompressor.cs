using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Amazon.S3.Model;

namespace SquishIt.S3
{
    internal class GZipCompressor : ICompressor
    {
        public GZipCompressor()
        {
            Headers = new NameValueCollection {{"Content-Encoding", "gzip"}};
        }

        public NameValueCollection Headers { get; private set; }

        public Stream Compress(string content)
        {
            var ms = new MemoryStream();
            using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                var buffer = Encoding.UTF8.GetBytes(content);
                zip.Write(buffer, 0, buffer.Length);
                zip.Flush();
            }

            ms.Position = 0;
            return ms;
        }
    }
}
