
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using CommandLine;
using CommandLine.Text;

namespace gamevault.Models
{
    /// <summary>
    /// The base CommandOptions class, used for parsing command line options and passing data between different instances of the running process
    /// </summary>
    [Verb("<none>", isDefault: true, Hidden = true)]
    public class CommandOptions
    {
        /// <summary>
        /// The types of command line options supported
        /// </summary>
        public static readonly Type[] CommandLineTypes = new Type[] {
            typeof(CommandOptions),
            typeof(CommandOptions_Show),
            typeof(CommandOptions_Install),
            typeof(CommandOptions_Uninstall),
            typeof(CommandOptions_Start),

#if false
            typeof(CommandOptions_Stop)
#endif
        };

        /// <summary>
        /// The types of command line options supported
        /// </summary>
        public enum ActionEnum
        {
            /// <summary>
            /// Show the main application or specific game
            /// </summary>
            Show,

            /// <summary>
            /// Install a game
            /// </summary>
            Install,

            /// <summary>
            /// Uninstall a game
            /// </summary>
            Uninstall,

            /// <summary>
            /// Start a game
            /// </summary>
            Start,

            /// <summary>
            /// Stop a game or all games
            /// </summary>
            Stop,

            // A means by which other programs can interact with our Pipe to ask for simple information
            // It is NOT a replacement for the Gamevault Backend API
            Query
        }

        /// <summary>
        /// The type of action to perform
        /// </summary>
        [Option("action", Required = false, Hidden = true)]
        public virtual ActionEnum Action { get; set; } = ActionEnum.Show;

        /// <summary>
        /// Whether or not this should happen in the background or bring the main window to the foreground
        /// </summary>
        [Option("minimized", Required = false, Hidden = true)]
        public virtual bool? Minimized { get; set; } = null;

        /// <summary>
        /// The id of the game to action on
        /// </summary>
        [Option("gameid", Required = false, Hidden = true)]
        public virtual int? GameId { get; set; } = null;

        /// <summary>
        /// Executes the Command of the Jumplistitem clicked
        /// </summary>
        [Option("jumplistcommand", Required = false, Hidden = true)]
        public virtual int? JumpListCommand { get; set; } = null;

        /// <summary>
        /// Whether or not to auto install (used by <see cref="ActionEnum.Start"/> ).
        /// </summary>
        [Option("autoinstall", Required = false, Hidden = true)]
        public virtual bool? AutoInstall { get; set; } = null;

        /// <summary>
        /// The type of query to do ( used by <see cref="ActionEnum.Query"/> ).
        /// Available options are in <see cref="PipeServiceHandler.ActionQueryEnum"/>
        /// </summary>
        [Option("query", Required = false, Hidden = true)]
        public virtual string? Query { get; set; }

        /// <summary>
        /// The uri data sent to the application via gamevault:// .
        /// This is a string representation of this <see cref="CommandOptions"/>
        /// </summary>
        [Option("uridata", Required = false, Hidden = true)]
        public string UriData
        {
            get => this.SerializeToUri();
            set => this.ParseFromUri(value);
        }

        /// <summary>
        /// The map of getters and setters for each property that needs to be (de)serialized to/from the <see cref="UriData"/>
        /// </summary>
        private static readonly Dictionary<string, (Func<CommandOptions, string?> get, Action<CommandOptions, string> set)> GettersAndSetters = new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(Minimized), (
                get: opt => opt.Minimized.HasValue ? opt.Minimized.ToString() : null,
                set: (opt, val) => { if (bool.TryParse(val, out var m)) opt.Minimized = m; }) },

            { nameof(GameId), (
                get: opt => opt.GameId.HasValue ? opt.GameId.ToString() : null,
                set: (opt, val) => { if (int.TryParse(val, out var id)) opt.GameId = id; }) },

            { nameof(JumpListCommand), (
                get: opt => opt.JumpListCommand.HasValue ? opt.JumpListCommand.ToString() : null,
                set: (opt, val) => { if (int.TryParse(val, out var id)) opt.JumpListCommand = id; }) },

            { nameof(AutoInstall), (
                get: opt => opt.AutoInstall.HasValue ? opt.AutoInstall.ToString() : null,
                set: (opt, val) => { if (bool.TryParse(val, out var m)) opt.AutoInstall = m; }) },

