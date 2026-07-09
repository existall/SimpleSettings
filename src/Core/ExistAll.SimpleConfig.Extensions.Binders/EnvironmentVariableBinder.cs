using System;
using System.Collections;
using System.Text;

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
            var sb = new StringBuilder();

            if (Prefix != null)
                sb.Append(Prefix);

            sb.Append(VariableNameFormatter != null
                ? VariableNameFormatter(context.Section, context.Key)
                : context.Key);

            var variableName = sb.ToString();
            
            if(_environmentVariables.Contains(variableName))
                context.SetNewValue(_environmentVariables[variableName]);
        }
    }
}