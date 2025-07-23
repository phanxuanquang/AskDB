using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskDB.Commons.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetFullExceptionDetails(this Exception ex)
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
