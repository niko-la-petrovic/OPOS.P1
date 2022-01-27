using System;
using System.IO;

namespace OPOS.P1.Lib.Threading
{
    public abstract class CustomResource
    {
        public string Uri { get; init; }

        public CustomResource(string uri)
        {
            Uri = uri;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomResource resource &&
                   Uri == resource.Uri;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Uri);
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
