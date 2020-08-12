namespace Html2md
{
    /// <summary>
    /// Describes how the data for a Front Matter property should be generated.
    /// </summary>
    public class PropertyMatchExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMatchExpression"/> class.
        /// </summary>
        /// <param name="xpathOrMacro">The xpath or macro for the property.</param>
        /// <param name="dataType">The type of the data.</param>
        public PropertyMatchExpression(string xpathOrMacro)
            : this(xpathOrMacro, PropertyDataType.Any)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMatchExpression"/> class.
        /// </summary>
        /// <param name="xpathOrMacro">The xpath or macro for the property.</param>
        /// <param name="dataType">The type of the data.</param>
        public PropertyMatchExpression(string xpathOrMacro, PropertyDataType dataType)
        {
            this.XpathOrMacro = xpathOrMacro;
            this.DataType = dataType;
        }

        public string XpathOrMacro { get; }
        public PropertyDataType DataType { get; }
    }
}
