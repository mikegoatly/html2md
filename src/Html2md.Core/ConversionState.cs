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
        }

        public static ConversionState InitialState { get; } = new ConversionState
        {
            EmitMarkDownStyles = true
        };

        public string? ListItemPrefix { get; private set; }

        public bool RenderingEnabled { get; private set; }

        public bool EmitMarkDownStyles { get; private set; }

        public int ListDepth { get; private set; }

        public ConversionState WithRenderingEnabled()
        {
            if (this.RenderingEnabled)
            {
                return this;
            }

            return new ConversionState(this) { RenderingEnabled = true };
        }

        public ConversionState WithoutMarkdownStyling()
        {
            return new ConversionState(this)
            {
                ListDepth = this.ListDepth + 1,
                ListItemPrefix = "1."
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
    }
}
