using System;
using System.Web;

namespace SquishIt.S3
{
    public interface IKeyBuilder
    {
        string GetKeyFor(string path);
    }

    internal class KeyBuilder : IKeyBuilder
    {
        public string GetKeyFor(string path)
        {
            return RelativeFromAbsolutePath(path).TrimStart('/');
        }

        static string RelativeFromAbsolutePath(string path)
        {
            if(HttpContext.Current != null)
            {
                var request = HttpContext.Current.Request;
                if (request != null)
                {
                    return path.Replace(request.ServerVariables["APPL_PHYSICAL_PATH"], "/").Replace(@"\", "/");
                }
            }
            throw new InvalidOperationException("We can only map an absolute back to a relative path if an HttpContext is available.");
        }
    }
}
