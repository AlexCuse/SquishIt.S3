using System;
using SquishIt.Framework.Files;

namespace SquishIt.S3
{
    public class S3FileReaderFactory : IFileReaderFactory
    {
        public IFileReader GetFileReader(string file)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string file)
        {
            throw new NotImplementedException();
        }
    }
}
