using AskDB.Commons.Extensions;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace AskDB.App.Helpers
{
    public static class ClipboardHelper
    {
        public static void CopyToClipboard(this string content)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }

        public static void CopyToClipboard(this Exception ex)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(ex.GetFullExceptionDetails());
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }
    }
}
