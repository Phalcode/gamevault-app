using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.Services.Store;

namespace gamevault.Helper
{
    internal class StoreHelper
    {
        private StoreContext context = null;
        private IReadOnlyList<StorePackageUpdate> updates = null;
        internal StoreHelper()
        {
            if (context == null)
            {
                context = StoreContext.GetDefault();
            }
        }
        public async Task<bool> UpdatesAvailable()
        {
            updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();
            if (updates.Count != 0)
            {
                return true;
            }
            return false;
        }
        public async Task DownloadAndInstallAllUpdatesAsync(Window window)
        {
            var wih = new System.Windows.Interop.WindowInteropHelper(window);
            var handlePtr = wih.Handle;
            //var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);            
            WinRT.Interop.InitializeWithWindow.Initialize(context, handlePtr);
            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> installOperation = this.context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);
            StorePackageUpdateResult result = await installOperation.AsTask();
            switch (result.OverallState)
            {
                case StorePackageUpdateState.Completed:
                    break;
                case StorePackageUpdateState.Canceled:
                    UpdateCanceledException();
                    break;
                default:
                    var failedUpdates = result.StorePackageUpdateStatuses.Where(status => status.PackageUpdateState != StorePackageUpdateState.Completed);

                    if (failedUpdates.Count() != 0)
                    {
                        PackageException();
                    }
                    break;
            }
        }
        public void NoInternetException()
        {
            MessageBox.Show("Cannot connect to microsoft services", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Warning);          
        }
        private void PackageException()
        {
            MessageBox.Show("Updates were not installed as intended.", "Update failed", MessageBoxButton.OK, MessageBoxImage.Warning);            
        }
        private void UpdateCanceledException()
        {
            MessageBox.Show("Updates were not installed as intended.", "Update canceled", MessageBoxButton.OK, MessageBoxImage.Warning);
            //MessageBox.Show("GameVault can not start because the Updates were not installed.", "Updates not installed", MessageBoxButton.OK, MessageBoxImage.Information);           
        }
    }
}
