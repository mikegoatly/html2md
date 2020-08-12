using HtmlAgilityPack;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

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
            builder.AppendLine("---");

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
                var format = arrayValue.Value.DataType;
                var matches = document.DocumentNode.SelectNodes(arrayValue.Value.XpathOrMacro);
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
                        .Append("- ")
                        .AppendLine(FormatValue(match.GetDirectInnerText().Trim(), format));
                }
            }

            builder.AppendLine("---");

            return builder.ToString();
        }

        private static string? ExtractValue(PropertyMatchExpression matchExpression, HtmlDocument document, Uri pageUri)
        {
            var xpathOrMacro = matchExpression.XpathOrMacro;
            string? text = null;
            if (xpathOrMacro.StartsWith("{{"))
            {
                if (Regex.IsMatch(xpathOrMacro, @"^{{'[^']*'}}$"))
                {
                    text = xpathOrMacro[3..^3];
                }
                else
                {
                    text = xpathOrMacro switch
                    {
                        "{{RelativeUriPath}}" => pageUri.LocalPath,
                        _ => throw new Exception("Unknown macro " + xpathOrMacro),
                    };
                }
            }
            else
            {
                var node = document.DocumentNode.SelectSingleNode(xpathOrMacro);
                if (node != null)
                {
                    var attributeName = Regex.Match(xpathOrMacro, @"/@(\w+)$");
                    if (attributeName.Success)
                    {
                        text = node.GetAttributeValue(attributeName.Groups[1].Value, string.Empty).Trim();
                    }
                    else
                    {
                        text = node.GetDirectInnerText().Trim();
                    }
                }
            }

            if (text == null)
            {
                return null;
            }

            return FormatValue(text, matchExpression.DataType);
        }

        private static string FormatValue(string text, PropertyDataType dataType)
        {
            text = dataType switch
            {
                PropertyDataType.Any => text,
                PropertyDataType.Date => DateTime.TryParse(text, out var dateTime)
                    ? dateTime.ToString("O")
                    : text,
                _ => throw new ArgumentException("Unknown property data type: " + dataType, nameof(dataType)),
            };

            text = HttpUtility.HtmlDecode(text);

            return "\"" + text.Replace("\"", "\\\"") + "\"";
        }
    }
}
