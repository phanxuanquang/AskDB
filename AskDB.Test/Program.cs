using AskDB.SemanticKernel.Factories;
using AskDB.SemanticKernel.Services;
using Microsoft.SemanticKernel;

namespace AskDB.Test
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var plugin = new WeatherPlugin();

            var kernelFactory = new KernelFactory()
                .UseGoogleGeminiProvider("", "gemini-2.0-flash")
                .WithPlugin(plugin)
                .WithService<IAutoFunctionInvocationFilter, AutoFunctionInvocationFilter>()
                .WithService<IFunctionInvocationFilter, FunctionInvocationFilter>();

            var chatCompletionService = new AgentChatCompletionService(kernelFactory);
            ;
        }
    }

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
    public class FunctionInvocationFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            if (context.Function.PluginName == nameof(WeatherPlugin) && context.Function.Name == WeatherPlugin.GetWeatherFunctionName)
            {
                Console.WriteLine($"Plugin: {context.Function.PluginName}");
                Console.WriteLine($"Function: {context.Function.Name}");
            }

            await next(context);

        }
    }
}
