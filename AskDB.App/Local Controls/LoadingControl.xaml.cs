using AskDB.App.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AskDB.App
{
    public sealed partial class LoadingControl : UserControl, INotifyPropertyChanged
    {
        private string? _message;
        private int _size;

        public string? Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }
        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public LoadingControl()
        {
            this.InitializeComponent();
        }

        public void SetLoading(string? message, bool isActivated, int size = 50)
        {
            Message = message;
            Size = size;
            LoadingPanel.Visibility = VisibilityHelper.SetVisible(isActivated);
        }
    }
}
