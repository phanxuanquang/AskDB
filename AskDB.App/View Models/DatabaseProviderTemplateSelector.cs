using AskDB.Commons.Enums;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskDB.App.View_Models
{
    public partial class DatabaseProviderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StandardConnectionTemplate { get; set; } = null!;

        public DataTemplate AssistantTemplate { get; set; } = null!;

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            
            return AssistantTemplate;
        }
    }
}
