using System.Text;

namespace DatabaseAnalyzer.Models
{
    public class Column
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public string? DefaultValue { get; set; }
        public string? PrimaryKey { get; set; }
        public string? ForeignKeyName { get; set; }
        public string? ParentColumn { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            var stringDataTypes = new List<string>()
            {
                "VARCHAR",
                "CHAR",
                "NVARCHAR",
                "NCHAR"
            };

            stringBuilder.Append($"{Name} {DataType.ToUpper()}");

            if (stringDataTypes.Contains(DataType.ToUpper()) && MaxLength.HasValue)
            {
                stringBuilder.Append($"({MaxLength.Value})");
            }

            if (PrimaryKey != null)
            {
                stringBuilder.Append($" PRIMARY KEY");
            }

            if (!IsNullable)
            {
                stringBuilder.Append(" NOT NULL");
            }

            if (DefaultValue != null)
            {
                stringBuilder.Append($" DEFAULT {DefaultValue}");
            }

            if (ForeignKeyName != null)
            {
                stringBuilder.Append($" REFERENCES {ReferencedTable}({ReferencedColumn})");
            }

            return stringBuilder.ToString();
        }
    }
}
