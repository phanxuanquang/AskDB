namespace DatabaseInteractor.FunctionCallings.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DescriptionAttribute(string description) : Attribute
    {
        public string Description { get; } = description;
    }
}
