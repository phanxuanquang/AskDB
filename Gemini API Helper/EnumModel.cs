using System.ComponentModel;
using System.Reflection;

namespace Gemini_API_Helper
{
    public enum EnumModel
    {
        [Description("gemini-1.5-flash")]
        Gemini_15_Flash,

        [Description("gemini-1.0-pro")]
        Gemini_10_Pro,

        [Description("gemini-1.5-pro")]
        Gemini_15_Pro,
    }
    public static class EnumHelper
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
