using gamevault.Models;
using IdentityModel.OidcClient.Browser;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.Helper
{
    public class WpfEmbeddedBrowser : IBrowser
    {
        private BrowserOptions _options = null;
        private WebView2 webView = null;
        private bool StartWithoutWindow;
        private Window signinWindow = null;
        public WpfEmbeddedBrowser(bool startWithoutWindow)
        {
            StartWithoutWindow = startWithoutWindow;
            if (StartWithoutWindow)
            {
                signinWindow = new Window()
                {
                    Title = "Phalcode Silent Sign-in"
                };
                VisualHelper.HideWindow(signinWindow);
            }
            else
            {
                signinWindow = new Window()
                {
                    Width = 600,
                    Height = 800,
                    Title = "Phalcode Sign-in",
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
            }
        }
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            _options = options;

            var semaphoreSlim = new SemaphoreSlim(0, 1);
            var browserResult = new BrowserResult()
            {
                ResultType = BrowserResultType.UserCancel
            };

            //signinWindow.Owner = App.Current.MainWindow;
            signinWindow.Closing += (s, e) =>
            {
                if (semaphoreSlim.CurrentCount == 0) // Ensure semaphore is not already released
                {
                    semaphoreSlim.Release();
                }
                webView.Dispose();
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

                    if (semaphoreSlim.CurrentCount == 0) // Ensure semaphore is not already released
                    {
                        semaphoreSlim.Release();
                    }
                    signinWindow.Close();
                }
            };
            webView.NavigationCompleted += (s, e) =>
            {
                try
                {
                    webView.ExecuteScriptAsync("document.getElementById(\"rememberMe\").checked=true;");
                }
                catch { }
            };

            signinWindow.Content = webView;
            signinWindow.Show();

            // Initialization
            //Alternative options: "--disk-cache-size=1000000 --disable-cache --disable-gpu-shader-disk-cache --disable-features=SubresourceFilter --disable-graphite-dawn-cache --disable-shader-cache"
            var startupOptions = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--disk-cache-size=1000000"
            };

            if (!Directory.Exists(ProfileManager.PhalcodeDir))
                Directory.CreateDirectory(ProfileManager.PhalcodeDir);

            var env = await CoreWebView2Environment.CreateAsync(null, ProfileManager.PhalcodeDir, startupOptions);

            await webView.EnsureCoreWebView2Async(env);

            // Delete existing Cookies so previous logins won't remembered
            //webView.CoreWebView2.CookieManager.DeleteAllCookies();

            // Navigate
            webView.CoreWebView2.Navigate(_options.StartUrl);

            await semaphoreSlim.WaitAsync();

            return browserResult;
        }

        public void ShowWindowIfHidden()
        {
            if (signinWindow.ShowInTaskbar == false)
            {
                VisualHelper.RestoreHiddenWindow(signinWindow, 800, 600);
                signinWindow.Title = "Phalcode Sign-in";
            }
        }
        public void ClearAllCookies()
        {
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
        }
        private bool IsBrowserNavigatingToRedirectUri(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith(_options.EndUrl);
        }

    }
}
