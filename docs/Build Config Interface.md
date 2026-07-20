# Build a Settings Interface

To create a Settings Interface you only need to build an interface with the property you need like so.

````C#
public interface ISomeInterface
{
    string SomeProperty { get; set; }

    [SettingsProperty(DefaultValue = new[] { 1, 2, 3, 4, 5 })]
    int[] ArrayOfIds { get; set; }
}
````

Now we need to mark the interface that this is a Settings Interface, SimpleSettings let you do this in three ways:
1. A base interface.
````C#
public interface ISomeInterface : ISettingsSection
{
    string SomeProperty { get; set; }

    [SettingsProperty(DefaultValue = new[] { 1, 2, 3, 4, 5 })]
    int[] ArrayOfIds { get; set; }
}
````
2. An Attribute.
````C#
[SettingsSection]
public interface ISomeInterface
{
    string SomeProperty { get; set; }

    [SettingsProperty(DefaultValue = new[] { 1, 2, 3, 4, 5 })]
    int[] ArrayOfIds { get; set; }
}
````

3. Interface name Suffix.
By default SimpleSettings scans for interfaces with a name ending with `Settings` and adds it to the collection. ( The suffix option can be changed using `SettingsOptions`, to know more look at the [Extend section](https://github.com/existall/SimpleSettings/blob/master/docs/Extending%20SimpleSettings.md) )

````C#
public interface ISomeInterfaceSettings
{
    string SomeProperty { get; set; }

    [SettingsProperty(DefaultValue = new[] { 1, 2, 3, 4, 5 })]
    int[] ArrayOfIds { get; set; }
}
````

## SimpleSettings is built to keep your application independent from frameworks so every indication of a settings interface can be changed using `SettingsOptions`

In the next page we will explain about the [SettingsProperty Attribute](https://github.com/existall/SimpleSettings/blob/master/docs/Default%20Values.md)
