using System.Collections.Generic;

namespace Html2md
{
    public class ConvertedDocument
    {
        public ConvertedDocument(string markdown, IReadOnlyList<ReferencedImage> collectedImages)
        {
            this.Markdown = markdown;
            this.Images = collectedImages;
        }

        public string Markdown { get; }
        public IReadOnlyList<ReferencedImage> Images { get; }
    }
}
