using AskDB.App.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace AskDB.App
{
    public sealed partial class LoadingControl : UserControl
    {
        private string? _message;
        private int _size;
        public LoadingControl()
        {
            this.InitializeComponent();
        }

        public void SetLoading(string? message, bool isActivated, int size = 50)
        {
            _message = message;
            _size = size;
            LoadingPanel.Visibility = VisibilityHelper.SetVisible(isActivated);
        }
    }
}
