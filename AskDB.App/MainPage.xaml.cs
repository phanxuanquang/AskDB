using DatabaseAnalyzer.Extractors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            IDatabaseExtractor extractor = new SqlServerExtractor();
            var MyDataTable = await extractor.GetData("Server=localhost;Database=CompanyDB;Integrated Security = True;", "Select * from Employees");

            for (int i = 0; i < MyDataTable.Columns.Count; i++)
            {
                myDataGrid.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn()
                {
                    Header = MyDataTable.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }

            var collectionObjects = new System.Collections.ObjectModel.ObservableCollection<object>();
            foreach (DataRow row in MyDataTable.Rows)
            {
                collectionObjects.Add(row.ItemArray);
            }
            myDataGrid.ItemsSource = collectionObjects;
        }
    }
}
