using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.Binders
{
    public class CommandLineSettingsBinderOptions
    {
        private readonly List<char> _argumentPrefixes = [..new[] { '-', '/' }];
        private readonly List<string> _delimiters = [..new[] { ":", "=" }];

        public NameFormatter? NameFormatter { get; set; }

        /// <summary>Characters that mark a token as a key (default '-' and '/'). A space-separated value
        /// whose first character is one of these is treated as a new key rather than the previous key's
        /// value, so it does not bind; use the inline delimiter form to bind such a value.</summary>
        public IEnumerable<char> ArgumentPrefixes => _argumentPrefixes;

        public IEnumerable<string> Delimiters => _delimiters;

        public bool IsCaseSensitive { get; set; } = true;

        /// <summary>Skip args[0] (the executable path) when parsing. Default false so AddArguments binds
        /// exactly what it is handed (Main(string[]) already excludes the exe); AddCommandLine sets this
        /// true internally because Environment.GetCommandLineArgs() carries the exe at index 0.</summary>
        public bool SkipFirstArgument { get; set; } = false;

        public void AddArgumentPrefix(char prefix)
        {
            if (prefix <= 0) 
                throw new ArgumentOutOfRangeException(nameof(prefix));
            _argumentPrefixes.Add(prefix);
        }

        public void AddDelimiter(string prefix)
        {
            if (prefix == null) 
                throw new ArgumentNullException(nameof(prefix));
            _delimiters.Add(prefix);
        }

        public void ClearArgumentPrefixes()
        {
            _argumentPrefixes.Clear();
        }

        public void ClearDelimiters()
        {
            _delimiters.Clear();
        }
    }
}