namespace Html2md
{
    /// <summary>
    /// The type of data that a Front Matter property is expected to be.
    /// </summary>
    public enum PropertyDataType
    {
        /// <summary>
        /// The data will be written out as-is.
        /// </summary>
        Any = 0,

        /// <summary>
        /// The input data to the property will be assumed to be convertable to an ISO 8601 date format. If the conversion fails the data
        /// will be written out as-is.
        /// </summary>
        Date = 1
    }
}
