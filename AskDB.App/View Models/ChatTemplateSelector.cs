using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AskDB.App.View_Models
{
    public partial class ChatTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; } = null!;

        public DataTemplate AssistantTemplate { get; set; } = null!;

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            ChatMessage? selectedObject = item as ChatMessage;

            if (selectedObject?.Role == AuthorRole.User)
            {
                return UserTemplate;
            }

            return AssistantTemplate;
        }
    }
}
