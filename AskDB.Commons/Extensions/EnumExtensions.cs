using System.ComponentModel;

namespace AskDB.Commons.Extensions
{
    public static class EnumExtensions
    {
        public static T? GetAttributeValue<T>(this Enum enumValue) where T : Attribute
        {
            // Lấy kiểu enum
            var type = enumValue.GetType();

            // Lấy tên của enum value (ví dụ: SqlServer)
            var memberInfo = type.GetMember(enumValue.ToString());

            if (memberInfo.Length == 0)
                return null;

            // Lấy attribute theo kiểu T
            var attribute = memberInfo[0]
                .GetCustomAttributes(typeof(T), false)
                .FirstOrDefault() as T;

            return attribute;
        }

        public static IEnumerable<Enum> GetValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<Enum>();
        }

        public static string GetDescription(this Enum enumValue)
        {
            var attr = enumValue.GetAttributeValue<DescriptionAttribute>();
            return attr?.Description ?? enumValue.ToString();
        }
    }
}
