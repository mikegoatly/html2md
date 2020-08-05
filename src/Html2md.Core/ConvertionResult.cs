using System.Collections.Generic;

namespace Html2md
{
    public class ConvertionResult
    {
        public ConvertionResult(IReadOnlyList<ConvertedDocument> documents, IReadOnlyList<ReferencedImage> images)
        {
            this.Documents = documents;
            this.Images = images;
        }

        public IReadOnlyList<ConvertedDocument> Documents { get; }
        public IReadOnlyList<ReferencedImage> Images { get; }
    }
}
