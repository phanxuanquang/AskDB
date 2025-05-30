namespace DatabaseInteractor.FunctionCallings.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NameAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}
