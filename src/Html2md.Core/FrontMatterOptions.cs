﻿using System.Collections.Generic;

namespace Html2md
{
    /// <summary>
    /// Configuration for writing Front Matter sections to converted documents.
    /// </summary>
    public class FrontMatterOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether Front Matter should be written to converted documents.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the XPath or macro properties that should be written to the Front Matter section.
        /// If an XPath is provided and more than one element matches, then the first is used.
        /// </summary>
        public Dictionary<string, PropertyMatchExpression> SingleValueProperties { get; set; } = new Dictionary<string, PropertyMatchExpression>();

        /// <summary>
        /// Gets or sets the XPath properties that should be written to the Front Matter section as a list. Each matching
        /// value will be written as an entry in the list.
        /// </summary>
        public Dictionary<string, PropertyMatchExpression> ArrayValueProperties { get; set; } = new Dictionary<string, PropertyMatchExpression>();
    }
}
