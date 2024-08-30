using System.Text;

namespace DatabaseAnalyzer.Models
{
    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; } = new List<Column>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {Name} (");

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine(",");
                }
                sb.Append($"    {Columns[i]}");
            }

            sb.AppendLine();
            sb.AppendLine(");");

            return sb.ToString();
        }
    }
}
