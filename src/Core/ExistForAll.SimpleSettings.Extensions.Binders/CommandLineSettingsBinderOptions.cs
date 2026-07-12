using System;
using System.Collections.Generic;
using ExistForAll.SimpleSettings.Binder;

namespace ExistForAll.SimpleSettings.Binders
{
    public class CommandLineSettingsBinderOptions
    {
        private readonly List<char> _argumentPrefixes = new List<char>(new[] {'-', '/'});
        private readonly List<string> _delimiters = new List<string>(new[] {":", "="});

        public NameFormatter? NameFormatter { get; set; }

        public IEnumerable<char> ArgumentPrefixes => _argumentPrefixes;

        public IEnumerable<string> Delimiters => _delimiters;

        public bool IsCaseSensitive { get; set; } = true;

        public void AddArgumentPrefix(char prefix)
        {
            if (prefix <= 0) throw new ArgumentOutOfRangeException(nameof(prefix));
            _argumentPrefixes.Add(prefix);
        }

        public void AddDelimiter(string prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
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