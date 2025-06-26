using AskDB.App.Local_Controls.Charts.Enums;
using System.Data;

namespace AskDB.App.View_Models
{
    public class DataVisualizationInfo
    {
        public string XAxisName { get; set; }
        public string YAxisName { get; set; }
        public ChartSeriesType SeriesType { get; set; }
        public DataTable DataSet { get; set; }
    }
}
