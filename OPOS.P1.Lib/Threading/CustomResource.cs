using System;

namespace OPOS.P1.Lib.Threading
{
    public class CustomResource
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
    }
}