            { nameof(Query), (
                get: opt => opt.Query,
                set: (opt, val) => { opt.Query = val; }) },
        };

        /// <summary>
        /// Serializes this <see cref="CommandOptions"/> to a uri ( gamevault://action?param=value )
        /// </summary>
        private string SerializeToUri()
        {
            StringBuilder uri = new StringBuilder();

            uri.Append($"{PipeServiceHandler.GAMEVAULT_URI_SCHEME}://");
            uri.Append($"{Enum.GetName(this.Action)}");

            uri.Append('?');

            foreach (var getterAndSetter in GettersAndSetters)
            {
                var value = getterAndSetter.Value.get(this);

                if (!string.IsNullOrEmpty(value))
                {
                    uri.Append($"{HttpUtility.UrlEncode(getterAndSetter.Key)}={HttpUtility.UrlEncode(value)}&");
                }
            }

            // Remove the trailing &
            if (uri[uri.Length - 1] == '&')
                uri.Remove(uri.Length - 1, 1);

            return uri.ToString();
        }

        /*
gamevault:action?something=true
gamevault://action?something=true
gamevault://action/?something=true
gamevault://///action/////?something=true
gamevault-2://action?something=true
gamevault:action
gamevault://action
action?something=true
action
        */
        private static readonly System.Text.RegularExpressions.Regex matchUriRegex = new System.Text.RegularExpressions.Regex(@"
^
(?:[\w\-]+:/*)?
(?<action>\w+) \/* \?*
(?<params>.*)?
$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Parses a uri ( gamevault://action?param=value )
        /// </summary>
        private void ParseFromUri(string? uri)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            var match = matchUriRegex.Match(uri);

            if (!match.Success)
            {
                // An invalid uri was passed (which should be impossible since we accept almost anything)
                return;
            }

            string action = match.Groups["action"].Value;
            string? parameters = null;

            if (match.Groups["params"].Success)
                parameters = match.Groups["params"].Value;

            if (Enum.TryParse<ActionEnum>(action, true, out var actionEnum))
            {
                this.Action = actionEnum;
            }
            else
            {
                System.Windows.MessageBox.Show($"Invalid action: {action}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            if (!string.IsNullOrEmpty(parameters))
            {
                var paramsSplit = parameters.Split('&', StringSplitOptions.RemoveEmptyEntries);

                foreach (var param in paramsSplit)
                {
                    var splitParam = param.Split('=', 2, StringSplitOptions.TrimEntries);

                    if (splitParam.Length != 2)
                    {
                        // An invalid param was passed
                        continue;
                    }

                    var property = HttpUtility.UrlDecode(splitParam[0]);
                    var value = HttpUtility.UrlDecode(splitParam[1]);

                    if (GettersAndSetters.TryGetValue(property, out var getterAndSetter))
                    {
                        getterAndSetter.set(this, value);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="CommandOptions"/> from the provided uri ( gamevault://action?param=value )
        /// </summary>
        public static CommandOptions CreateFromUri(string? uri)
        {
            CommandOptions options = new CommandOptions();

            options.ParseFromUri(uri);

            return options;
        }
    }

    #region Verb instances of CommandOptions

    [Verb("show", HelpText = "Show GameVault, or optionally a specific game.")]
    public class CommandOptions_Show : CommandOptions
    {
        public override ActionEnum Action { get => ActionEnum.Show; set => base.Action = ActionEnum.Show; }

        [Option("minimized", Required = false, HelpText = "Start minimized.")]
        public new bool Minimized { get => base.Minimized.GetValueOrDefault(); set => base.Minimized = value; }

        [Option("gameid", Required = false, HelpText = $"The {nameof(Game.ID)} of the game to show, or null to simply open GameVault.", Default = null)]
        public override int? GameId { get => base.GameId; set => base.GameId = value; }

        [Option("jumplistcommand", Required = false, HelpText = $"Executes the command of the jump list item clicked", Default = null)]
        public override int? JumpListCommand { get => base.JumpListCommand.GetValueOrDefault(); set => base.JumpListCommand = value; }
    }

    [Verb("install", HelpText = "Install a game from GameVault.")]
    public class CommandOptions_Install : CommandOptions
    {
        public override ActionEnum Action { get => ActionEnum.Install; set => base.Action = ActionEnum.Install; }

        [Option("gameid", Required = true, HelpText = $"The {nameof(Game.ID)} of the game to install.")]
        public new int GameId { get => base.GameId.GetValueOrDefault(); set => base.GameId = value; }
    }

    [Verb("uninstall", HelpText = "Uninstall a game from GameVault.")]
    public class CommandOptions_Uninstall : CommandOptions
    {
        public override ActionEnum Action { get => ActionEnum.Uninstall; set => base.Action = ActionEnum.Uninstall; }

        [Option("gameid", Required = true, HelpText = $"The {nameof(Game.ID)} of the game to uninstall.")]
        public new int GameId { get => base.GameId.GetValueOrDefault(); set => base.GameId = value; }
    }

    [Verb("start", HelpText = "Start a game, or install it if necessary.")]
    public class CommandOptions_Start : CommandOptions
    {
        public override ActionEnum Action { get => ActionEnum.Start; set => base.Action = ActionEnum.Start; }

        [Option("gameid", Required = true, HelpText = $"The {nameof(Game.ID)} of the game to start.")]
        public new int GameId { get => base.GameId.GetValueOrDefault(); set => base.GameId = value; }

        [Option("autoinstall", Required = false, HelpText = "Install the game if it's not already installed.", Default = true)]
        public new bool AutoInstall { get => base.AutoInstall.GetValueOrDefault(); set => base.AutoInstall = value; }
    }

#if false
    Explicitly disabled STOP since we don't support that until we track active games
    Remove this message which causes a syntax error if implemented

    [Verb(nameof(ActionEnum.Stop), HelpText = "Stop a specific game or all games currently opened by GameVault.")]
    public class CommandOptions_Stop : CommandOptions
    {
        public override ActionEnum Action { get => ActionEnum.Stop; set => base.Action = ActionEnum.Stop; }

        [Option("gameid", Required = true, HelpText = $"The {nameof(Game.ID)} of the game to stop, or null to just close any open games.", Default = null)]
        public override int? GameId { get => base.GameId; set => base.GameId = value; }
    }
#endif

    #endregion
}
