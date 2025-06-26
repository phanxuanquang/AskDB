using AskDB.App.Local_Controls.Charts.Enums;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;

namespace AskDB.App.Local_Controls.Charts.Factories
{
    public static class CartesianChartSeriesFactory
    {
        public static ISeries CreateSeries(this ChartSeriesType chartType, List<double> values, string name)
        {
            return chartType switch
            {
                ChartSeriesType.Column => new ColumnSeries<double>
                {
                    Values = values,
                    Name = name,
                },
                ChartSeriesType.Line => new LineSeries<double>
                {
                    Values = values,
                    Name = name,
                },
                ChartSeriesType.Scatter => new ScatterSeries<double>
                {
                    Values = values,
                    Name = name,
                },
                _ => throw new NotSupportedException($"The {chartType} chart is not supported."),
            };
        }
    }
}
