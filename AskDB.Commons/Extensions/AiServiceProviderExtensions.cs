using AskDB.Commons.Attributes;
using AskDB.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskDB.Commons.Extensions
{
    public static class AiServiceProviderExtensions
    {
        public static string? GetDefaultModel(this AiServiceProvider enumValue)
        {
            var attr = enumValue.GetAttributeValue<DefaultModelAttribute>();
            return attr?.DefaultModel;
        }
    }
}
