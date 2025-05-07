using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;


namespace gamevault.Windows
{
    public class LoginUser
    {
        public string ID { get; set; }
        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
        public DateTime BirthDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public bool IsLoggedInWithOAuth { get; set; }
    }
    public enum LoginStep
    {
        LoadingAction = 0,
        ChooseProfile = 1,
        SignInOrSignUp = 2,
        SignIn = 3,
        SignUp = 4,
        EditProfile = 5,
        PendingActivation = 6
    }
    public partial class LoginWindow
    {

        private LoginWindowViewModel ViewModel { get; set; }
        private StoreHelper StoreHelper;
        private bool SkipBootTasks = false;
        public LoginWindow(bool skipBootTasks = false)
        {
            InitializeComponent();
            ViewModel = new LoginWindowViewModel();
            this.DataContext = ViewModel;
            ProfileManager.EnsureRootDirectory();
            this.SkipBootTasks = skipBootTasks;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            VisualHelper.AdjustWindowChrome(this);
            ViewModel.RememberMe = Preferences.Get(AppConfigKey.LoginRememberMe, ProfileManager.ProfileConfigFile) == "1";
            if (!SkipBootTasks)
            {
                await CheckForUpdates(this);
                ViewModel.StatusText = "Checking License...";
                string phalcodeLoginMessage = await LoginManager.Instance.PhalcodeLogin(true);
                if (phalcodeLoginMessage != string.Empty)
                    ViewModel.AppBarText = phalcodeLoginMessage;


                if (ViewModel.RememberMe)
                {
                    try
                    {
                        string lastUserProfileIdentifier = Preferences.Get(AppConfigKey.LastUserProfile, ProfileManager.ProfileConfigFile);
                        UserProfile lastUserProfile = ProfileManager.GetUserProfiles().First(up => up.RootDir == lastUserProfileIdentifier);
                        await Login(lastUserProfile);
                    }
                    catch { }
                }
            }

            foreach (UserProfile userProfile in ProfileManager.GetUserProfiles())
            {
                ViewModel.UserProfiles.Add(userProfile);
            }
            if (ViewModel.UserProfiles.Count == 0)
            {
                CreateDemoUser();
            }
            ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
        }
        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStepIndex = (int)LoginStep.SignInOrSignUp;
        }
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStepIndex = (int)LoginStep.SignIn;
        }
        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStepIndex = (int)LoginStep.SignUp;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            int loginStep = (int)((FrameworkElement)sender).Tag;
            ViewModel.LoginStepIndex = loginStep;
        }

        private async void SaveAndLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ValidateSignInData(ViewModel.LoginUser);
                string cleanedServerUrl = RemoveSpecialCharacters(ViewModel.LoginUser.ServerUrl);
                UserProfile profile = ProfileManager.CreateUserProfile(RemoveSpecialCharacters(cleanedServerUrl));
                profile.ServerUrl = ViewModel.LoginUser.ServerUrl;
                Preferences.Set(AppConfigKey.ServerUrl, ViewModel.LoginUser.ServerUrl, profile.UserConfigFile);
                if (!ViewModel.LoginUser.IsLoggedInWithOAuth)
                {
                    profile.Name = ViewModel.LoginUser.Username;
                    ViewModel.UserProfiles.Add(profile);
                    Preferences.Set(AppConfigKey.Username, ViewModel.LoginUser.Username, profile.UserConfigFile);
                    Preferences.Set(AppConfigKey.Password, ViewModel.LoginUser.Password, profile.UserConfigFile, true);
                }
                else
                {
                    Preferences.Set(AppConfigKey.IsLoggedInWithOAuth, "1", profile.UserConfigFile);
                }
                ViewModel.LoginUser = new LoginUser();//Reset
                RemoveDemoUserIfExists();
                await Login(profile, true);
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = ex.Message;
            }
        }
        private async void ProfileLogin_Click(object sender, RoutedEventArgs e)
        {
            await Login((UserProfile)((FrameworkElement)sender).DataContext);
        }
        private string ValidateUriScheme(string uri)
        {
            if (uri.EndsWith("/"))
            {
                uri = uri.Substring(0, SettingsViewModel.Instance.ServerUrl.Length - 1);
            }
            if (!uri.Contains(System.Uri.UriSchemeHttp))
            {
                uri = $"{System.Uri.UriSchemeHttps}://{uri}";
            }
            return uri;
        }
        private void ValidateSignInData(LoginUser loginUser)
        {
            if (string.IsNullOrWhiteSpace(loginUser.ServerUrl))
                throw new ArgumentException("ServerUrl is not set");

            loginUser.ServerUrl = ValidateUriScheme(loginUser.ServerUrl);

            if (!ViewModel.LoginUser.IsLoggedInWithOAuth)
            {
                if (string.IsNullOrWhiteSpace(loginUser.Username))
                    throw new ArgumentException("Username is not set");

                if (string.IsNullOrWhiteSpace(loginUser.Password))
                    throw new ArgumentException("Password is not set");
            }
        }

        private async Task Login(UserProfile profile, bool firstTimeLogin = false)
        {
            ViewModel.StatusText = "Logging in...";
            ViewModel.LoginStepIndex = (int)LoginStep.LoadingAction;
            bool isLoggedInWithOAuth = Preferences.Get(AppConfigKey.IsLoggedInWithOAuth, profile.UserConfigFile) == "1";
            LoginState state = LoginState.Success;
            if (!isLoggedInWithOAuth)
            {
                string username = Preferences.Get(AppConfigKey.Username, profile.UserConfigFile);
                string password = Preferences.Get(AppConfigKey.Password, profile.UserConfigFile, true);
                state = await LoginManager.Instance.Login(profile.ServerUrl, username, password);
            }
            else
            {
                state = await LoginManager.Instance.OAuthLogin(profile, firstTimeLogin);
                if (state == LoginState.Success)
                {
                    Preferences.Set(AppConfigKey.Username, LoginManager.Instance.GetCurrentUser().Username, profile.UserConfigFile);
                }
                else if (state != LoginState.Success && firstTimeLogin)
                {
                    ProfileManager.DeleteUserProfile(profile);
                }
            }
            if (state == LoginState.Success)
            {
                LoginManager.Instance.SetUserProfile(profile);
                SettingsViewModel.Instance.Init();
                Preferences.Set(AppConfigKey.UserID, LoginManager.Instance.GetCurrentUser()!.ID, profile.UserConfigFile);
                ViewModel.StatusText = "Optimizing Cache...";
                await CacheHelper.OptimizeCache();
                if (ViewModel.RememberMe)
                {
                    Preferences.Set(AppConfigKey.LastUserProfile, profile.RootDir, ProfileManager.ProfileConfigFile);
                }                
                App.Current.MainWindow = new MainWindow();
                //Window Visibility will be determined by the protocol handler
                try
                {
                    this.DialogResult = true;//will throw error, if its called from the MainWindow
                }
                catch { }
                this.Close();
            }
            else
            {
                ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                ViewModel.AppBarText = LoginManager.Instance.GetServerLoginResponseMessage();
            }
        }
        private async void SaveAndSignUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ValidateSignUpData();
                LoginState state = await LoginManager.Instance.Register(ViewModel.SignupUser);
                //if (state != LoginState.Error)
                //{
                //    ViewModel.SignupUser = new LoginUser();
                //}
                if (state == LoginState.NotActivated)
                {
                    ViewModel.LoginStepIndex = (int)LoginStep.PendingActivation;
                    while (ViewModel.LoginStepIndex == (int)LoginStep.PendingActivation)
                    {
                        if (await LoginManager.Instance.Login(ViewModel.SignupUser.ServerUrl, ViewModel.SignupUser.Username, ViewModel.SignupUser.Password) == LoginState.Success && LoginManager.Instance.GetCurrentUser() != null && LoginManager.Instance.GetCurrentUser().Activated == true)
                        {//To Do: The Login Method should check itself if the user is activated or not.

                        }
                        await Task.Delay(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = ex.Message;
            }
        }
        private void ValidateSignUpData()
        {
            if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.ServerUrl))
            {
                throw new Exception("Server URL is not set");
            }
            ViewModel.SignupUser.ServerUrl = ValidateUriScheme(ViewModel.SignupUser.ServerUrl);
            if (!ViewModel.SignupUser.IsLoggedInWithOAuth)
            {
                if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.Password) || string.IsNullOrWhiteSpace(ViewModel.SignupUser.RepeatPassword))
                {
                    throw new Exception("Password is not set");
                }
                if (ViewModel.SignupUser.Password != ViewModel.SignupUser.RepeatPassword)
                {
                    throw new Exception("Password must be equal");
                }
                if (string.IsNullOrWhiteSpace(ViewModel.SignupUser.Username))
                {
                    throw new Exception("Username is not set");
                }
            }
        }
        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void UserProfileContextMenu_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = true;
            }
        }

        private void EditUserProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuItem menuItem)
                {
                    UserProfile profileToEdit = (UserProfile)((FrameworkElement)((ContextMenu)menuItem.Parent).TemplatedParent).DataContext;
                    ViewModel.EditUser = new LoginUser() { ID = profileToEdit.RootDir, ServerUrl = profileToEdit.ServerUrl, Username = profileToEdit.Name, Password = Preferences.Get(AppConfigKey.Password, profileToEdit.UserConfigFile, true) };
                    ViewModel.LoginStepIndex = (int)LoginStep.EditProfile;
                }
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = ex.Message;
            }
        }

        private async void DeleteUserProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                UserProfile profileToDelete = (UserProfile)((FrameworkElement)((ContextMenu)menuItem.Parent).TemplatedParent).DataContext;
                MessageDialogResult result = await ((MetroWindow)this).ShowMessageAsync($"Are you sure you want to delete Profile '{profileToDelete.Name}'?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    ProfileManager.DeleteUserProfile(profileToDelete);
                    ViewModel.UserProfiles.Remove(profileToDelete);
                }
            }
        }

        private void UserProfileEditSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UserProfile? profileToEdit = ViewModel.UserProfiles.FirstOrDefault(x => x.RootDir == ViewModel.EditUser.ID);
                if (profileToEdit == null)
                    throw new Exception("User Profile not found");
                if (profileToEdit.ServerUrl != ViewModel.EditUser.ServerUrl)
                {
                    ProfileManager.DeleteUserProfile(profileToEdit);
                    ViewModel.UserProfiles.Remove(profileToEdit);
                    string cleanedServerUrl = RemoveSpecialCharacters(ViewModel.EditUser.ServerUrl);
                    UserProfile profile = ProfileManager.CreateUserProfile(cleanedServerUrl);
                    profile.Name = ViewModel.EditUser.Username;
                    profile.ServerUrl = ViewModel.EditUser.ServerUrl;
                    ViewModel.UserProfiles.Add(profile);
                    Preferences.Set(AppConfigKey.ServerUrl, ViewModel.EditUser.ServerUrl, profile.UserConfigFile);
                    Preferences.Set(AppConfigKey.Username, ViewModel.EditUser.Username, profile.UserConfigFile);
                    Preferences.Set(AppConfigKey.Password, ViewModel.EditUser.Password, profile.UserConfigFile, true);
                }
                else
                {
                    profileToEdit.Name = ViewModel.EditUser.Username;
                    Preferences.Set(AppConfigKey.Username, ViewModel.EditUser.Username, profileToEdit.UserConfigFile);
                    Preferences.Set(AppConfigKey.Password, ViewModel.EditUser.Password, profileToEdit.UserConfigFile, true);
                }
                ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = ex.Message;
            }
        }
        private void CreateDemoUser()
        {
            try
            {
                string demoServerUrl = "https://demo.gamevau.lt";
                string demoUsername = "demo";
                string demoPassword = "demodemo";

                UserProfile demoProfile = ProfileManager.CreateUserProfile(RemoveSpecialCharacters(demoServerUrl));
                demoProfile.Name = demoUsername;
                demoProfile.ServerUrl = demoServerUrl;

                ViewModel.UserProfiles.Add(demoProfile);

                Preferences.Set(AppConfigKey.ServerUrl, demoServerUrl, demoProfile.UserConfigFile);
                Preferences.Set(AppConfigKey.Username, demoUsername, demoProfile.UserConfigFile);
                Preferences.Set(AppConfigKey.Password, demoPassword, demoProfile.UserConfigFile, true);
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = $"Failed to create demo user: {ex.Message}";
            }
        }

        private void RemoveDemoUserIfExists()
        {
            try
            {
                UserProfile? demoProfile = ViewModel.UserProfiles.FirstOrDefault(p => p.ServerUrl == "https://demo.gamevau.lt");

                if (demoProfile != null && ViewModel.UserProfiles.Count > 1)
                {
                    ProfileManager.DeleteUserProfile(demoProfile);
                    ViewModel.UserProfiles.Remove(demoProfile);
                }
            }
            catch (Exception ex) { }
        }
        public async Task CheckForUpdates(Window root)
        {
            try
            {
                ViewModel.StatusText = "Searching for Updates...";
                StoreHelper = new StoreHelper();
                if (true == await StoreHelper.UpdatesAvailable())
                {
                    ViewModel.StatusText = "Updating...";
                    await StoreHelper.DownloadAndInstallAllUpdatesAsync(root);
                }
                App.IsWindowsPackage = true;
            }
            catch (COMException comEx)
            {
                //Is no MSIX package
            }
            catch (Exception ex)
            {
                //rest of the cases
            }
            try
            {
                if (App.IsWindowsPackage == false)
                {
                    var response = await WebHelper.BaseGetAsync("https://api.github.com/repos/Phalcode/gamevault-app/releases");
                    dynamic obj = JsonNode.Parse(response);
                    string version = (string)obj[0]["tag_name"];
                    if (Convert.ToInt32(version.Replace(".", "")) > Convert.ToInt32(SettingsViewModel.Instance.Version.Replace(".", "")))
                    {
                        MessageBoxResult result = MessageBox.Show($"A new version of GameVault is now available on GitHub.\nCurrent Version '{SettingsViewModel.Instance.Version}' -> new Version '{version}'\nWould you like to download it? (No automatic installation)", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Yes)
                        {
                            string downloadUrl = (string)obj[0]["assets"][0]["browser_download_url"];
                            Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                            App.Current.Shutdown();
                        }
                    }
                }
            }
            catch { }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!App.HideToSystemTray)
            {
                App.Current.Shutdown();
            }
        }
    }
}
