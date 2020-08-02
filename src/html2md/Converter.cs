using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace html2md
{
    public class CollectedImage
    {
        public CollectedImage(string fileName, byte[] data)
        {
            this.FileName = fileName;
            this.Data = data;
        }

        public string FileName { get; }
        public byte[] Data { get; }
    }

    public class ImageCollector
    {
        private readonly Uri relativeUri;
        private readonly List<string> imageUris = new List<string>();

        public ImageCollector(Uri relativeUri)
        {
            this.relativeUri = relativeUri;
        }

        public string Collect(string imageUri)
        {
            this.imageUris.Add(imageUri);
            return Path.GetFileName(imageUri);
        }

        public async Task<IReadOnlyList<CollectedImage>> GetCollectedImagesAsync(HttpClient client)
        {
            var collectedImages = new List<CollectedImage>(this.imageUris.Count);
            foreach (var image in this.imageUris)
            {
                try
                {
                    Console.WriteLine("Loading image data for " + image);
                    var data = await client.GetByteArrayAsync(new Uri(this.relativeUri, image));
                    collectedImages.Add(new CollectedImage(Path.GetFileName(image), data));
                }
                catch (HttpRequestException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Error getting image data: {ex.Message}");
                    Console.ResetColor();
                }
            }

            return collectedImages;
        }
    }

    public class Converter
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
        private readonly CommandLineArgs args;

        public Converter(CommandLineArgs args)
        {
            this.args = args;
        }

        public async Task<(string markdown, IReadOnlyList<CollectedImage> collectedImages)> ConvertAsync(Uri url)
        {
            var content = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            var builder = new StringBuilder();
            var imageCollector = new ImageCollector(url);

            Console.WriteLine("Loading page content for " + url);
            doc.LoadHtml(content);

            this.ProcessNode(doc.DocumentNode, builder, imageCollector, false);

            return (
                this.RemoveRedundantWhiteSpace(builder.ToString()),
                await imageCollector.GetCollectedImagesAsync(client)
                );
        }

        private string RemoveRedundantWhiteSpace(string text)
        {
            return Regex.Replace(text, "(^" + Environment.NewLine + "){2,}", Environment.NewLine, RegexOptions.Multiline);
        }

        private void ProcessNode(HtmlNode node, StringBuilder builder, ImageCollector imageCollector, bool emitText, string listItemType = "-")
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    if (emitText)
                    {
                        builder.Append(this.ExtractText(node));
                    }

                    break;

                case HtmlNodeType.Document:
                case HtmlNodeType.Element:
                    if (!this.args.ExcludeTags.Contains(node.Name))
                    {
                        var emitNewLineAfterChildren = false;
                        emitText = emitText || this.args.IncludeTags.Contains(node.Name);
                        if (emitText)
                        {
                            switch (node.Name)
                            {
                                case "table":
                                    this.EmitTable(node, builder, imageCollector);
                                    this.EmitNewLine(builder);
                                    return;

                                case "img":
                                    this.EmitImage(node, builder, imageCollector);
                                    return;

                                case "p":
                                    emitNewLineAfterChildren = true;
                                    break;

                                case "ul":
                                    listItemType = "-";
                                    emitNewLineAfterChildren = true;
                                    break;

                                case "ol":
                                    listItemType = "1.";
                                    emitNewLineAfterChildren = true;
                                    break;

                                case "li":
                                    this.EmitListItemPrefix(builder, listItemType);
                                    break;

                                case "h1":
                                case "h2":
                                case "h3":
                                case "h4":
                                case "h5":
                                case "h6":
                                case "h7":
                                    this.EmitNewLine(builder);
                                    emitNewLineAfterChildren = true;
                                    builder.Append('#', node.Name[1] - '0').Append(' ');
                                    break;

                                case "a":
                                    this.EmitLink(node, builder, imageCollector);
                                    return;

                                case "em":
                                    this.EmitFormattedText(node, builder, "*");
                                    return;

                                case "strong":
                                    this.EmitFormattedText(node, builder, "**");
                                    return;

                                case "pre":
                                    this.EmitPreformattedText(node, builder);
                                    return;
                            }
                        }

                        this.ProcessChildNodes(node.ChildNodes, builder, imageCollector, emitText, listItemType);

                        if (emitNewLineAfterChildren)
                        {
                            this.EmitNewLine(builder);
                        }
                    }

                    break;
            }
        }

        private void ProcessChildNodes(HtmlNodeCollection nodes, StringBuilder builder, ImageCollector imageCollector, bool emitText, string listItemType = "-")
        {
            foreach (var childNode in nodes)
            {
                this.ProcessNode(childNode, builder, imageCollector, emitText, listItemType);
            }
        }

        private void EmitImage(HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
        {
            var src = node.GetAttributeValue("src", null);
            var alt = node.GetAttributeValue("alt", "image");

            if (src == null)
            {
                this.Warn("No src attribute for image - it will not be emitted: " + node.OuterHtml);
            }
            else
            {
                var imageUri = imageCollector.Collect(src);
                builder.Append("![").Append(alt).Append("](").Append(imageUri).Append(')');
            }
        }

        private void EmitTable(HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
        {
            this.EmitNewLine(builder);

            var (headers, skipFirstRow) = this.GetTableHeaders(node);

            foreach (var header in headers)
            {
                builder.Append("|").Append(header);
            }

            builder.AppendLine("|");

            this.EmitRepeated(builder, "|-", headers.Count, "|");

            IEnumerable<HtmlNode> rows = this.GetTableRows(node);
            if (skipFirstRow)
            {
                rows = rows.Skip(1);
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells.Count != headers.Count)
                {
                    this.Warn("Table row has different number of columns to header - output will likely be malformed");
                }

                foreach (var cell in cells)
                {
                    builder.Append("|");
                    this.ProcessNode(cell, builder, imageCollector, true);
                }

                builder.AppendLine("|");
            }
        }

        private (IReadOnlyList<string> headers, bool skipFirstRow) GetTableHeaders(HtmlNode node)
        {
            var headRow = node.SelectSingleNode("thead/tr");
            var skipFirstRow = false;
            if (headRow == null)
            {
                headRow = node.SelectSingleNode("//tr");
                skipFirstRow = true;
            }

            return
                (
                    (headRow.SelectNodes("td") ?? Enumerable.Empty<HtmlNode>())
                        .Concat(headRow.SelectNodes("th") ?? Enumerable.Empty<HtmlNode>())
                        .Select(n => this.ExtractText(n))
                        .ToList(),
                    skipFirstRow
                );
        }

        private HtmlNodeCollection GetTableRows(HtmlNode node)
        {
            var tbody = node.SelectSingleNode("tbody");
            if (tbody != null)
            {
                return tbody.SelectNodes("tr");
            }

            return node.SelectNodes("tr");
        }

        private void EmitRepeated(StringBuilder builder, string repeatedText, int count, string endofLineText)
        {
            for (var i = 0; i < count; i++)
            {
                builder.Append(repeatedText);
            }

            builder.AppendLine(endofLineText);
        }

        private void EmitListItemPrefix(StringBuilder builder, string listItemType)
        {
            builder.AppendLine().Append(listItemType).Append(" ");
        }

        private void EmitNewLine(StringBuilder builder)
        {
            builder.AppendLine().AppendLine();
        }

        private void EmitPreformattedText(HtmlNode node, StringBuilder builder)
        {
            if (node.HasClass("code"))
            {
                this.EmitPreformattedText(node, builder, this.args.DefaultCodeLanguage);
            }
            else
            {
                this.EmitPreformattedText(node, builder, string.Empty);
            }
        }

        private void EmitPreformattedText(HtmlNode node, StringBuilder builder, string language)
        {
            this.EmitNewLine(builder);

            builder
                .Append("``` ")
                .AppendLine(language)
                .AppendLine(HttpUtility.HtmlDecode(this.ExtractText(node, removeOnlyLeadingAndTrailingNewLines: true)))
                .AppendLine("```");
        }

        private void EmitFormattedText(HtmlNode node, StringBuilder builder, string wrapWith)
        {
            builder
                .Append(wrapWith)
                .Append(this.ExtractText(node))
                .Append(wrapWith);
        }

        private string ExtractText(HtmlNode node, bool removeOnlyLeadingAndTrailingNewLines = false)
        {
            var text = node.InnerText;
            if (removeOnlyLeadingAndTrailingNewLines)
            {
                return text.Trim('\r', '\n');
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // return Regex.Replace(text, "[\r\n]+", "");
            return text;
        }

        private void EmitLink(HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
        {
            var href = node.GetAttributeValue("href", null);
            if (href == null)
            {
                this.Warn("Anchor has missing href and will be rendered as text: " + node.OuterHtml);
                this.ProcessChildNodes(node.ChildNodes, builder, imageCollector, true);
            }
            else
            {
                // Handle special case where the link is to an image - we'll download that to the images folder
                // as if it was an img tag
                if (contentTypeProvider.TryGetContentType(href, out var contentType) && contentType.StartsWith("image/"))
                {
                    href = imageCollector.Collect(href);
                }

                builder.Append($"[");
                this.ProcessChildNodes(node.ChildNodes, builder, imageCollector, true);
                builder.Append("](").Append(href).Append(')');
            }
        }

        private void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
