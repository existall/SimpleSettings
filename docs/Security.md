# Security & Behavior

This page is the deep reference for SimpleSettings' security and behavior guarantees. Each guarantee is stated with its exact caveats — read the caveats, not just the headline, before you rely on any invariant.

## Secret redaction (value-free exceptions)

No bound configuration value — which may be a secret — appears in any library exception's `ToString()` chain that reaches your logs. Every exception SimpleSettings throws is **value-free**: it carries only type and property metadata, and it never chains an inner exception that saw the bound value. The guarantee is *structural*, not conventional: when a conversion fails, `SettingsPropertyValueException` is constructed from the failure's CLR `Type` (the converter error's `Type`, not the `Exception` and not the value), so there is no path for the value to travel into the message or an inner exception (Phase 4 `SECURITY.md:24-27`).

There are exactly **two carve-outs** — places the value-free guard does *not* reach:

- **Author-supplied `ValidationError` text.** The message you put in a `ValidationError` reaches `SettingsValidationException.ToString()` by design. The library treats it as author text, not a bound value, so it is never redacted. If you echo a secret into your own error message, it surfaces.
- **DI-resolved validator constructors.** When the container resolves a validator, the **constructor** runs *outside* the value-free bind guard. A constructor that logs or echoes an injected secret leaks it — the library cannot redact code it did not run.

Both carve-outs are documented accepted residuals of the Phase 4 security sign-off (`SECURITY.md:75-78`). Do not read the redaction invariant as "secrets never appear in any exception" — it covers the library's own exceptions, not your validator text or your validator constructors.

## Validators must not echo secrets

Follow two rules when authoring a validator:

1. Never put a bound value (a possible secret) into `ValidationError` text.
2. Never log or echo a secret in a validator **constructor** — DI resolution runs before the value-free bind guard.

The following validator is secret-safe: it inspects a value and reports a bound-independent message, echoing no bound value.

````C#
public class EmailSettingsValidator : ISettingValidation<IEmailSenderSettings>
{
    public ValidationResult Validate(ValidationContext<IEmailSenderSettings> context)
    {
        var result = new ValidationResult();
        if (context.Settings!.Retries < 0)
            result.AddError(new ValidationError(nameof(IEmailSenderSettings.Retries), "Retries must be >= 0"));
        return result; // never put a bound value (possible secret) in the message
    }
}
````

## Opt-in / deferred DI validation

DI-registered `ISettingValidation<T>` validators cannot run during `AddSimpleSettings` — the container is not built yet, so `AddSimpleSettings` only registers the validation runner and never invokes a validator. To run them, the host must call `ValidateSimpleSettings()` explicitly, **after** `BuildServiceProvider()`.

The method extends **`IServiceProvider`** — not `IServiceCollection`, not `IHost` — and returns the same provider so the call is chainable.

````C#
var serviceProvider = services.BuildServiceProvider();

// opt-in, deferred DI validation — runs the DI-registered validators now:
serviceProvider.ValidateSimpleSettings();
````

Attribute and `ValidatorType` validators (`[SettingsSection(ValidatorType = ...)]`, `[SettingsProperty(ValidatorType = ...)]`) run **inline during binding** and need no such call — `ValidateSimpleSettings()` is only for the DI-resolved `ISettingValidation<T>` path.

## Validators make a type discoverable

Declaring an object-level validator via `[SettingsSection(ValidatorType = typeof(EmailSettingsValidator))]` also marks the type as scan-discovered: the type now carries `SettingsSectionAttribute`, which is one of the discovery mechanisms. Attaching a validator to a settings interface is therefore sufficient on its own to make it a discovered settings section — a side effect worth knowing when you add a validator to a type that was not previously discovered.

````C#
[SettingsSection(ValidatorType = typeof(EmailSettingsValidator))]
public interface IEmailSenderSettings
{
    [SettingsProperty(DefaultValue = "https://smtp.example.com")]
    string ServiceUrl { get; set; }

    [SettingsProperty(DefaultValue = 3)]
    int Retries { get; set; }
}
````

## Command-line values with spaces

`AddCommandLine` sources the process arguments from `Environment.GetCommandLineArgs()` and skips the executable path (`arg[0]`) internally. For a prefixed key with no inline delimiter, the parser looks ahead to the **next** token and binds it as the value — **unless** that next token itself starts with a prefix character (`-` or `/`), in which case it is treated as a new key rather than a value.

So a shell-quoted value containing spaces arrives as a single token and binds:

````C#
// --Key "a b c" reaches the process as one token and binds as the value of Key
builder.AddCommandLine();
````

But a *value* that begins with `-` or `/` will not bind via the space-separated form — the lookahead treats it as a new key. For any value that may start with a prefix character, use the inline delimiter form instead:

````C#
// bind a value that begins with '-' or '/' — use the inline '=' delimiter
// --Key=-leading-dash-value
builder.AddCommandLine();
````

Prefer the inline `--Key=value` form whenever a value could begin with `-` or `/`.
