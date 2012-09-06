using System;
using System.Collections.Specialized;
using System.IO;

namespace SquishIt.S3
{
    public interface ICompressor
    {
        NameValueCollection Headers { get; }
        Stream Compress(string content);
    }
}