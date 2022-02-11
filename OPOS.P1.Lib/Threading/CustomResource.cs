using System;
using System.IO;
using System.Text.Json.Serialization;

namespace OPOS.P1.Lib.Threading
{
    public class CustomResource
    {
        public string Uri { get; init; }

        public CustomResource()
        {

        }

        public CustomResource(string uri) => Uri = uri;

        public override bool Equals(object obj)
        {
            return obj is CustomResource resource &&
                   Uri == resource.Uri;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Uri);
        }

        public override string ToString()
        {
            return $"Uri = {Uri}";
        }

        //public abstract void Initialize();
    }

    public class CustomResourceFile : CustomResource
    {
        public CustomResourceFile(string uri) : base(uri)
        {
        }
    }
}
