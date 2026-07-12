# Default Values

Default values allow you to instantiate properties with default values on run time, if no value has been set by the binders SimpleSettings will check to see if there is a `SettingsProperty` attribute with a `DefaultValue` and set it to the property.

### Disclaimer: at this time your application is coupled with the `SettingsProperty` attribute, yet SimpleSettings will check for inheritance thus you can inherit from `SettingsPropertyAttribute` and decouple your application this way.

### Supported Types and values
`DefaultValue` accepts any value (including arrays) that can be converted to the property type, for example;

````C#

[SettingsProperty(DefaultValue = "Moby Dick")]
public string Name { get; set; }

[SettingsProperty(DefaultValue = new[] { 1, 2, 3, 8 })]
public int[] Ids { get; set; }
````

SimpleSettings checks what is the return property type and tries to convert it into the correct value.

`DefaultValue` also supports : `Enum`, `DateTime`, `Uri`, arrays and `IEnumerable<T>` collections.


Since many developers use different `DateTime` formats, SimpleSettings allows you to change the parser format. The default parser format is `"yyyy-MM-dd"` yet you can change it by placing a different format in `SettingsOptions.DateTimeFormat`.

````C#
[SettingsProperty(DefaultValue = "2012-09-19")]
public DateTime StartTime { get; set; }
````

### More `SettingsProperty` options
The `SettingsProperty` attribute exposes a few more members beyond `DefaultValue`:
1. `Name` - override the key the binders look up for this property (defaults to the property name).
2. `ConverterType` - use a specific `ISettingsTypeConverter` for this property.
3. `AllowEmpty` - when set to `false`, binding throws if no value was resolved for the property (defaults to `true`).

In the next page we will discuss how to create a [SectionBinder](https://github.com/existall/SimpleSettings/blob/master/docs/Build%20a%20SectionBinder.md)
