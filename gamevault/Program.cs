using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using CommandLine;
using gamevault.Models;

namespace gamevault
{
    /// <summary>
    /// Main entry point
    /// </summary>
    public class Program
    {
        // Unique ID for the mutex, which will not be shared by another application
        private const string GAMEVAULT_MUTEX = "0C8E52D8-ECD4-4F12-95B5-CE3412C073EA:GameVault";

        /// <summary>
        /// The main entry point for the application.
        /// This will load the minimal number of dlls needed before passing off to the main UI.
        /// </summary>
        /// <param name="args">Args passed to the application from the command line</param>
        [STAThread]
        public static void Main(string[] args)
        {
            //string msg = $"File:Program.cs - Method:Main - Arg Lenght: {args.Length} - Arg content: {string.Join(",", args)}";
            //System.Windows.MessageBox.Show(msg);
            CommandOptions cmdLineOptions;
            if (args.Length > 0 && args.Any(s => s.Contains("gamevault://")) && !args[0].Contains("--uridata"))
            {
                List<string> stringList = new List<string>(args);
                stringList.Insert(0, "--uridata");
                args = stringList.ToArray();
            }

            using (var parser = new Parser(with =>
            {
                with.AutoVersion = true;
                with.AutoHelp = true;
                with.HelpWriter = null;

                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.IgnoreUnknownArguments = true;
            }))
            {
                var cmdLineParserResult = parser.ParseArguments(args, CommandOptions.CommandLineTypes);

                #region Command Line Help

                cmdLineParserResult.WithNotParsed(errors =>
                {
                    // We only treat uri requests differently for the sake of help, otherwise it's parsed automatically
                    var isFromUri = args.Any(x => x.StartsWith("--uridata", StringComparison.OrdinalIgnoreCase));

                    var help = CommandLine.Text.HelpText.AutoBuild(cmdLineParserResult, help =>
                    {
                        help.AddEnumValuesToHelpText = true;
                        help.AddNewLineBetweenHelpSections = true;
                        help.AdditionalNewLineAfterOption = false;

                        // Attempt to get the actual verb used
                        var action = errors.OfType<HelpVerbRequestedError>().FirstOrDefault()?.Verb ?? "<action>";

                        help.AddPreOptionsLine(Environment.NewLine);

                        if (isFromUri)
                        {
                            // URI help
                            help.AddDashesToOption = false;
                            help.AddPreOptionsLine($"USAGE: {PipeServiceHandler.GAMEVAULT_URI_SCHEME}://{action}?[param=value]&...");
                        }
                        else
                        {
                            // Default in case we can't determine the actual executable
                            var executable = "gamevault.exe";

                            // winexe does not easily support getting our .exe file
                            //var executablePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                            // so get it from the process
                            var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

                            if (!string.IsNullOrEmpty(executablePath))
                                executable = Path.GetFileName(executablePath);

                            help.AddPreOptionsLine($"USAGE: {executable} {action} [param=value] ...");
                        }

                        return CommandLine.Text.HelpText.DefaultParsingErrorsHandler(cmdLineParserResult, help);
                    }, maxDisplayWidth: 600);

                    // We can't return help on the command line, so show a message box instead
                    System.Windows.MessageBox.Show(
                        help.ToString(),
                        "Help",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    if (errors.Any(e => e.StopsProcessing))
                        Environment.Exit(1);

                    return;
                });

                #endregion Command Line Help

                // Downgrade to CommandOptions which can be treated generically
                if (cmdLineParserResult?.Value is CommandOptions commandOptions)
                    cmdLineOptions = commandOptions;
                else
                    cmdLineOptions = new CommandOptions();
            }

            bool createdMutex = false;

            // Mutexes are preferred when checking if a program is already running, since in theory another GameVault.exe could be running that isn't ours
            if (!Mutex.TryOpenExisting(GAMEVAULT_MUTEX, out _))
            {
                // GameVault is not running
                _ = new Mutex(true, GAMEVAULT_MUTEX, out createdMutex);
            }

            if (!createdMutex)
            {
                // GameVault is already running and we should send messages to that instance
                // We can't use await here since it puts is on a non-STA thread

                if (cmdLineOptions.Action == CommandOptions.ActionEnum.Query)
                {
                    // We're sending a query through the pipe, this is mostly for debugging
                    var result = PipeServiceHandler.SendMessage(cmdLineOptions.UriData!, expectsResult: true).GetAwaiter().GetResult();

                    result = result?.Trim('\r', '\n');

                    // Limitation of running as WinExe, we can't write to Console.Out so, we can only show a visible version of the result
                    if (!string.IsNullOrEmpty(result))
                        System.Windows.MessageBox.Show(result, "Query Result", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    Environment.Exit(1);
                    return;
                }

                PipeServiceHandler.SendMessage(cmdLineOptions.UriData, expectsResult: false).GetAwaiter().GetResult();
                Environment.Exit(1);
                return;
            }

            PipeServiceHandler.StartInstance();
            RunApp(cmdLineOptions);
        }

        /// <summary>
        /// Runs the main application with the given command line
        /// </summary>
        // Ensure the method is not inlined, so you don't need to load any WPF dll in the Main method
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void RunApp(CommandOptions cmdLineOptions)
        {
            App.CommandLineOptions = cmdLineOptions;


            App.Instance.InitializeComponent();
            App.Instance.Run();
        }
    }
}
