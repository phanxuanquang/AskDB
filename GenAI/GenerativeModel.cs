using System.ComponentModel;

namespace GenAI
{
    public enum GenerativeModel
    {
        [Description("gemini-1.5-pro-latest")]
        Gemini_15_Pro = 1,

        [Description("gemini-1.5-flash-latest")]
        Gemini_15_Flash = 2,
    }
}
