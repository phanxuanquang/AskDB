using Microsoft.SemanticKernel;

namespace AskDB.SemanticKernel.InvocationFilters
{
    public class AutoFunctionInvocationFilter : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            Console.WriteLine("Before Prompt Executed");

            Console.WriteLine("AutoFunctionInvocationFilter");

            Console.WriteLine(context.Function.PluginName);
            Console.WriteLine(context.Function.Name);

            await next(context);

            Console.WriteLine("After AutoFunctionInvocationFilter Executed");
        }
    }
}
