using System.ComponentModel;

namespace GenAI
{
    public enum GenerativeModel
    {
        [Description("gemini-1.0-pro")]
        Gemini_10_Pro = 1,

        [Description("gemini-1.5-pro-latest")]
        Gemini_15_Pro = 2,

        [Description("gemini-1.5-flash-latest")]
        Gemini_15_Flash = 3,

        [Description("gemini-2.0-flash-lite-preview-02-05")]
        Gemini_20_Flash_Lite = 4,

        [Description("gemini-2.0-flash")]
        Gemini_20_Flash = 5,

        [Description("gemini-2.0-flash-thinking-exp-01-21")]
        Gemini_20_Flash_Thinking = 6,
    }
}
