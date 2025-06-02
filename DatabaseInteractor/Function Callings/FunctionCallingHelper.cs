using DatabaseInteractor.Function_Callings.Attributes;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
using System.Reflection;

namespace DatabaseInteractor.FunctionCallings.Services
{
    public static class FunctionCallingHelper
    {
        public static List<FunctionDeclaration> FunctionDeclarations { get; } = [];

        public static void RegisterFunction(Delegate del, Parameters? parameters = null)
        {
            var attr = del.GetFunctionDeclarationAttribute();

            if (FunctionDeclarations.Exists(fd => fd.Name.Equals(attr.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            FunctionDeclarations.Add(new FunctionDeclaration
            {
                Name = attr.Name,
                Description = attr.Description,
                Parameters = parameters
            });
        }

        public static FunctionDeclarationAttribute GetFunctionDeclarationAttribute(this Delegate del)
        {
            MethodInfo methodInfo = del.Method;
            var attribute = methodInfo.GetCustomAttribute<FunctionDeclarationAttribute>();
            if (attribute == null)
            {
                MethodInfo baseMethod = methodInfo.GetBaseDefinition();
                if (baseMethod != methodInfo)
                {
                    attribute = baseMethod.GetCustomAttribute<FunctionDeclarationAttribute>();
                }
            }

            return attribute ?? throw new InvalidOperationException($"Method '{del.Method.Name}' does not have any FunctionDeclaration attribute.");
        }

        public static string GetFunctionName(Delegate del)
        {
            return del.GetFunctionDeclarationAttribute()?.Name;
        }

        public static FunctionResponse CreateResponse(string name, string output) => new()
        {
            Name = name,
            Response = new Response
            {
                Output = output
            }
        };
    }
}