using System;
using System.IO;

namespace Html2md
{
    /// <summary>
    /// An image referenced in a converted page.
    /// </summary>
    public class ReferencedImage
    {
        /// <summary>
        /// Constructs a new instance of <see cref="ReferencedImage"/>.
        /// </summary>
        /// <param name="originalUri">The original URI of the image.</param>
        /// <param name="data">The raw image data.</param>
        public ReferencedImage(Uri originalUri, byte[] data)
        {
            this.OriginalUri = originalUri;
            this.FileName = Path.GetFileName(originalUri.AbsoluteUri);
            this.Data = data;
        }

        /// <summary>
        /// Gets the original uri of the image.
        /// </summary>
        public Uri OriginalUri { get; }

        /// <summary>
        /// Gets the file name of the image.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the raw image data.
        /// </summary>
        public byte[] Data { get; }
    }
}
