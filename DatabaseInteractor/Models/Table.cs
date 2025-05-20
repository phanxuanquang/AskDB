using System.Text;

namespace DatabaseInteractor.Models
{
    public class Table
    {
        public string? Schema { get; set; }
        public required string Name { get; set; }
        public List<Column> Columns { get; set; } = [];

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Schema))
            {
                sb.Append($"CREATE TABLE [{Schema}].[{Name}] (");
            }
            else
            {
                sb.Append($"CREATE TABLE [{Name}] (");
            }

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine(",");
                }
                sb.Append($"    {Columns[i].ToString()}");
            }

            sb.AppendLine();
            sb.AppendLine(");");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
