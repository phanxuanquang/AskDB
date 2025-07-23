
namespace AskDB.Commons.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultModelAttribute(string modelId) : Attribute
    {
        public string DefaultModel { get; } = modelId;
    }
}
