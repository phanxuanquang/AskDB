using AskDB.App.Local_Controls.Charts.Factories;
using AskDB.App.View_Models;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Data;

namespace AskDB.App.Converters
{
    public partial class DataVisualizationInfoToChartVisualizationInfo : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DataVisualizationInfo dataVisualizationInfo)
            {
                if (dataVisualizationInfo.DataSet == null || dataVisualizationInfo.DataSet.Rows.Count == 0)
                {
                    throw new ArgumentNullException(nameof(dataVisualizationInfo.DataSet), "DataVisualizationInfo or DataSet cannot be null or empty.");
                }

                if (!dataVisualizationInfo.DataSet.Columns.Contains(dataVisualizationInfo.XAxisName) || !dataVisualizationInfo.DataSet.Columns.Contains(dataVisualizationInfo.YAxisName))
                {
                    throw new ArgumentException("Horizontal or Vertical axis columns are missing.", nameof(dataVisualizationInfo.DataSet));
                }

                var labels = new List<string>();
                var values = new List<double>();

                foreach (DataRow row in dataVisualizationInfo.DataSet.Rows)
                {
                    var rowValue = row[dataVisualizationInfo.XAxisName];

                    var rowValueType = rowValue.GetType();

                    if (rowValueType == typeof(DateTime))
                    {
                        labels.Add(((DateTime)rowValue).ToLongDateString());
                    }
                    else if (rowValueType == typeof(DateTimeOffset))
                    {
                        labels.Add(((DateTimeOffset)rowValue).ToString("yyyy-MM-dd HH:mm:ss zzz"));
                    }
                    else
                    {
                        labels.Add(rowValue.ToString());
                    }

                    values.Add(System.Convert.ToDouble(row[dataVisualizationInfo.YAxisName]));
                }

                var series = dataVisualizationInfo.SeriesType.CreateSeries(values, dataVisualizationInfo.YAxisName);

                return new ChartVisualizationInfo
                {
                    XAxes = new([new Axis
                    {
                        Labels = labels,
                        Name = dataVisualizationInfo.XAxisName
                    }]),
                    YAxes = new([new Axis
                    {
                        Labels = labels,
                        Name = dataVisualizationInfo.YAxisName
                    }]),
                    Series = new([series])
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
