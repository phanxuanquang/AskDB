using AskDB.App.Helpers;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    public sealed partial class LoadingControl : UserControl
    {
        public LoadingControl()
        {
            this.InitializeComponent();
        }

        public void SetLoading(string message, bool isActivated, int radius = 50)
        {
            textBlock.Text = message;
            loadingPanel.Visibility = VisibilityHelper.SetVisible(isActivated);
            progressRing.Width = progressRing.Height = radius;
        }
    }
}
