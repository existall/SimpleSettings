# Build a SectionBinder
SectionBinders are SimpleSettings' way to pass values into properties not only from the `SettingsProperty` default value but from other data sources.

Out of the box SimpleSettings provides these Binders:
1. `InMemoryCollection` - lives in the core package.
2. `ConfigurationBinder` - extracts values from .NET `IConfiguration`.
3. `EnvironmentVariableBinder` - reads values from environment variables.
4. A command line binder - reads values from command line arguments.

The last three live in the `ExistForAll.SimpleSettings.Binders` package.


### Building a SectionBinder

A SectionBinder is an implementation of the `ISectionBinder` interface.

SimpleSettings invokes the next method

`void BindPropertySettings(BindingContext context);`

- The context provides the current `Section`, `Key` and the current value up till now (`CurrentValue`).
- When you resolve a value from your data source, call `context.SetNewValue(value)` and SimpleSettings will try to convert it and set it into the property.
- If you have nothing to contribute, simply don't call `SetNewValue` and the current value is preserved for the next binder.

````C#
public class MySectionBinder : ISectionBinder
{
    public void BindPropertySettings(BindingContext context)
    {
        if (TryLookup(context.Section, context.Key, out var value))
            context.SetNewValue(value);
    }
}
````

### Section
The section name the context provides is the name of the interface with some manipulation, for example the interface `ISomeInterface` will be provided by the context as `SomeInterface`. SimpleSettings removes the leading `I`.
#### this is configurable via `SettingsOptions` (or per interface with `[SettingsSection("name")]`) and can be viewed on the [Extend section](https://github.com/existall/SimpleSettings/blob/master/docs/Extend%20Simple%20Config.md)

### Key
The Key name the context provides is the property name as is, for example the interface `ISomeInterface.SomeProperty` will be provided as `SomeProperty`. You can override it per property with `[SettingsProperty(Name = "...")]`.

### Current Value

Since order between the binders is important the context will provide the current value (as `object?`) that the former binders have set already, starting from the property's default value.

### In-memory values

The `InMemoryCollection` binder is the simplest way to feed values. Create a collection, add your `section`/`key`/`value` entries, and wire it up with `AddInMemoryCollection`:

````C#
var collection = new InMemoryCollection();
collection.Add("SomeInterface", "SomeProperty", "value");

var settings = SettingsBuilder
    .CreateBuilder(factory => factory.AddInMemoryCollection(collection))
    .GetSettings<ISomeInterface>();
````

In the next page we will learn how to [Extend](https://github.com/existall/SimpleSettings/blob/master/docs/Extend%20Simple%20Config.md) SimpleSettings and future features.
