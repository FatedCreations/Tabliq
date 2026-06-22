using System.Text;

namespace Tabliq.Sql.Printer
{
    public class IndentedTextWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();

        private int indentLevel = 0;

        public void Indented(Action action)
        {
            indentLevel++;
            try
            {
                action();
            }
            finally
            {
                indentLevel--;
            }
        }

        public void WriteLine()
        {
            _sb.AppendLine();
            for (int i = 0; i < indentLevel; i++)
            {
                _sb.Append("    "); // Assuming 4 spaces per indent level
            }
        }

        public void Write(string value)
        {
            _sb.Append(value);
        }

        public void Write(object value)
        {
            _sb.Append(value);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
