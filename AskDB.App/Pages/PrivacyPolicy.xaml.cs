using AskDB.App.Helpers;
using AskDB.Commons.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PrivacyPolicy : Page
{
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        SetLoading(true);
        PolicyContent.Text = await GithubOnlineContentHelper.GetContentFromUrlAsync("https://raw.githubusercontent.com/phanxuanquang/AskDB/refs/heads/master/Policy/Privacy%20Policy.md");
        SetLoading(false);
    }

    private void SetLoading(bool isLoading)
    {
        LoadingOverlay.SetLoading(null, isLoading, 72);
        MainSpace.Visibility = VisibilityHelper.SetVisible(!isLoading);
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (Cache.HasUserEverConnectedToDatabase)
        {
            this.Frame.Navigate(typeof(ExistingDatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
        else
        {
            this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
