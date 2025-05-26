namespace AskDB.Commons.Extensions
{
    public static class EnumExtensions
    {
        public static T? GetAttributeValue<T>(this Enum enumValue, string attributeName)
        {
            var fi = enumValue.GetType().GetField(enumValue.ToString());
            var attrs = fi.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                var prop = attr.GetType().GetProperty(attributeName);
                if (prop != null && prop.PropertyType == typeof(T))
                {
                    return (T?)prop.GetValue(attr);
                }
            }
            return default;
        }

        public static IEnumerable<Enum> GetValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<Enum>();
        }
    }
}
