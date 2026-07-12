using System;
using System.Collections;

namespace ExistForAll.SimpleSettings.Binders
{
    public class EnvironmentVariableBinder : ISectionBinder
    {
        private readonly IDictionary _environmentVariables;
        public string? Prefix { get; }

        public Func<string, string, string>? VariableNameFormatter { get; set; }

        public EnvironmentVariableBinder(string prefix)
            : this()
        {
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        public EnvironmentVariableBinder()
        {
            _environmentVariables = Environment.GetEnvironmentVariables();
        }

        public void BindPropertySettings(BindingContext context)
        {
            // Fast path (the common case): with no prefix and no formatter the variable name is just the
            // key, so skip the StringBuilder allocation entirely.
            string variableName;
            if (Prefix == null && VariableNameFormatter == null)
            {
                variableName = context.Key;
            }
            else
            {
                var name = VariableNameFormatter != null
                    ? VariableNameFormatter(context.Section, context.Key)
                    : context.Key;
                variableName = Prefix != null ? Prefix + name : name;
            }

            // Single lookup: the non-generic IDictionary indexer returns null for an absent key, and an
            // environment variable is never null when present.
            var value = _environmentVariables[variableName];
            if (value != null)
                context.SetNewValue(value);
        }
    }
}