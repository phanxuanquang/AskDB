using AskDB.Commons.Attributes;
using AskDB.Commons.Enums;
using System.ComponentModel;

namespace AskDB.Commons.Extensions
{
    public static class EnumExtensions
    {
        public static T? GetAttributeValue<T>(this Enum value) where T : Attribute
        {
            var memberInfo = value.GetType().GetMember(value.ToString());

            if (memberInfo.Length == 0) return null;

            return memberInfo[0].GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
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

        public static string GetFriendlyName(this Enum enumValue)
        {
            var attr = enumValue.GetAttributeValue<FriendlyNameAttribute>();
            return attr?.FriendlyName ?? enumValue.ToString();
        }
    }
}
