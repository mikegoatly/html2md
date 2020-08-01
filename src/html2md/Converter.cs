using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
                        builder.Append(node.InnerText);
                    }

                    break;

                case HtmlNodeType.Document:
                case HtmlNodeType.Element:
                    if (!this.args.ExcludeTags.Contains(node.Name))
                    {
                        emitText = emitText || this.args.IncludeTags.Contains(node.Name);
                        foreach (var childNode in node.ChildNodes)
                        {
                            ProcessNode(childNode, builder, emitText);
                        }
                    }

                    break;
            }


        }
    }
}
