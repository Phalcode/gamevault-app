using gamevault.Helper;
using gamevault.Models;
using gamevault.UserControls;
using gamevault.ViewModels;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Sms;

namespace gamevault
{
    /// <summary>
    /// Provides a client-side API to & from the main running instance of the application
    /// </summary>
    public class PipeServiceHandler
    {
        /// <summary>
        /// The URI scheme used for communication
        /// </summary>
        public const string GAMEVAULT_URI_SCHEME = "gamevault";

        /// <summary>
        /// The friendly name of the URI
        /// </summary>
        private const string GAMEVAULT_URI_NAME = "GameVault App";

        /// <summary>
        /// The name of the named pipe
        /// </summary>
        private const string GAMEVAULT_PIPE_NAME = "GameVault";

        /// <summary>
        /// The singleton instance of the handler, which is not null after the application has started
        /// </summary>
        public static PipeServiceHandler? Instance { get; private set; }

        private TaskCompletionSource isReadyForCommandsTCS = new TaskCompletionSource();

        private bool _isReadyForCommands = false;

        /// <summary>
        /// Whether or not the handler is ready for commands. If set to false (the default) then all commands sent to the main instance will be queued until ready.
        /// </summary>
        public bool IsReadyForCommands
        {
            get => _isReadyForCommands;
            set
            {
                if (_isReadyForCommands == value)
                    return;

                if (value)
                {
                    isReadyForCommandsTCS?.TrySetResult();
                }
                else
                {
                    // If we somehow have an existing tcs, cancel it before making a new one
                    isReadyForCommandsTCS?.TrySetCanceled();
                    isReadyForCommandsTCS = new TaskCompletionSource();
                }

                _isReadyForCommands = value;
            }
        }
        public bool IsAppStartup = true;
        private PipeServiceHandler()
        {
        }

        /// <summary>
        /// Starts the handler
        /// </summary>
        public static void StartInstance()
        {
            if (Instance != null)
                return;

            Instance = new PipeServiceHandler();
            try
            {
                Instance.RegisterUriScheme();
            }
            catch { } //

            try
            {
                Instance.StartNamedPipeServer();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "An error while startin the internal pipe server", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Registers the URI scheme which lets other applications communicate with the application
        /// </summary>
        private void RegisterUriScheme()
        {
            string? executablePath = null;

            // winexe does not easily support getting our .exe file
            //executablePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            // so get it from the process
            executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(executablePath))
                return;

#if WINDOWS
            if (!App.IsWindowsPackage)
            {
                var view = Microsoft.Win32.RegistryView.Registry32;
                if (Environment.Is64BitOperatingSystem)
                    view = Microsoft.Win32.RegistryView.Registry64;

                using var root = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, view);
                using var classes = root.OpenSubKey(@"Software\Classes", true)!;

                var openString = $"\"{executablePath}\" --uridata \"%1\"";
                using var existing = classes.OpenSubKey(@$"{GAMEVAULT_URI_SCHEME}\shell\open\command");

                if (existing != null && existing.GetValue("")?.ToString() == openString)
                    return;

                using var newEntry = classes.CreateSubKey(GAMEVAULT_URI_SCHEME);
                newEntry.SetValue("", $"URL:{GAMEVAULT_URI_NAME}");
                newEntry.SetValue("URL Protocol", "");

                using var command = newEntry.CreateSubKey(@"shell\open\command");
                command.SetValue("", openString);
            }
            else
            {
                //Add MC Store Code if necessary
            }
#endif
        }

