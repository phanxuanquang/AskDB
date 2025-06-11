using System;
using System.Threading.Tasks;
using Windows.System;

namespace AskDB.App.Helpers
{
    public static class LicenseHelper
    {
        public const string ApplicationId = "9P1MCX47432Z";

        public static async Task SendFeedbackAsyn()
        {
            var uri = new Uri($"ms-windows-store://pdp/?productid={ApplicationId}");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}
