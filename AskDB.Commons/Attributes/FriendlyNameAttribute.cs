namespace AskDB.Commons.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FriendlyNameAttribute(string displayName) : Attribute
    {
        public string FriendlyName { get; } = displayName;
    }
}
