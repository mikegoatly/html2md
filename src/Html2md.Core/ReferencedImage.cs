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
        /// <param name="fileName">The file name of the image.</param>
        /// <param name="data">The raw image data.</param>
        public ReferencedImage(string fileName, byte[] data)
        {
            this.FileName = fileName;
            this.Data = data;
        }

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
