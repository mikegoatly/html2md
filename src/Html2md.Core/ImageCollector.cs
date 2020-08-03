using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Html2md
{
    internal class ImageCollector
    {
        private readonly Uri relativeUri;
        private readonly ILogger logger;
        private readonly List<string> imageUris = new List<string>();

        public ImageCollector(Uri relativeUri, ILogger logger)
        {
            this.relativeUri = relativeUri;
            this.logger = logger;
        }

        public string Collect(string imageUri)
        {
            this.imageUris.Add(imageUri);
            return Path.GetFileName(imageUri);
        }

        public async Task<IReadOnlyList<ReferencedImage>> GetCollectedImagesAsync(HttpClient client)
        {
            var collectedImages = new List<ReferencedImage>(this.imageUris.Count);
            foreach (var image in this.imageUris)
            {
                try
                {
                    logger.LogInformation("Loading image data for {ImageName}", image);
                    var data = await client.GetByteArrayAsync(new Uri(this.relativeUri, image));
                    collectedImages.Add(new ReferencedImage(Path.GetFileName(image), data));
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning(ex, $"Error getting image data");
                }
            }

            return collectedImages;
        }
    }
}
