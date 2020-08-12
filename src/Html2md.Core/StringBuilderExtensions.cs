using System.Text;

namespace Html2md
{
    public static class StringBuilderExtensions
    {
        public static bool EndsWithNewLine(this StringBuilder builder)
        {
            if (builder.Length == 0)
            {
                return false;
            }

            return builder[^1] == '\n';
        }
    }
}
