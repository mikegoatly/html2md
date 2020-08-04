using System.Collections.Generic;

namespace Html2md
{
    /// <inheritdoc />
    public class ConversionOptions : IConversionOptions
    {
        /// <inheritdoc />
        public string ImagePathPrefix { get; set; } = "";

        /// <inheritdoc />
        public string DefaultCodeLanguage { get; set; } = "csharp";

        /// <inheritdoc />
        public ISet<string> IncludeTags { get; set; } = new HashSet<string>();

        /// <inheritdoc />
        public ISet<string> ExcludeTags { get; set; } = new HashSet<string>();

        /// <inheritdoc />
        public IDictionary<string, string> CodeLanguageClassMap { get; set; } = new Dictionary<string, string>();
    }
}
