using Microsoft.UI.Xaml;

namespace AskDB.App.Helpers
{
    public static class VisibilityHelper
    {
        public static Visibility SetVisible(bool value)
        {
            return value ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
