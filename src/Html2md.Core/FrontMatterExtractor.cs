using HtmlAgilityPack;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Html2md
{
    public class FrontMatterExtractor
    {
        public string? Extract(FrontMatterOptions options, HtmlDocument document, Uri pageUri)
        {
            if (!options.Enabled)
            {
                return null;
            }

            var builder = new StringBuilder();
            builder.AppendLine(options.Delimiter);

            foreach (var singleValue in options.SingleValueProperties)
            {
                builder
                    .Append(singleValue.Key)
                    .Append(": ")
                    .AppendLine(ExtractValue(singleValue.Value, document, pageUri));
            }

            foreach (var singleValue in options.ArrayValueProperties)
            {
                builder
                    .Append(singleValue.Key)
                    .AppendLine(":");

                foreach (var match in document.DocumentNode.SelectNodes(singleValue.Value))
                {
                    builder
                        .Append("  - ")
                        .AppendLine(match.GetDirectInnerText().Trim());
                }
            }

            builder.AppendLine(options.Delimiter);

            return builder.ToString();
        }

        private static string ExtractValue(string xpathOrMacro, HtmlDocument document, Uri pageUri)
        {
            if (xpathOrMacro.StartsWith("{{"))
            {
                if (Regex.IsMatch(xpathOrMacro, @"^{{'[^']*'}}$"))
                {
                    return xpathOrMacro.Substring(3, xpathOrMacro.Length - 6);
                }

                return xpathOrMacro switch
                {
                    "{{RelativeUriPath}}" => pageUri.LocalPath,
                    _ => throw new Exception("Unknown macro " + xpathOrMacro),
                };
            }
            else
            {
                var node = document.DocumentNode.SelectSingleNode(xpathOrMacro);
                var attributeName = Regex.Match(xpathOrMacro, @"/@(\w+)$");
                if (attributeName.Success)
                {
                    return node.GetAttributeValue(attributeName.Groups[1].Value, string.Empty).Trim();
                }

                return node.GetDirectInnerText().Trim();
            }
        }
    }
}
