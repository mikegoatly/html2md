using HtmlAgilityPack;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Html2md
{
    public class MarkdownConverter
    {
        private static readonly FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
        private readonly IConversionOptions options;
        private readonly ILogger logger;
        private readonly HttpClient httpClient;
        private readonly FrontMatterExtractor frontMatterExtractor = new FrontMatterExtractor();

        public MarkdownConverter(IConversionOptions options, ILogger? logger = null)
            : this(options, null, logger)
        {
        }

        public MarkdownConverter(IConversionOptions options, HttpClient? httpClient = null, ILogger? logger = null)
        {
            this.options = options;
            this.logger = logger ?? NullLogger.Instance;
            this.httpClient = httpClient ?? new HttpClient();
        }

        public async Task<ConvertionResult> ConvertAsync(IEnumerable<Uri> urls)
        {
            urls = urls as IList<Uri> ?? urls.ToList();
            var builder = new StringBuilder();
            var imageCollector = new ImageCollector(this.logger);
            var documents = new List<ConvertedDocument>(urls.Count());

            foreach (var url in urls)
            {
                documents.Add(await this.ConvertAsync(url, builder, imageCollector));
                builder.Length = 0;
            }

            return new ConvertionResult(
                documents,
                await imageCollector.GetCollectedImagesAsync(this.httpClient));
        }

        public async Task<ConvertionResult> ConvertAsync(Uri url)
        {
            var builder = new StringBuilder();
            var imageCollector = new ImageCollector(this.logger);

            var document = await this.ConvertAsync(url, builder, imageCollector);

            return new ConvertionResult(
                new[] { document },
                await imageCollector.GetCollectedImagesAsync(this.httpClient));
        }

        private async Task<ConvertedDocument> ConvertAsync(Uri pageUri, StringBuilder builder, ImageCollector imageCollector)
        {
            this.logger.LogInformation("Loading page content for {PageUri}", pageUri);
            var content = await this.httpClient.GetStringAsync(pageUri);
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var frontMatter = this.frontMatterExtractor.Extract(this.options.FrontMatter, doc, pageUri);
            if (frontMatter != null)
            {
                builder.Append(frontMatter);
            }

            this.logger.LogDebug("Processing page content");
            this.ProcessNode(pageUri, doc.DocumentNode, builder, imageCollector, false);

            return new ConvertedDocument(pageUri, this.RemoveRedundantWhiteSpace(builder.ToString()));
        }

        private string RemoveRedundantWhiteSpace(string text)
        {
            return Regex.Replace(text, "(^" + Environment.NewLine + "){2,}", Environment.NewLine, RegexOptions.Multiline);
        }

        private void ProcessNode(
            Uri pageUri,
            HtmlNode node, 
            StringBuilder builder, 
            ImageCollector imageCollector, 
            bool emitText, 
            string listItemType = "-")
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
                    if (!this.options.ExcludeTags.Contains(node.Name))
                    {
                        var emitNewLineAfterChildren = false;
                        emitText = emitText || this.IsIncludedTag(node.Name);
                        if (emitText)
                        {
                            switch (node.Name)
                            {
                                case "table":
                                    this.EmitTable(pageUri, node, builder, imageCollector);
                                    this.EmitNewLine(builder);
                                    return;

                                case "img":
                                    this.EmitImage(pageUri, node, builder, imageCollector);
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
                                    this.EmitNewLine(builder);
                                    emitNewLineAfterChildren = true;
                                    builder.Append('#', node.Name[1] - '0').Append(' ');
                                    break;

                                case "a":
                                    this.EmitLink(pageUri, node, builder, imageCollector);
                                    return;

                                case "i":
                                case "em":
                                    this.EmitFormattedText(node, builder, "*");
                                    return;

                                case "b":
                                case "strong":
                                    this.EmitFormattedText(node, builder, "**");
                                    return;

                                case "pre":
                                    this.EmitPreformattedText(node, builder);
                                    return;
                            }
                        }

                        this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, emitText, listItemType);

                        if (emitNewLineAfterChildren)
                        {
                            this.EmitNewLine(builder);
                        }
                    }

                    break;
            }
        }

        private bool IsIncludedTag(string tagName)
        {
            return this.options.IncludeTags.Count == 0 || this.options.IncludeTags.Contains(tagName);
        }

        private void ProcessChildNodes(
            Uri pageUri,
            HtmlNodeCollection nodes, 
            StringBuilder builder, 
            ImageCollector imageCollector, 
            bool emitText, 
            string listItemType = "-")
        {
            foreach (var childNode in nodes)
            {
                this.ProcessNode(pageUri, childNode, builder, imageCollector, emitText, listItemType);
            }
        }

        private void EmitImage(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
        {
            var src = node.GetAttributeValue("src", null);
            var alt = node.GetAttributeValue("alt", "image");

            if (src == null)
            {
                this.logger.LogWarning("No src attribute for image - it will not be emitted: {NodeHtml}", node.OuterHtml);
            }
            else
            {
                var imageUri = src;
                if (imageCollector.CanCollect(pageUri, src))
                {
                    imageUri = this.BuildImagePath(imageCollector.Collect(pageUri, src));
                }

                builder.Append("![").Append(alt).Append("](").Append(imageUri).Append(')');
            }
        }

        private void EmitTable(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
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
                    this.logger.LogWarning("Table row has different number of columns to header - output will likely be malformed");
                }

                foreach (var cell in cells)
                {
                    builder.Append("|");
                    this.ProcessNode(pageUri, cell, builder, imageCollector, true);
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
            foreach (var className in node.GetClasses())
            {
                if (this.options.CodeLanguageClassMap.TryGetValue(className, out var language))
                {
                    this.EmitPreformattedText(node, builder, language);
                    return;
                }
            }

            if (node.HasClass("code"))
            {
                this.EmitPreformattedText(node, builder, this.options.DefaultCodeLanguage);
            }
            else
            {
                this.EmitPreformattedText(node, builder, string.Empty);
            }
        }

        private void EmitPreformattedText(HtmlNode node, StringBuilder builder, string language)
        {
            this.EmitNewLine(builder);

            builder.Append("```");
            if (language.Length > 0)
            {
                builder.Append(" ");
            }

            builder.AppendLine(language)
                .AppendLine(HttpUtility.HtmlDecode(this.ExtractText(node, trimNewLines: true, escapeMarkDownCharacters: false)))
                .AppendLine("```");
        }

        private void EmitFormattedText(HtmlNode node, StringBuilder builder, string wrapWith)
        {
            builder
                .Append(wrapWith)
                .Append(this.ExtractText(node))
                .Append(wrapWith);
        }

        private string ExtractText(HtmlNode node, bool trimNewLines = false, bool escapeMarkDownCharacters = true)
        {
            var text = node.InnerText;
            if (trimNewLines)
            {
                text = text.Trim('\r', '\n');
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (escapeMarkDownCharacters)
            {
                text = Regex.Replace(text, "[\\\\`*_{}\\[\\]()#+-.!]", "\\$0");
            }

            return text;
        }

        private void EmitLink(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector)
        {
            var href = node.GetAttributeValue("href", null);
            if (href == null)
            {
                this.logger.LogWarning("Anchor has missing href and will be rendered as text: {NodeHtml}", node.OuterHtml);
                this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, true);
            }
            else
            {
                // Handle special case where the link is to an image - we'll download that to the images folder
                // as if it was an img tag
                if (contentTypeProvider.TryGetContentType(href, out var contentType) && contentType.StartsWith("image/"))
                {
                    if (imageCollector.CanCollect(pageUri, href))
                    {
                        href = this.BuildImagePath(imageCollector.Collect(pageUri, href));
                    }
                }

                builder.Append($"[");
                this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, true);
                builder.Append("](").Append(href).Append(')');
            }
        }

        private string BuildImagePath(string fileName)
        {
            return Path.Combine(this.options.ImagePathPrefix, fileName);
        }
    }
}
