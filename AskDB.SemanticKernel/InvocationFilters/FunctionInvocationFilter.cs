using AskDB.SemanticKernel.Plugins;
using Microsoft.SemanticKernel;

namespace AskDB.SemanticKernel.InvocationFilters
{
    public class FunctionInvocationFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            if (context.Function.PluginName == nameof(DatabaseInteractionPlugin) && context.Function.Name == nameof(DatabaseInteractionPlugin.SearchTablesByNameAsync))
            {
                Console.WriteLine($"Plugin: {context.Function.PluginName}");
                Console.WriteLine($"Function: {context.Function.Name}");
            }

            await next(context);

        }
    }
}