        /// <summary>
        /// Starts the named pipe server which accepts connections.
        /// The named pipe will run on a background task (which should be a separate thread).
        /// </summary>
        private void StartNamedPipeServer()
        {
            _ = Task.Factory.StartNew(async () =>
            {
                var pipeSecurity = new PipeSecurity();

                // Allow all users on the current machine to connect
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                    System.Security.AccessControl.AccessControlType.Allow));

                while (true)
                {
                    // Each time we accept a connection, we create a new named pipe

                    try
                    {
                        var server = NamedPipeServerStreamAcl.Create(GAMEVAULT_PIPE_NAME, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, pipeSecurity);

                        await server.WaitForConnectionAsync().ConfigureAwait(false);

                        // Handle the pipe in the background, so we don't block our thread and can handle the next connection asap
                        _ = HandlePipeConnection(server).ConfigureAwait(false);
                    }
                    catch (Exception) { }
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles a single pipe connection
        /// </summary>
        private async Task HandlePipeConnection(NamedPipeServerStream server)
        {
            // There is no loop inside this method since we only accept a SINGLE message and then close the connection
            // It might be possible to have a persistent connection in the future, in which case this would need to keep the pipe open

            StreamWriter? writer = null;
            StreamReader? reader = null;

            try
            {
                if (!IsReadyForCommands)
                    await isReadyForCommandsTCS.Task;

                string? message = null;

                reader = new StreamReader(server, leaveOpen: true);

                message = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(message))
                {
                    var response = await HandleMessage(message).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(response))
                    {
                        // It's possible we get a response to send back (query), so do that
                        writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };
                        await writer.WriteLineAsync(response).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                // Safely dispose anything we created
                SafeDispose(writer);
                SafeDispose(reader);
                SafeDispose(server);
            }
        }

        /// <summary>
        /// Sends a single message to the main running instance
        /// </summary>
        /// <param name="message">The message to send (which should be an uri form)</param>
        /// <param name="expectsResult">True if we expect a response (such as from a Query)</param>
        /// <returns></returns>
        public static async Task<string?> SendMessage(string message, bool expectsResult = false)
        {
            string? result = null;
            var client = new NamedPipeClientStream(GAMEVAULT_PIPE_NAME);
            StreamWriter? writer = null;
            StreamReader? reader = null;

            try
            {
                await client.ConnectAsync();

                writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true };
                await writer.WriteLineAsync(message);

                if (expectsResult)
                {
                    reader = new StreamReader(client, leaveOpen: true);
                    result = await reader.ReadLineAsync();
                }
            }
            finally
            {
                SafeDispose(writer);
                SafeDispose(reader);
                SafeDispose(client);
            }

            return result;
        }

        /// <summary>
        /// Safely dispose anything without throwing an error if it's already disposed or closed or whatever
        /// </summary>
        /// <param name="disposable">The object to dispose</param>
        private static void SafeDispose(IDisposable? disposable)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Handles a single message sent to this instance
        /// </summary>
        /// <param name="message">The message to handle</param>
        /// <returns>A response if required (such as from a Query)</returns>
        private async Task<string?> HandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            CommandOptions opts;

            if (message.Equals("ShowMainWindow", StringComparison.OrdinalIgnoreCase))
            {
                // ShowMainWindow is a legacy message which we don't pass around anymore
                // But it's possible (unlikely) that other applications are sending it
                // Remove in the future if we don't need it
                opts = new CommandOptions() { Action = CommandOptions.ActionEnum.Show };
            }
            else
            {
                // Parse the message
                opts = CommandOptions.CreateFromUri(message);
            }

            return await HandleCommand(opts);
        }

