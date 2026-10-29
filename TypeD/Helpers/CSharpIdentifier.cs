using System.Text;

namespace TypeD.Helpers
{
    public static class CSharpIdentifier
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        /// <summary>
        /// Turns a free text name into a valid C# identifier by removing whitespace and illegal characters, and by
        /// prefixing an underscore when it starts with a digit or is a keyword.
        /// </summary>
        public static string Sanitize(string name)
        {
            var builder = new StringBuilder();
            foreach (var character in name ?? string.Empty)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    builder.Append(character);
            }

            if (builder.Length == 0)
                return "_";

            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            var result = builder.ToString();
            return Keywords.Contains(result) ? "_" + result : result;
        }
    }
}
