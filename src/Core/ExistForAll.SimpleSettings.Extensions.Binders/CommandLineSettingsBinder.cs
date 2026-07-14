using System;
using System.Collections.Generic;
using System.Linq;
using ExistForAll.SimpleSettings;
using ExistForAll.SimpleSettings.Binders;

namespace ExistForAll.SimpleSettings.Binders
{
   public class CommandLineSettingsBinder : ISectionBinder
   {
      private readonly CommandLineSettingsBinderOptions _options;
      private readonly Dictionary<string, string> _argumentStore;

      public CommandLineSettingsBinder(string[] args, CommandLineSettingsBinderOptions options)
      {
         if (args == null) throw new ArgumentNullException(nameof(args));
         _options = options ?? throw new ArgumentNullException(nameof(options));

         _argumentStore =
            new Dictionary<string, string>(options.IsCaseSensitive
               ? StringComparer.Ordinal
               : StringComparer.OrdinalIgnoreCase);

         Parse(args);
      }

      public void BindPropertySettings(BindingContext context)
      {
         var key = _options.NameFormatter != null ? _options.NameFormatter(context.Section, context.Key) : context.Key;
         
         if(_argumentStore.TryGetValue(key, out var value))
         {
            context.SetNewValue(value);
         }
      }
      
      private void Parse(string[] args)
      {
         _argumentStore.Clear();

         if (args == null) return;

         var prefixes = _options.ArgumentPrefixes.ToArray();
         var start = _options.SkipFirstArgument && args.Length > 0 ? 1 : 0;

         for (var index = start; index < args.Length; index++)
         {
            var split = SplitByDelimiter(args[index], _options);
            if (split == null) continue;

            var (key, value) = split;
            var name = key.TrimStart(prefixes);

            if (value != null)
            {
               _argumentStore[name] = value;
               continue;
            }

            var tokenWasPrefixed = name.Length != key.Length;
            if (!tokenWasPrefixed || index + 1 >= args.Length) continue;

            var next = args[index + 1];
            if (next.Length > 0 && Array.IndexOf(prefixes, next[0]) >= 0) continue;

            _argumentStore[name] = next;
            index++;
         }
      }

      private static Tuple<string, string?>? SplitByDelimiter(string str, CommandLineSettingsBinderOptions options)
      {
         if (str == null)
            return null;

         string key;
         string? value;

         if (!options.Delimiters.Any())
         {
            key = str.Trim();
            value = null;
         }
         else
         {

            var indices = options.Delimiters.Where(delimiter => delimiter != null)
               .Select(d => str.IndexOf(d))
               .Where(d => d != -1).ToList();

            if (indices.Count == 0)
            {
               key = str.Trim();
               value = null;
            }
            else
            {
               var idx = indices.OrderBy(i => i).First();
               key = str.Substring(0, idx);
               value = str.Substring(idx + 1);
            }
         }

         return new Tuple<string, string?>(key, value);
      }
   }
}