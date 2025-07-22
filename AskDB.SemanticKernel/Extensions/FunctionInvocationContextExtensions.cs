using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace AskDB.SemanticKernel.Extensions
{
    public static class FunctionInvocationContextExtensions
    {
        public static T? GetFunctionArgument<T>(this FunctionInvocationContext context, string argumentName)
        {
            if (!context.Arguments.ContainsKey(argumentName))
            {
                Debug.WriteLine($"Function: {context.Function.PluginName}.{context.Function.Name} | Argument: {argumentName} → NOT FOUND");
                return default;
            }

            var value = context.Arguments[argumentName];

            if (value == null)
            {
                Debug.WriteLine($"Function: {context.Function.PluginName}.{context.Function.Name} | Argument: {argumentName} → NULL");
                return default;
            }

            Debug.WriteLine($"Function: {context.Function.PluginName}.{context.Function.Name} | Argument: {argumentName} → `{value}`");


            if (typeof(T) == typeof(string))
            {
                return (T?)(object?)value?.ToString();
            }

            return (T?)value;
        }
    }
}
