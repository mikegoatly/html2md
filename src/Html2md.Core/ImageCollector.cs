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
        private readonly ILogger logger;
        private readonly HashSet<Uri> imageUris = new HashSet<Uri>();

        public ImageCollector(ILogger logger)
        {
            this.logger = logger;
        }

        public bool CanCollect(Uri pageUri, string imageUri)
        {
            if (Uri.TryCreate(imageUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri.IsAbsoluteUri == false || uri.Host == pageUri.Host;
            }

            return false;
        }

        public string Collect(Uri pageUri, string imageUri)
        {
            this.imageUris.Add(new Uri(pageUri, imageUri));
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
                    var data = await client.GetByteArrayAsync(image);
                    collectedImages.Add(new ReferencedImage(image, data));
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
