using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using DatabaseAnalyzer;
using Helper;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TableSelection : Page
    {
        public TableSelection()
        {
            this.InitializeComponent();
            this.Loaded += TableSelection_Loaded;
        }

        private async void TableSelection_Loaded(object sender, RoutedEventArgs e)
        {
            if (Analyzer.DbExtractor.Tables.Count == 0)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please ensure that your database has at least one table with data (not including system tables).", "Empty Database!");
                Frame.GoBack();
            }
            else
            {
                tablesListView.ItemsSource = Analyzer.DbExtractor.Tables.Select(t => t.Name);
                selectAllCheckbox.IsChecked = false;
            }
        }
    }
}
