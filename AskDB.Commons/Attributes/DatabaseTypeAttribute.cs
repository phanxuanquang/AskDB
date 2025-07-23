namespace AskDB.Commons.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultPortAttribute(int port) : Attribute
    {
        public int Port { get; } = port;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultHostAttribute(string host) : Attribute
    {
        public string Host { get; } = host;
    }
}
