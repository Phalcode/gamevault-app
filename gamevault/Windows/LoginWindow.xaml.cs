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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using System.Collections.ObjectModel;


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
        public bool IsLoggedInWithSSO { get; set; }
    }
    public enum LoginStep
    {
        LoadingAction = 0,
        ChooseProfile = 1,
        SignInOrSignUp = 2,
        SignIn = 3,
        SignUp = 4,
        EditProfile = 5,
        PendingActivation = 6,
        Settings = 7
    }
    public partial class LoginWindow
    {

        private LoginWindowViewModel ViewModel { get; set; }
        private StoreHelper StoreHelper;
        private bool SkipBootTasks = false;
        private InputTimer InputTimer { get; set; }
        public LoginWindow(bool skipBootTasks = false)
        {
            InitializeComponent();
            ViewModel = new LoginWindowViewModel();
            this.DataContext = ViewModel;
            ProfileManager.EnsureRootDirectory();
            this.SkipBootTasks = skipBootTasks;

            InputTimer = new InputTimer();
            InputTimer.Interval = TimeSpan.FromMilliseconds(400);
            InputTimer.Tick += ServerUrlInput_Tick;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            VisualHelper.AdjustWindowChrome(this);
            ViewModel.RememberMe = Preferences.Get(AppConfigKey.LoginRememberMe, ProfileManager.ProfileConfigFile) == "1";
            try
            {
                string result = Preferences.Get(AppConfigKey.AdditionalRequestHeaders, ProfileManager.ProfileConfigFile);
                var objResult = JsonSerializer.Deserialize<ObservableCollection<RequestHeader>>(result);
                ViewModel.AdditionalRequestHeaders = objResult;
                WebHelper.SetAdditionalRequestHeaders(ViewModel.AdditionalRequestHeaders?.ToArray());
            }
            catch { }
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
                        ProfileManager.EnsureUserProfileFileTree(lastUserProfile);
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
            if (!SettingsViewModel.Instance.License.IsActive() && ViewModel.UserProfiles.Count >= 1)
            {
                bool isDemoUserException = ViewModel.UserProfiles.Count == 1 && ViewModel.UserProfiles[0].ServerUrl == "https://demo.gamevau.lt";
                if (!isDemoUserException)
                {
                    ViewModel.AppBarText = "Oops! You just reached a premium feature of GameVault - Upgrade now and support the devs!";
                    return;
                }                
            }
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
        private async void ServerUrlInput_Tick(object sender, EventArgs e)
        {
            try
            {
                InputTimer.Stop();
                string result = await WebHelper.BaseGetAsync($"{ValidateUriScheme(InputTimer?.Data)}/api/status");
                ServerInfo serverInfo = JsonSerializer.Deserialize<ServerInfo>(result);
                if (ViewModel.LoginStepIndex == (int)LoginStep.SignIn)
                {
                    ViewModel.LoginServerInfo = new BindableServerInfo(serverInfo);
                }
                else if (ViewModel.LoginStepIndex == (int)LoginStep.SignUp)
                {
                    ViewModel.SignUpServerInfo = new BindableServerInfo(serverInfo);
                }
            }
            catch (Exception ex)
            {
                string message = WebExceptionHelper.TryGetServerMessage(ex);
                if (ViewModel.LoginStepIndex == (int)LoginStep.SignIn)
                {
                    ViewModel.LoginServerInfo = new BindableServerInfo(message == "" ? "Could not connect to server" : message);
                }
                else if (ViewModel.LoginStepIndex == (int)LoginStep.SignUp)
                {
                    ViewModel.SignUpServerInfo = new BindableServerInfo(message == "" ? "Could not connect to server" : message);
                }
            }
        }
        private void ServerUrlInput_TextChanged(object sender, RoutedEventArgs e)
        {
            InputTimer.Stop();
            InputTimer.Data = ((TextBox)sender).Text;
            InputTimer.Start();
        }
        private UserProfile SetupUserProfile(LoginUser user)
        {
            string cleanedServerUrl = WebHelper.RemoveSpecialCharactersFromUrl(user.ServerUrl);
            UserProfile profile = ProfileManager.CreateUserProfile(cleanedServerUrl);
            profile.ServerUrl = user.ServerUrl;
            Preferences.Set(AppConfigKey.ServerUrl, user.ServerUrl, profile.UserConfigFile, true);
            if (!user.IsLoggedInWithSSO)
            {
                profile.Name = user.Username;
                ViewModel.UserProfiles.Add(profile);
                Preferences.Set(AppConfigKey.Username, user.Username, profile.UserConfigFile);
                Preferences.Set(AppConfigKey.Password, user.Password, profile.UserConfigFile, true);
            }
            else
            {
                Preferences.Set(AppConfigKey.IsLoggedInWithSSO, "1", profile.UserConfigFile);
            }
            return profile;
        }
        private async void SaveWithoutLogin_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndLogin(true);
        }
        private async void SaveAndLogin_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndLogin();
        }
        private async Task SaveAndLogin(bool saveOnly = false)
        {
            try
            {
                ValidateSignInData(ViewModel.LoginUser, true);
                UserProfile profile = SetupUserProfile(ViewModel.LoginUser);
                ViewModel.LoginUser = new LoginUser();//Reset
                ViewModel.LoginServerInfo = new BindableServerInfo();//Reset
                RemoveDemoUserIfExists();
                if (saveOnly)
                {
                    ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                    return;
                }
                await Login(profile, true);
            }
            catch (Exception ex)
            {
                ViewModel.AppBarText = ex.Message;
            }
        }
        private async void ProfileLogin_Click(object sender, RoutedEventArgs e)
        {
            UserProfile selectedProfile = (UserProfile)((FrameworkElement)sender).DataContext;
            ProfileManager.EnsureUserProfileFileTree(selectedProfile);
            await Login(selectedProfile);
        }
        private string ValidateUriScheme(string uri)
        {
            if (uri.EndsWith("/"))
            {
                uri = uri.Substring(0, uri.Length - 1);
            }
            if (!uri.Contains(System.Uri.UriSchemeHttp))
            {
                uri = $"{System.Uri.UriSchemeHttps}://{uri}";
            }
            return uri;
        }
        private void ValidateSignInData(LoginUser loginUser, bool isLogin)
        {
            if (ViewModel.UserProfiles.Any(user => user.Name == loginUser.Username))
                throw new ArgumentException("Profile with this name already exists");

            if (string.IsNullOrWhiteSpace(loginUser.ServerUrl))
                throw new ArgumentException("ServerUrl is not set");

            if (isLogin && !ViewModel.LoginServerInfo.IsAvailable)
            {
                throw new ArgumentException("Server could not be reached");
            }

            loginUser.ServerUrl = ValidateUriScheme(loginUser.ServerUrl);

            if (!ViewModel.LoginUser.IsLoggedInWithSSO)
            {
                if (string.IsNullOrWhiteSpace(loginUser.Username))
                    throw new ArgumentException("Username is not set");

                if (string.IsNullOrWhiteSpace(loginUser.Password))
                    throw new ArgumentException("Password is not set");
            }
        }

        private async Task Login(UserProfile profile, bool firstTimeLogin = false, bool calledByActivationLoop = false)
        {
            if (!calledByActivationLoop)
            {
                ViewModel.StatusText = "Logging in...";
                ViewModel.LoginStepIndex = (int)LoginStep.LoadingAction;

                if (await CheckIfServerIsOutdated(profile.ServerUrl))
                {
                    ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                    return;
                }
            }

            bool isLoggedInWithSSO = Preferences.Get(AppConfigKey.IsLoggedInWithSSO, profile.UserConfigFile) == "1";
            LoginState state = LoginState.Success;
            if (!isLoggedInWithSSO)
            {
                string username = Preferences.Get(AppConfigKey.Username, profile.UserConfigFile);
                string password = Preferences.Get(AppConfigKey.Password, profile.UserConfigFile, true);
                state = await LoginManager.Instance.Login(profile, username, password);
            }
            else
            {
                state = await LoginManager.Instance.SSOLogin(profile);
                if (state == LoginState.Success)
                {
                    Preferences.Set(AppConfigKey.Username, LoginManager.Instance.GetCurrentUser()?.Username, profile.UserConfigFile);
                }
                else if (state != LoginState.Success && firstTimeLogin)
                {
                    try
                    {
                        await Task.Delay(500);
                        ProfileManager.DeleteUserProfile(profile);
                    }
                    catch
                    {
                        await Task.Delay(1500);//For slower machines, we have to wait for the webview of SSOLogin to free the web cache
                        ProfileManager.DeleteUserProfile(profile);
                    }
                }
            }
            if (state == LoginState.Success)
            {
                Preferences.Set(AppConfigKey.UserID, LoginManager.Instance.GetCurrentUser()?.ID, profile.UserConfigFile);
                await LoadMainWindow(profile);
            }
            else if (state == LoginState.NotActivated)
            {
                ViewModel.LoginStepIndex = (int)LoginStep.PendingActivation;
                await Task.Delay(5000);
                if (ViewModel.LoginStepIndex == (int)LoginStep.PendingActivation)
                    await Login(profile, firstTimeLogin, true);
                return;
            }
            else
            {
                if (firstTimeLogin)
                {
                    ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                    ViewModel.AppBarText = LoginManager.Instance.GetServerLoginResponseMessage();
                }
                else
                {
                    try
                    {//Check if the user was ever connected and is properly set up
                        string result = Preferences.Get(AppConfigKey.UserID, profile.UserConfigFile);
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            throw new Exception("NOID");
                        }
                        await LoadMainWindow(profile);
                    }
                    catch (Exception ex)
                    {
                        ViewModel.AppBarText = ex.Message == "NOID" ? LoginManager.Instance.GetServerLoginResponseMessage() : "Can not load user profile in offline mode";
                        ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                    }
                }
            }
        }
        private async Task LoadMainWindow(UserProfile profile)
        {
            LoginManager.Instance.SetUserProfile(profile);
            SettingsViewModel.Instance.Init();

            ViewModel.StatusText = "Optimizing Cache...";
            await CacheHelper.OptimizeCache();
            if (ViewModel.RememberMe)
            {
                Preferences.Set(AppConfigKey.LastUserProfile, profile.RootDir, ProfileManager.ProfileConfigFile);
            }
            App.Current.MainWindow = new MainWindow();
            if (!PipeServiceHandler.Instance.IsAppStartup && !SettingsViewModel.Instance.BackgroundStart)
            {
                App.Current.MainWindow.Show();
            }
            //First Startup Window Visibility will be determined by the protocol handler
            try
            {
                this.DialogResult = true;//will throw error, if its called from the MainWindow
            }
            catch { }
            this.Close();
        }
        private async void SaveAndSignUp_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndSignUp();
        }
        private async Task SaveAndSignUp()
        {
            try
            {
                ValidateSignUpData();
                LoginState state = LoginState.Success;
                if (!ViewModel.SignupUser.IsLoggedInWithSSO)
                {
                    state = await LoginManager.Instance.Register(ViewModel.SignupUser);//Only non-provider login has a additional call. Else its just a login, because the server will create the account internally
                }
                UserProfile profile = null;
                if (state != LoginState.Error)
                {
                    profile = SetupUserProfile(ViewModel.SignupUser);
                    if (profile == null)
                    {
                        ViewModel.AppBarText = "Failed to setup User Profile";
                        return;
                    }
                    await Login(profile, true);
                    return;
                }
                ViewModel.AppBarText = LoginManager.Instance.GetServerLoginResponseMessage();
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
            if (!ViewModel.SignupUser.IsLoggedInWithSSO)
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
                    string cleanedServerUrl = WebHelper.RemoveSpecialCharactersFromUrl(ViewModel.EditUser.ServerUrl);
                    UserProfile profile = ProfileManager.CreateUserProfile(cleanedServerUrl);
                    profile.Name = ViewModel.EditUser.Username;
                    profile.ServerUrl = ViewModel.EditUser.ServerUrl;
                    ViewModel.UserProfiles.Add(profile);
                    Preferences.Set(AppConfigKey.ServerUrl, ViewModel.EditUser.ServerUrl, profile.UserConfigFile, true);
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

                UserProfile demoProfile = ProfileManager.CreateUserProfile(WebHelper.RemoveSpecialCharactersFromUrl(demoServerUrl));
                demoProfile.Name = demoUsername;
                demoProfile.ServerUrl = demoServerUrl;

                ViewModel.UserProfiles.Add(demoProfile);

                Preferences.Set(AppConfigKey.ServerUrl, demoServerUrl, demoProfile.UserConfigFile, true);
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

        private async Task<bool> CheckIfServerIsOutdated(string serverUrl)
        {
            //User Notification for major client/server update
            bool isServerOutdated = false;
            try
            {
                string serverResonse = await WebHelper.BaseGetAsync(@$"{serverUrl}/api/status");
                string currentServerVersion = System.Text.Json.JsonSerializer.Deserialize<ServerInfo>(serverResonse).Version;
                if (currentServerVersion == null || currentServerVersion == "")
                {
                    isServerOutdated = true;
                }
                isServerOutdated = new Version(currentServerVersion) < new Version("15.0.0");
            }
            catch { }
            if (isServerOutdated)
            {
                try
                {
                    MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync("CLIENT-SERVER-INCOMPABILITY DETECTED",
                          $"Your GameVault Client is not compatible with the GameVault Server you are using (<15.0.0). This server is too old for your client.\r\n\r\nYou have the following options:\r\n",
                          MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                          {
                              AffirmativeButtonText = "Get older client version from GitHub",
                              NegativeButtonText = "Update the server",
                              AnimateHide = false,
                              DialogMessageFontSize = 20,
                              DialogTitleFontSize = 25
                          });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        Process.Start(new ProcessStartInfo("https://github.com/Phalcode/gamevault-app/releases") { UseShellExecute = true });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo("https://github.com/Phalcode/gamevault-backend/releases/tag/12.2.0") { UseShellExecute = true });
                    }
                }
                catch { }
            }
            return isServerOutdated;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!App.HideToSystemTray)
            {
                App.Current.Shutdown();
            }
        }

        private void AddAdditionalRequestHeader_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AdditionalRequestHeaders.Add(new RequestHeader());
        }

        private void RemoveAdditionalRequestHeader_Click(object sender, RoutedEventArgs e)
        {
            int index = ViewModel.AdditionalRequestHeaders.IndexOf(((RequestHeader)((FrameworkElement)sender).DataContext));
            if (index >= 0)
            {
                ViewModel.AdditionalRequestHeaders.RemoveAt(index);
            }
        }
        private void SaveAdditionalHeaders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preferences.Set(AppConfigKey.AdditionalRequestHeaders, ViewModel.AdditionalRequestHeaders.Where(rh => !string.IsNullOrWhiteSpace(rh.Name) && !string.IsNullOrWhiteSpace(rh.Value)), ProfileManager.ProfileConfigFile);
                WebHelper.SetAdditionalRequestHeaders(ViewModel.AdditionalRequestHeaders?.ToArray());
                ViewModel.LoginStepIndex = (int)LoginStep.ChooseProfile;
                ViewModel.AppBarText = "Successfully saved additional request headers";
            }
            catch (Exception ex) { ViewModel.AppBarText = ex.Message; }
        }
        private void LoginWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoginStepIndex = (int)LoginStep.Settings;
        }

        private async void UserLoginTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await SaveAndLogin();
            }
        }
        private async void UserRegistrationTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await SaveAndSignUp();
            }
        }

    }
}
