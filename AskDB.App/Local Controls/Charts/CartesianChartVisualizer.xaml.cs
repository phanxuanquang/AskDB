using AskDB.App.View_Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace AskDB.App.Local_Controls.Charts
{
    public sealed partial class ChartVisualizer : Page, INotifyPropertyChanged
    {
        private ChartVisualizationInfo _visualizationInfo;

        public ChartVisualizationInfo VisualizationInfo
        {
            get => _visualizationInfo;
            set
            {
                _visualizationInfo = value;
                OnPropertyChanged(nameof(VisualizationInfo));
            }
        }
        public ChartVisualizer()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CartesianChart_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (VisualizationInfo?.Series?.Count > 0)
            {
                if (App.AppTheme == ElementTheme.Light)
                {
                    LiveCharts.Configure(config => config.AddLightTheme());
                }
                else
                {
                    LiveCharts.Configure(config => config.AddDarkTheme());
                }

                CartesianChart.Series = new ObservableCollection<ISeries>(VisualizationInfo.Series);
                CartesianChart.SetBinding(CartesianChart.SeriesProperty, new Binding { Source = VisualizationInfo.Series });
            }
        }
    }
}
