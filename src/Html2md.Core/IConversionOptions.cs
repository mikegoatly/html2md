using System.Collections.Generic;

namespace Html2md
{
    /// <summary>
    /// Exposes options that allow for more control over the HTML to markdown process.
    /// </summary>
    public interface IConversionOptions
    {
        /// <summary>
        /// Gets the prefix to apply to all rendered image URLs - helpful when you're going to be serving
        /// images from a different location, relative or absolute.
        /// </summary>
        string ImagePathPrefix { get; }

        /// <summary>
        /// Gets the default code language to apply to code blocks mapped from pre tags. The default is "csharp".
        /// </summary>
        string DefaultCodeLanguage { get; }

        /// <summary>
        /// Get a map between a pre tag's class names and languages. E.g. you might map the class name "sh_csharp" to "csharp" 
        /// and "sh_powershell" to "powershell".
        /// </summary>
        IDictionary<string, string> CodeLanguageClassMap { get; }

        /// <summary>
        /// Gets the set of tags to include in the conversion process. If this is empty then all elements will processed.
        /// </summary>
        ISet<string> IncludeTags { get; }

        /// <summary>
        /// Gets the set of tags to exclude from the conversion process. You can use this if there are certain parts of
        /// a document you don't want translating to markdown, e.g. aside, nav, etc.
        /// </summary>
        ISet<string> ExcludeTags { get; }

        /// <summary>
        /// Gets the FrontMatter configuration to apply to the conversion process.
        /// </summary>
        FrontMatterOptions FrontMatter { get; }
    }
}
