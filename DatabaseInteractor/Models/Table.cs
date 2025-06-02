namespace DatabaseInteractor.Models
{
    public class Table
    {
        public string? Schema { get; set; }
        public required string Name { get; set; }
        public List<Column> Columns { get; set; } = [];
    }
}
