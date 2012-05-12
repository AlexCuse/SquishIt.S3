namespace SquishIt.S3
{
    public interface IKeyBuilder
    {
        string GetKeyFor(string path);
    }

    internal class KeyBuilder : IKeyBuilder
    {
        readonly string physicalApplicationPath;
        readonly string virtualDirectory;

        public KeyBuilder(string physicalApplicationPath, string virtualDirectory)
        {
            this.physicalApplicationPath = physicalApplicationPath;
            this.virtualDirectory = virtualDirectory;
        }

        public string GetKeyFor(string path)
        {
            return RelativeFromAbsolutePath(path).TrimStart('/');
        }

        //TODO: it would be nice to find a way to test this (using a mocked HttpContext(Base) resolver would help)
        string RelativeFromAbsolutePath(string path)
        {
            path = path.StartsWith(physicalApplicationPath)
                           ? path.Substring(physicalApplicationPath.Length)
                           : path;

            return virtualDirectory + "/" + path.Replace(@"\", "/").TrimStart('/');
        }
    }
}
