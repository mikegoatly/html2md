using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
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

        public async Task ExecuteAsync()
        {
            var client = new HttpClient();
            var content = await client.GetStringAsync(args.Url);
            var doc = new HtmlDocument();
            var builder = new StringBuilder();

            doc.LoadHtml(content);

            ProcessNode(doc.DocumentNode, builder, false);

            Console.WriteLine(builder.ToString());
        }

        private void ProcessNode(HtmlNode node, StringBuilder builder, bool emitText)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    if (emitText)
                    {
                        builder.Append(node.InnerText.Trim());
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
                                case "p":
                                    emitNewLineAfterChildren = true;
                                    break;

                                case "h1":
                                case "h2":
                                case "h3":
                                case "h4":
                                case "h5":
                                case "h6":
                                case "h7":
                                    EmitNewLine(builder);
                                    emitNewLineAfterChildren = true;
                                    builder.Append('#', (int)(node.Name[1] - '0')).Append(' ');
                                    break;

                                case "a":
                                    EmitLink(node, builder);
                                    return;

                                case "em":
                                    EmitFormattedText(node, builder, "*");
                                    return;

                                case "strong":
                                    EmitFormattedText(node, builder, "**");
                                    return;

                                case "pre":
                                    EmitPreformattedText(node, builder);
                                    return;
                            }
                        }

                        foreach (var childNode in node.ChildNodes)
                        {
                            ProcessNode(childNode, builder, emitText);
                        }

                        if (emitNewLineAfterChildren)
                        {
                            EmitNewLine(builder);
                        }
                    }

                    break;
            }


        }

        private void EmitNewLine(StringBuilder builder)
        {
            builder.AppendLine().AppendLine();
        }

        private void EmitPreformattedText(HtmlNode node, StringBuilder builder)
        {
            if (node.HasClass("code"))
            {
                EmitPreformattedText(node, builder, this.args.DefaultCodeLanguage);
            }
            else
            {
                EmitPreformattedText(node, builder, string.Empty);
            }
        }

        private void EmitPreformattedText(HtmlNode node, StringBuilder builder, string language)
        {
            EmitNewLine(builder);

            builder
                .Append("``` ")
                .AppendLine(language)
                .AppendLine(HttpUtility.HtmlDecode(node.InnerText.Trim()))
                .AppendLine("```");
        }

        private void EmitFormattedText(HtmlNode node, StringBuilder builder, string wrapWith)
        {
            builder.Append(wrapWith)
                .Append(node.InnerText.Trim())
                .Append(wrapWith);
        }

        private void EmitLink(HtmlNode node, StringBuilder builder)
        {
            var href = node.GetAttributeValue("href", null);
            var text = node.InnerText.Trim();

            if (text.Length == 0)
            {
                Warn("Anchor has no content an will not be rendered: " + node.OuterHtml);
                return;
            }

            if (href == null)
            {
                Warn("Anchor has missing href and will be rendered as text: " + node.OuterHtml);
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
