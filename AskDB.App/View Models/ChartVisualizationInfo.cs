using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using System.Collections.ObjectModel;

namespace AskDB.App.View_Models
{
    public class ChartVisualizationInfo
    {
        public ObservableCollection<ICartesianAxis> XAxes { get; set; } = [];
        public ObservableCollection<ICartesianAxis> YAxes { get; set; } = [];
        public ObservableCollection<ISeries> Series { get; set; } = [];
    }
}
