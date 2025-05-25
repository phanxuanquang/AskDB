using Microsoft.UI.Xaml;

namespace AskDB.App
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            var iconPath = "Assets/icon.ico";
            appWindow.SetIcon(iconPath);

            Window.Activate();
        }

        public static Window Window { get; private set; }

    }
}
