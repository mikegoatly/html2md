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
                var value = ExtractValue(singleValue.Value, document, pageUri);
                if (value != null)
                {
                    builder
                        .Append(singleValue.Key)
                        .Append(": ")
                        .AppendLine(value);
                }
            }

            foreach (var arrayValue in options.ArrayValueProperties)
            {
                var matches = document.DocumentNode.SelectNodes(arrayValue.Value);
                if (matches == null)
                {
                    continue;
                }

                builder
                    .Append(arrayValue.Key)
                    .AppendLine(":");

                foreach (var match in matches)
                {
                    builder
                        .Append("  - ")
                        .AppendLine(match.GetDirectInnerText().Trim());
                }
            }

            builder.AppendLine(options.Delimiter);

            return builder.ToString();
        }

        private static string? ExtractValue(string xpathOrMacro, HtmlDocument document, Uri pageUri)
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
                if (node == null)
                {
                    return null;
                }

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
