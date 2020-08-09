using System.Collections.Generic;

namespace Html2md
{
    public class FrontMatterOptions
    {
        public bool Enabled { get; set; }

        public string Delimiter { get; set; } = "---";

        public Dictionary<string, string> SingleValueProperties { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> ArrayValueProperties { get; set; } = new Dictionary<string, string>();
    }
}
