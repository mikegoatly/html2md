using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace html2md
{
    public class Converter
    {
        private readonly CommandLineArgs args;

        public Converter(CommandLineArgs args)
        {
            this.args = args;
        }

        public async Task<string> ConvertAsync(string url)
        {
            var client = new HttpClient();
            var content = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            var builder = new StringBuilder();

            doc.LoadHtml(content);

            this.ProcessNode(doc.DocumentNode, builder, false);

            return RemoveRedundantWhiteSpace(builder.ToString());
        }

        private string RemoveRedundantWhiteSpace(string text)
        {
            text = Regex.Replace(text, "(^" + Environment.NewLine + "){2,}", Environment.NewLine, RegexOptions.Multiline);
            return Regex.Replace(text, " {2,}", " ");
        }

        private void ProcessNode(HtmlNode node, StringBuilder builder, bool emitText, string listItemType = "-")
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
                                    EmitTable(node, builder);
                                    EmitNewLine(builder);
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
                                    this.EmitLink(node, builder);
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

                        foreach (var childNode in node.ChildNodes)
                        {
                            this.ProcessNode(childNode, builder, emitText, listItemType);
                        }

                        if (emitNewLineAfterChildren)
                        {
                            this.EmitNewLine(builder);
                        }
                    }

                    break;
            }


        }

        private void EmitTable(HtmlNode node, StringBuilder builder)
        {
            EmitNewLine(builder);

            var (headers, skipFirstRow) = GetTableHeaders(node);

            foreach (var header in headers)
            {
                builder.Append("|").Append(header);
            }

            builder.AppendLine("|");

            EmitRepeated(builder, "|-", headers.Count, "|");

            IEnumerable<HtmlNode> rows = GetTableRows(node);
            if (skipFirstRow)
            {
                rows = rows.Skip(1);
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells.Count != headers.Count)
                {
                    Warn("Table row has different number of columns to header - output will likely be malformed");
                }

                foreach (var cell in cells)
                {
                    builder.Append("|");
                    ProcessNode(cell, builder, true);
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
                        .Select(n => ExtractText(n))
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

        private void EmitLink(HtmlNode node, StringBuilder builder)
        {
            var href = node.GetAttributeValue("href", null);
            var text = this.ExtractText(node);

            if (text.Length == 0)
            {
                this.Warn("Anchor has no content an will not be rendered: " + node.OuterHtml);
                return;
            }

            if (href == null)
            {
                this.Warn("Anchor has missing href and will be rendered as text: " + node.OuterHtml);
                builder.Append(text);
            }

            builder.Append($"[{text}]({href})");
        }

        private void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
