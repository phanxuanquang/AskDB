using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace AskDB.App.Local_Controls.AIProviderConnections
{
    public sealed partial class StandardAIProviderConnection : UserControl, INotifyPropertyChanged
    {
        private string _apiKey;

        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (_apiKey != value)
                {
                    _apiKey = value.Trim();
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StandardAIProviderConnection()
        {
            InitializeComponent();
        }
    }
}
