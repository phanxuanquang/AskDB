using AskDB.Commons.Helpers;
using AskDB.SemanticKernel.Factories;
using System;
using System.Collections;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace AskDB.App.Helpers
{
    public static class Cache
    {
        public static string ApiKey { get; set; }

        public static bool HasUserEverConnectedToDatabase { get; set; } = false;
        public static GeminiCodeAssistConnector GeminiCodeAssistConnector;

        public static KernelFactory? KernelFactory { get; set; }

        public const string DefaultModelAlias = "gemini-2.5-flash";

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

        private static string GetFullExceptionDetails(this Exception ex)
        {
            if (ex == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("Exception Type: " + ex.GetType().FullName);
            sb.AppendLine("Message: " + ex.Message);
            sb.AppendLine("Source: " + ex.Source);
            sb.AppendLine("TargetSite: " + ex.TargetSite?.ToString());
            sb.AppendLine("StackTrace: " + ex.StackTrace);

            if (ex.Data != null && ex.Data.Count > 0)
            {
                sb.AppendLine("Data:");
                foreach (DictionaryEntry de in ex.Data)
                {
                    sb.AppendLine($"  {de.Key}: {de.Value}");
                }
            }

            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exception:");
                sb.AppendLine(GetFullExceptionDetails(ex.InnerException));
            }

            return sb.ToString();
        }

    }
}