        /// <summary>
        /// Handles a command (which could be potentially our command line options)
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<string?> HandleCommand(CommandOptions? options)
        {
            if (options == null)
                return null;

            if ((options.Action == CommandOptions.ActionEnum.Install || options.Action == CommandOptions.ActionEnum.Uninstall) && !SettingsViewModel.Instance.License.IsActive())
            {
                try
                {
                    string url = "https://phalco.de/products/gamevault-plus/checkout?hit_paywall=true";
                    if (SettingsViewModel.Instance.DevTargetPhalcodeTestBackend)
                    {
                        url = "https://test.phalco.de/products/gamevault-plus/checkout?hit_paywall=true";
                    }
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return null;
                }
                catch { return null; }
            }

            if (!IsReadyForCommands)
                await isReadyForCommandsTCS.Task;

            bool showMainWindow = false;
            bool isGameInstalled = false;
            Task? task = null;

            if (options.GameId.HasValue)
            {
                // There's a gameid and most of our actions want to know if it's already installed
                isGameInstalled = await GetInstalledGame(options.GameId.Value) != null;
            }

            // do stuff
            switch (options.Action)
            {
                case CommandOptions.ActionEnum.Show:
                    showMainWindow = true;

                    if (options.JumpListCommand.HasValue)
                    {
                        task = ExecuteJumpListCommand(options.JumpListCommand.Value);
                    }
                    if (options.GameId.HasValue)
                    {
                        task = ShowGame(options.GameId.Value);
                    }
                    break;
                case CommandOptions.ActionEnum.Install:
                    if (options.GameId.HasValue)
                    {
                        showMainWindow = true;

                        if (!isGameInstalled)
                            task = InstallGame(options.GameId.Value);
                        else
                            task = ShowGame(options.GameId.Value);
                    }
                    break;
                case CommandOptions.ActionEnum.Uninstall:
                    showMainWindow = isGameInstalled;
                    if (options.GameId.HasValue)
                    {
                        showMainWindow = true;

                        if (isGameInstalled)
                            task = UninstallGame(options.GameId.Value);
                        else
                            task = ShowGame(options.GameId.Value);
                    }
                    break;
                case CommandOptions.ActionEnum.Start:
                    if (options.GameId.HasValue)
                    {
                        if (!isGameInstalled)
                        {
                            showMainWindow = true;

                            if (options.AutoInstall == true && SettingsViewModel.Instance.License.IsActive())
                                task = InstallGame(options.GameId.Value);
                            else
                                task = ShowGame(options.GameId.Value);
                        }
                        else
                        {
                            showMainWindow = false;

                            task = StartGame(options.GameId.Value);
                        }
                    }
                    break;
                case CommandOptions.ActionEnum.Stop:
                    showMainWindow = false;

                    task = StopGame(options.GameId);
                    break;
                case CommandOptions.ActionEnum.Query:
                    // Shortcut the return since we absolutely never want to show the UI if we're doing a query
                    return await HandleQuery(options);
                default:
                    // You should really implement new actions that you add
                    throw new NotImplementedException($"Action {options.Action} not implemented");
            }
            if (IsAppStartup)
            {
                showMainWindow = !SettingsViewModel.Instance.BackgroundStart;
            }
            if (options.Minimized.HasValue)
            {
                // If we're provided a Minimized value then we can explicitly use that for whether or not to be shown
                showMainWindow = !options.Minimized.Value;
            }

            if (showMainWindow)
            {
                // Dispatch is used to ensure that we're on the UI thread
                await Dispatch(() =>
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow;

                    if (mainWindow != null)
                    {
                        // Make visible
                        mainWindow.Show();

                        // Restore
                        if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
                            mainWindow.WindowState = System.Windows.WindowState.Normal;

                        // Bring to foreground
                        mainWindow.Activate();
                    }
                });


                if (task != null)
                    await task;
            }
            IsAppStartup = false;
            return null;
        }

        /// <summary>
        /// Get an installed game
        /// </summary>
        /// <param name="id">The game id</param>
        /// <returns>The game or null if not installed</returns>
        private async Task<Game?> GetInstalledGame(int id)
        {
            Game? game = null;
            if (!InstallViewModel.Instance.InstalledGames.Any())
            {
                try
                {
                    await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames(true);
                }
                catch { }
            }
            game = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == id).Select(g => g.Key).FirstOrDefault();
            return game;
        }

        /// <summary>
        /// Get a game from the server or offline cache
        /// </summary>
        /// <param name="id">The game id</param>
        /// <returns>The game or null if not found</returns>
        private async Task<Game?> GetServerGame(int id)
        {
            Game? game = null;

            if (LoginManager.Instance.IsLoggedIn())
            {
                try
                {
                    string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{id}");
                    game = JsonSerializer.Deserialize<Game>(result);
                }
                catch (Exception)
                {

                }
            }

            if (game == null)
            {
                try
                {
                    string compressedStringObject = Preferences.Get(id.ToString(), LoginManager.Instance.GetUserProfile().OfflineCache);
                    if (!string.IsNullOrEmpty(compressedStringObject))
                    {
                        string decompressedObject = StringCompressor.DecompressString(compressedStringObject);
                        Game? deserializedObject = JsonSerializer.Deserialize<Game>(decompressedObject);
                        game = deserializedObject;
                    }
                }
                catch (Exception) { }
            }

            return game;
        }

        /// <summary>
        /// Start a game
        /// </summary>
        private async Task StartGame(int id)
        {
            var game = await GetInstalledGame(id);

            if (game == null)
                return;

            // Ensure we're on the UI thread
            await Dispatch(async () =>
            {
                if (App.Instance.MainWindow != null)
                {
                    App.Instance.MainWindow.Activate();
                    await Task.Delay(500);
                }

                var gameViewUserControl = new UserControls.GameViewUserControl(game, LoginManager.Instance.IsLoggedIn());
                // Set the correct UI regardless of if it's visible to let the user manage it
                MainWindowViewModel.Instance.SetActiveControl(gameViewUserControl);
                await InstallUserControl.PlayGame(game.ID);
            });
        }

        /// <summary>
        /// Stop a game
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private async Task StopGame(int? id)
        {
            if (id.HasValue)
            {
                var game = await GetInstalledGame(id.Value);

                if (game == null)
                    return;
            }

            // Can't implement this yet since we don't actually track games that are running or stopped
            throw new NotImplementedException();
        }

        /// <summary>
        /// Uninstall a game
        /// </summary>
        private async Task UninstallGame(int id)
        {
            var game = await GetInstalledGame(id);

            if (game == null)
                return;

            // Ensure we're on the UI thread
            await Dispatch(async () =>
            {
                var gameSettingsUserControl = new UserControls.GameSettingsUserControl(game) { Width = 1200, Height = 800, Margin = new Thickness(50) };

                // Would be nice to have this all in the background but there's potentially multiple popups that could have multiple options
                MainWindowViewModel.Instance.OpenPopup(gameSettingsUserControl);

                await gameSettingsUserControl.UninstallGame();
            });
        }

        /// <summary>
        /// Install a game
        /// </summary>
        private async Task InstallGame(int id)
        {
            var game = await GetInstalledGame(id);

            // Check if the game is already installed
            if (game == null)
            {
                game = await GetServerGame(id);

                if (game == null)
                {
                    MessageBox.Show($"Game with ID {id} not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ensure we're on the UI thread
                await Dispatch(async () =>
                {
                    await MainWindowViewModel.Instance.Downloads.TryStartDownload(game);
                    MainWindowViewModel.Instance.SetActiveControl(MainWindowViewModel.Instance.Downloads);
                });

                // Let user hit the Install button since the UI is complicated and doesn't support auto installing anyway
            }
        }

        /// <summary>
        /// Show a specific game
        /// </summary>
        private async Task ShowGame(int id)
        {
            var game = await GetInstalledGame(id);

            if (game == null)
                game = await GetServerGame(id);

            if (game != null)
            {
                // Ensure we're on the UI thread
                await Dispatch(() =>
                {
                    var gameViewUserControl = new UserControls.GameViewUserControl(game, LoginManager.Instance.IsLoggedIn());
                    MainWindowViewModel.Instance.SetActiveControl(gameViewUserControl);
                });
            }
            else
            {
                MessageBox.Show($"Game with ID {id} not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task ExecuteJumpListCommand(int id)
        {
            // Ensure we're on the UI thread
            await Dispatch(async () =>
            {
                switch (id)
                {
                    case 15:
                        {
                            await App.Instance.ExitApp();
                            break;
                        }
                    default:
                        {
                            if (Enum.TryParse(id.ToString(), out MainControl mainControl))
                            {
                                MainWindowViewModel.Instance.SetActiveControl(mainControl);
                            }
                            break;
                        }
                }

            });

        }

        public enum ActionQueryEnum
        {
            /// <summary>
            /// Returns true/false depending on if the game exists (installed or not)
            /// </summary>
            Exists,

            /// <summary>
            /// Returns true/false depending on if the game is downloaded
            /// </summary>
            Downloaded,

            /// <summary>
            /// Returns true/false depending on if the game is installed
            /// </summary>
            Installed,

            /// <summary>
            /// Returns the name of the game
            /// </summary>
            GetName,

            /// <summary>
            /// Returns the install directory of the game
            /// </summary>
            GetInstallDirectory,

            /// <summary>
            /// Returns the version of the app
            /// </summary>
            GetAppVersion,

            /// <summary>
            /// Returns the server url used by the app
            /// </summary>
            GetServerUrl,

            /// <summary>
            /// Returns true/false depending on if the user is logged in
            /// </summary>
            IsLoggedIn,

            /// <summary>
            /// Returns all Games of the server the current profile is connected to
            /// </summary>
            GetAllGames

        }

        /// <summary>
        /// Handles a query (<see cref="CommandOptions.ActionEnum.Query"/>) with a <see cref="CommandOptions.Query"/> of the enum type <see cref="ActionQueryEnum"/>
        /// </summary>
        /// <returns>The result of the query OR a string prefixed with "error: " if there was an error</returns>
        /// <exception cref="NotImplementedException">If the relevant <see cref="ActionQueryEnum"/> is not implemented</exception>
        private async Task<string> HandleQuery(CommandOptions options)
        {
            if (!Enum.TryParse<ActionQueryEnum>(options.Query, true, out var query))
                return "error: invalid query";

            switch (query)
            {
                case ActionQueryEnum.Exists:
                    {
                        if (!options.GameId.HasValue)
                            return "error: no game id";

                        var game = await GetInstalledGame(options.GameId.Value);

                        if (game == null)
                            game = await GetServerGame(options.GameId.Value);

                        return (game != null).ToString();
                    }
                case ActionQueryEnum.Downloaded:
                    {
                        if (!options.GameId.HasValue)
                            return "error: no game id";

                        var downloaded = false;
                        var game = await GetInstalledGame(options.GameId.Value);

                        if (game == null)
                            downloaded = true;
                        else if (DownloadsViewModel.Instance.DownloadedGames.Any(g => g.GetGameId() == options.GameId.Value))
                            downloaded = true;

                        return downloaded.ToString();
                    }
                case ActionQueryEnum.Installed:
                    {
                        if (!options.GameId.HasValue)
                            return "error: no game id";

                        var game = await GetInstalledGame(options.GameId.Value);

                        return (game != null).ToString();
                    }
                case ActionQueryEnum.GetName:
                    {
                        if (!options.GameId.HasValue)
                            return "error: no game id";

                        var game = await GetInstalledGame(options.GameId.Value);

                        if (game == null)
                            game = await GetServerGame(options.GameId.Value);

                        return game?.Title ?? "error: game not found";
                    }
                case ActionQueryEnum.GetInstallDirectory:
                    {
                        if (!options.GameId.HasValue)
                            return "error: no game id";

                        var game = await GetInstalledGame(options.GameId.Value);

                        if (game == null)
                            return "";

                        var installDirectory = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == game.ID).Select(g => g.Value).FirstOrDefault();

                        return installDirectory ?? "";
                    }
                case ActionQueryEnum.GetAllGames:
                    {
                        try
                        {
                            string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games?limit=-1");
                            return Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
                        }
                        catch { }
                        return "";
                    }
                case ActionQueryEnum.GetServerUrl:
                    return SettingsViewModel.Instance.ServerUrl;
                case ActionQueryEnum.GetAppVersion:
                    return SettingsViewModel.Instance.Version;
                case ActionQueryEnum.IsLoggedIn:
                    return LoginManager.Instance.IsLoggedIn().ToString();
                default:
                    // You should really implement new Action Queries
                    throw new NotImplementedException($"Query {query} not implemented");
            }
        }

        /// <summary>
        /// Dispatches an action on the UI thread (if available)
        /// </summary>
        private async Task Dispatch(Action action)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            if (dispatcher != null)
            {
                await dispatcher.InvokeAsync(action);
            }
            else
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Dispatches an async action on the UI thread (if available)
        /// </summary>
        private async Task Dispatch(Func<Task> action)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;

            if (dispatcher != null)
            {
                await dispatcher.InvokeAsync(action);
            }
            else
            {
                await action.Invoke();
            }
        }
    }
}
