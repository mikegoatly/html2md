using System;
using System.IO;

namespace Html2md
{
    public class ConvertedDocument
    {
        public ConvertedDocument(Uri originalLocation, string markdown)
        {
            this.Name = Path.GetFileNameWithoutExtension(originalLocation.AbsoluteUri) + ".md";
            this.OriginalLocation = originalLocation;
            this.Markdown = markdown;
        }

        public string Name { get; }
        public Uri OriginalLocation { get; }
        public string Markdown { get; }
    }
}
