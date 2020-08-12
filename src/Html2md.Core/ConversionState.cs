using HtmlAgilityPack;
using System.Collections.Generic;

namespace Html2md
{
    internal struct ConversionState
    {
        private ConversionState(ConversionState previous)
        {
            this.RenderingEnabled = previous.RenderingEnabled;
            this.ListDepth = previous.ListDepth;
            this.ListItemPrefix = previous.ListItemPrefix;
            this.EmitMarkDownStyles = previous.EmitMarkDownStyles;
            this.LinePrefix = previous.LinePrefix;
            this.NodesToExclude = previous.NodesToExclude;
        }

        public string? ListItemPrefix { get; private set; }

        public bool RenderingEnabled { get; private set; }

        public bool EmitMarkDownStyles { get; private set; }

        public int ListDepth { get; private set; }

        public string? LinePrefix { get; private set; }
        public ISet<HtmlNode> NodesToExclude { get; private set; }

        public static ConversionState InitialState(ISet<HtmlNode> nodesToExclude)
        {
            return new ConversionState
            {
                NodesToExclude = nodesToExclude,
                EmitMarkDownStyles = true
            };
        }

        public ConversionState WithRenderingEnabled()
        {
            if (this.RenderingEnabled)
            {
                return this;
            }

            return new ConversionState(this) { RenderingEnabled = true };
        }

        public ConversionState StartPreformattedTextBlock()
        {
            return new ConversionState(this)
            {
                EmitMarkDownStyles = false
            };
        }

        public ConversionState StartOrderedList()
        {
            return new ConversionState(this)
            {
                ListDepth = this.ListDepth + 1,
                ListItemPrefix = "1."
            };
        }

        public ConversionState StartUnorderedList()
        {
            return new ConversionState(this)
            {
                ListDepth = this.ListDepth + 1,
                ListItemPrefix = "-"
            };
        }

        public ConversionState WithLinePrefix(string prefix)
        {
            return new ConversionState(this)
            {
                LinePrefix = prefix
            };
        }
    }
}
