using gamevault.Models;
using IdentityModel.OidcClient.Browser;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.Helper
{
    public class WpfEmbeddedBrowser : IBrowser
    {
        private BrowserOptions _options = null;
        private WebView2 webView = null;
              
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            _options = options;

            var semaphoreSlim = new SemaphoreSlim(0, 1);
            var browserResult = new BrowserResult()
            {
                ResultType = BrowserResultType.UserCancel
            };

            var signinWindow = new Window()
            {
                Width = 600,
                Height = 800,
                Title = "Sign In",
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            //signinWindow.Owner = App.Current.MainWindow;
            signinWindow.Closing += (s, e) =>
            {
                semaphoreSlim.Release();
            };

            webView = new WebView2();
            webView.NavigationStarting += (s, e) =>
            {
                if (IsBrowserNavigatingToRedirectUri(new Uri(e.Uri)))
                {
                    e.Cancel = true;

                    browserResult = new BrowserResult()
                    {
                        ResultType = BrowserResultType.Success,
                        Response = new Uri(e.Uri).AbsoluteUri
                    };

                    semaphoreSlim.Release();
                    signinWindow.Close();
                }
            };

            signinWindow.Content = webView;
            signinWindow.Show();

            // Initialization
            var env = await CoreWebView2Environment.CreateAsync(null, AppFilePath.WebConfigDir);
            await webView.EnsureCoreWebView2Async(env);

            // Delete existing Cookies so previous logins won't remembered
            //webView.CoreWebView2.CookieManager.DeleteAllCookies();

            // Navigate
            webView.CoreWebView2.Navigate(_options.StartUrl);

            await semaphoreSlim.WaitAsync();

            return browserResult;
        }
        private bool IsBrowserNavigatingToRedirectUri(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith(_options.EndUrl);
        }

    }
}
