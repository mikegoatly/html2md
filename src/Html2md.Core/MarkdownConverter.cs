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
        private readonly List<string> includeXPaths;
        private readonly HashSet<string> includeTags;
        private readonly List<string> excludeXPaths;
        private readonly FrontMatterExtractor frontMatterExtractor = new FrontMatterExtractor();
        private readonly HashSet<string> excludeTags;

        public MarkdownConverter(IConversionOptions options, ILogger? logger = null)
            : this(options, null, logger)
        {
        }

        public MarkdownConverter(IConversionOptions options, HttpClient? httpClient = null, ILogger? logger = null)
        {
            this.options = options;
            this.logger = logger ?? NullLogger.Instance;
            this.httpClient = httpClient ?? new HttpClient();

            this.includeXPaths = this.options.IncludeTags.Where(t => t.StartsWith("/")).ToList();
            this.includeTags = this.options.IncludeTags.Except(this.includeXPaths).ToHashSet();

            this.excludeXPaths = this.options.ExcludeTags.Where(t => t.StartsWith("/")).ToList();
            this.excludeTags = this.options.ExcludeTags.Except(this.includeXPaths).ToHashSet();
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

            this.logger.LogTrace("Building list of explicitly included elements");
            var nodesToProcess = this.includeXPaths.SelectMany(p => doc.DocumentNode.SelectNodes(p) ?? Enumerable.Empty<HtmlNode>()).ToList();
            var nodesToExclude = this.excludeXPaths.SelectMany(p => doc.DocumentNode.SelectNodes(p) ?? Enumerable.Empty<HtmlNode>()).ToHashSet();

            if (nodesToProcess.Count == 0)
            {
                nodesToProcess.Add(doc.DocumentNode);
            }

            var index = 0;
            foreach (var node in nodesToProcess)
            {
                this.ProcessNode(pageUri, node, builder, imageCollector, ConversionState.InitialState(nodesToExclude));
                if (++index != nodesToProcess.Count)
                {
                    builder.AppendLine().AppendLine();
                }
            }

            return new ConvertedDocument(pageUri, this.RemoveRedundantWhiteSpace(builder.ToString()));
        }

        private string RemoveRedundantWhiteSpace(string text)
        {
            text = Regex.Replace(text, "(^" + Environment.NewLine + "){2,}", Environment.NewLine, RegexOptions.Multiline);
            text = Regex.Replace(text, "(^```.*)(" + Environment.NewLine + "){2,}", "$1" + Environment.NewLine, RegexOptions.Multiline);
            return text;
        }

        private void ProcessNode(
            Uri pageUri,
            HtmlNode node,
            StringBuilder builder,
            ImageCollector imageCollector,
            ConversionState state)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    if (state.RenderingEnabled)
                    {
                        builder.Append(this.ExtractText(node, state));
                    }

                    break;

                case HtmlNodeType.Document:
                case HtmlNodeType.Element:
                    if (!this.excludeTags.Contains(node.Name) && !state.NodesToExclude.Contains(node))
                    {
                        var emitNewLineAfterChildren = false;
                        if (this.IsIncludedTag(node.Name))
                        {
                            state = state.WithRenderingEnabled();
                        }

                        ConversionState? childState = null;
                        if (state.RenderingEnabled)
                        {
                            if (state.EmitMarkDownStyles)
                            {
                                switch (node.Name)
                                {
                                    case "table":
                                        this.EmitTable(pageUri, node, builder, imageCollector, state);
                                        this.EmitNewLine(builder, state);
                                        return;

                                    case "img":
                                        this.EmitImage(pageUri, node, builder, imageCollector);
                                        return;

                                    case "p":
                                        emitNewLineAfterChildren = true;
                                        break;

                                    case "br":
                                        this.EmitNewLine(builder, state, 1);
                                        break;

                                    case "blockquote":
                                        childState = state.WithLinePrefix(state.LinePrefix ?? "" + ">");
                                        this.EmitNewLine(builder, state);
                                        builder.Append(childState?.LinePrefix).Append(" ");
                                        emitNewLineAfterChildren = true;
                                        break;

                                    case "ul":
                                        state = state.StartUnorderedList();
                                        emitNewLineAfterChildren = state.ListDepth == 1;
                                        break;

                                    case "ol":
                                        state = state.StartOrderedList();
                                        emitNewLineAfterChildren = state.ListDepth == 1;
                                        break;

                                    case "li":
                                        this.EmitListItemPrefix(builder, state);
                                        break;

                                    case "h1":
                                    case "h2":
                                    case "h3":
                                    case "h4":
                                    case "h5":
                                    case "h6":
                                        this.EmitNewLine(builder, state);
                                        emitNewLineAfterChildren = true;
                                        builder.Append('#', node.Name[1] - '0').Append(' ');
                                        break;

                                    case "a":
                                        this.EmitLink(pageUri, node, builder, imageCollector, state);
                                        return;

                                    case "i":
                                    case "em":
                                        this.EmitFormattedText(node, builder, "*", state);
                                        return;

                                    case "b":
                                    case "strong":
                                        this.EmitFormattedText(node, builder, "**", state);
                                        return;

                                    case "pre":
                                        this.EmitPreformattedText(pageUri, node, builder, imageCollector, state);
                                        return;
                                }
                            }
                            else
                            {
                                switch (node.Name)
                                {
                                    case "br":
                                        builder.AppendLine();
                                        break;
                                }
                            }
                        }

                        this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, childState ?? state);

                        if (emitNewLineAfterChildren)
                        {
                            this.EmitNewLine(builder, state);
                        }
                    }

                    break;
            }
        }

        private bool IsIncludedTag(string tagName)
        {
            return this.includeTags.Count == 0 || this.includeTags.Contains(tagName);
        }

        private void ProcessChildNodes(
            Uri pageUri,
            HtmlNodeCollection nodes,
            StringBuilder builder,
            ImageCollector imageCollector,
            ConversionState state)
        {
            foreach (var childNode in nodes)
            {
                this.ProcessNode(pageUri, childNode, builder, imageCollector, state);
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

        private void EmitTable(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector, ConversionState state)
        {
            this.EmitNewLine(builder, state);

            var (headers, skipFirstRow) = this.GetTableHeaders(node, state);

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
                    this.ProcessNode(pageUri, cell, builder, imageCollector, state.WithAllNewLinesStripped());
                }

                builder.AppendLine("|");
            }
        }

        private (IReadOnlyList<string> headers, bool skipFirstRow) GetTableHeaders(HtmlNode node, ConversionState state)
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
                        .Select(n => this.ExtractText(n, state))
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

        private void EmitListItemPrefix(StringBuilder builder, ConversionState state)
        {
            builder.AppendLine();

            if (state.ListDepth > 1)
            {
                builder.Append(' ', 4 * (state.ListDepth - 1));
            }

            builder.Append(state.ListItemPrefix ?? "-")
                .Append(" ");
        }

        private void EmitNewLine(StringBuilder builder, ConversionState state, int count = 2)
        {
            if (state.LinePrefix != null)
            {
                if (state.PreventNewLines)
                {
                    this.logger.LogWarning("New lines are being emitted in an unexpected context (e.g. a list in a table cell) - output is likely malformed.");
                }

                builder.AppendLine();

                for (var i = 0; i < count - 1; i++)
                {
                    builder.Append(state.LinePrefix)
                        .AppendLine(" ");
                }

                builder.Append(state.LinePrefix)
                    .Append(' ');
            }
            else
            {
                if (state.PreventNewLines)
                {
                    if (builder[^1] != ' ')
                    {
                        builder.Append(' ');
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        builder.AppendLine();
                    }
                }
            }
        }

        private void EmitPreformattedText(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector, ConversionState state)
        {
            foreach (var className in node.GetClasses())
            {
                if (this.options.CodeLanguageClassMap.TryGetValue(className, out var language))
                {
                    this.EmitPreformattedText(pageUri, node, builder, imageCollector, state, language);
                    return;
                }
            }

            if (node.HasClass("code"))
            {
                this.EmitPreformattedText(pageUri, node, builder, imageCollector, state, this.options.DefaultCodeLanguage);
            }
            else
            {
                this.EmitPreformattedText(pageUri, node, builder, imageCollector, state, string.Empty);
            }
        }

        private void EmitPreformattedText(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector, ConversionState state, string language)
        {
            this.EmitNewLine(builder, state);

            builder.Append("```");
            if (language.Length > 0)
            {
                builder.Append(" ");
            }

            builder.AppendLine(language);

            this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, state.StartPreformattedTextBlock());

            if (!builder.EndsWithNewLine())
            {
                builder.AppendLine();
            }

            builder.AppendLine("```");
        }

        private void EmitFormattedText(HtmlNode node, StringBuilder builder, string wrapWith, ConversionState state)
        {
            builder
                .Append(wrapWith)
                .Append(this.ExtractText(node, state))
                .Append(wrapWith);
        }

        private string ExtractText(HtmlNode node, ConversionState state)
        {
            var text = node.InnerText;

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (state.EmitMarkDownStyles)
            {
                // Escape markdown characters that may clash
                text = Regex.Replace(text, "[\\\\`*_{}\\[\\]()#+-.!]", "\\$0");
            }

            if (state.PreventNewLines)
            {
                text = text.Replace(Environment.NewLine, " ");
            }

            text = HttpUtility.HtmlDecode(text);

            return text;
        }

        private void EmitLink(Uri pageUri, HtmlNode node, StringBuilder builder, ImageCollector imageCollector, ConversionState state)
        {
            var href = node.GetAttributeValue("href", null);
            if (href == null)
            {
                this.logger.LogWarning("Anchor has missing href and will be rendered as text: {NodeHtml}", node.OuterHtml);
                this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, state);
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
                this.ProcessChildNodes(pageUri, node.ChildNodes, builder, imageCollector, state);
                builder.Append("](").Append(href).Append(')');
            }
        }

        private string BuildImagePath(string fileName)
        {
            return Path.Combine(this.options.ImagePathPrefix, fileName);
        }
    }
}
