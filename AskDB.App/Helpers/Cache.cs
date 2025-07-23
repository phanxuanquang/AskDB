using AskDB.SemanticKernel.Factories;
using AskDB.SemanticKernel.Models;
using System;
using System.Collections;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace AskDB.App.Helpers
{
    public static class Cache
    {
        public static StandardAiServiceProviderCredential? StandardAiServiceProviderCredential { get; set; } = null;
        public static KernelFactory? KernelFactory { get; set; } = null;

        public static bool HasUserEverConnectedToDatabase { get; set; } = false;
    }
}
