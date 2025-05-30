using DatabaseInteractor.FunctionCallings.Attributes;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
using System.Reflection;

namespace DatabaseInteractor.FunctionCallings.Services
{
    public static class FunctionCallingHelper
    {
        public static List<FunctionDeclaration> FunctionDeclarations { get; } = [];

        public static void RegisterFunction(string name, string description, Parameters? parameters = null)
        {
            if (FunctionDeclarations.Exists(fd => fd.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            FunctionDeclarations.Add(new FunctionDeclaration
            {
                Name = name,
                Description = description,
                Parameters = parameters
            });
        }

        public static void RegisterFunction(Delegate del, Parameters? parameters = null)
        {
            var method = del.Method;
            var name = method.GetCustomAttribute<NameAttribute>().Name;

            if (FunctionDeclarations.Exists(fd => fd.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            string description = method.GetCustomAttribute<DescriptionAttribute>().Description;

            FunctionDeclarations.Add(new FunctionDeclaration
            {
                Name = name,
                Description = description,
                Parameters = parameters
            });
        }

        public static string GetParameterValue(Delegate del, string parameterName)
        {
            MethodInfo method = del.Method;
            ParameterInfo[] parameters = method.GetParameters();
            foreach (var parameter in parameters)
            {
                return (string)parameter.DefaultValue;
            }
            throw new ArgumentException($"Parameter '{parameterName}' not found in function '{method.Name}'.");
        }

        public static string GetFunctionName(Delegate del)
        {
            MethodInfo method = del.Method;
            NameAttribute? nameAttribute = method.GetCustomAttribute<NameAttribute>();
            if (nameAttribute != null)
            {
                return nameAttribute.Name;
            }
            throw new InvalidOperationException($"Function {method.Name} does not have a Name attribute defined.");
        }

        public static FunctionResponse CreateResponse(Delegate del, string output) => new()
        {
            Name = del.Method.GetCustomAttribute<NameAttribute>().Name,
            Response = new Response { Output = output }
        };

        public static FunctionResponse CreateResponse(string name, string output) => new()
        {
            Name = name,
            Response = new Response { Output = output }
        };
    }
}