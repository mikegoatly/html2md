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
        private readonly Uri pageUri;
        private readonly ILogger logger;
        private readonly List<string> imageUris = new List<string>();

        public ImageCollector(Uri pageUri, ILogger logger)
        {
            this.pageUri = pageUri;
            this.logger = logger;
        }

        public bool CanCollect(string imageUri)
        {
            if (Uri.TryCreate(imageUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri.IsAbsoluteUri == false || uri.Host == pageUri.Host;
            }

            return false;
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
                    var data = await client.GetByteArrayAsync(new Uri(this.pageUri, image));
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
