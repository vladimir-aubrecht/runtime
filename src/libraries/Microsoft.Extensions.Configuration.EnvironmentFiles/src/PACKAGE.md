## About

Reads environment (`.env`) files as a configuration source. This is a minimal
API prototype: it does not claim full Docker Compose, shell, or Node.js dotenv
compatibility.

## How to use

```csharp
using System.IO;
using Microsoft.Extensions.Configuration;

string contentRootPath = Directory.GetCurrentDirectory();
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(contentRootPath)
    .AddEnvironmentFile(".env", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
```

`contentRootPath` is the application's chosen configuration directory. Relative
paths use the builder's `IFileProvider`; without one, the default base directory
is `AppContext.BaseDirectory`, not necessarily the project directory. The
provider does not search parent directories or automatically load other files.
The example also uses the existing EnvironmentVariables and CommandLine packages.

For default physical file access, this provider includes dot-prefixed files so
that `.env` can be read, and owns and disposes the physical provider it creates.
It uses the builder's physical provider for its base directory, not its filtering
or watcher configuration. Its watcher uses the standard
`DOTNET_USE_POLLING_FILE_WATCHER` environment setting. To control filtering or
watcher settings directly, supply `source.FileProvider` or the file-provider
overload; that provider retains its own settings and ownership. For an explicit
`PhysicalFileProvider`, use `ExclusionFilters.None` when dot-prefixed files must
be included. Non-physical builder file providers are used unchanged.

If initial file loading fails without being ignored by `OnLoadException`, the
configuration provider releases its change subscription and any physical
provider it owns. Supplied file providers remain caller-owned. Later reload
failures follow the normal file-configuration error policy without disposing
the provider, so a corrected file can be loaded again.

Later providers override earlier providers. In the example, process environment
variables and command-line arguments override file values. Adding a provider
after a host builder has already been initialized does not reinitialize host
settings such as the hosting environment.

## File format

```dotenv
# A full-line comment
export SERVER=localhost
ConnectionStrings__Default='a literal connection string'
LABEL=" surrounding whitespace is preserved "
EMPTY=
URL=https://example.invalid/#fragment
MODE=development # an inline comment
```

- Names use ASCII letters, digits, and underscores, and cannot start with a digit.
- Assignments use the first `=` as the separator; further `=` characters belong
  to the value.
- Blank lines and lines beginning with `#`, after trimming whitespace, are ignored.
- Unquoted values are trimmed. Quoted values preserve the characters between
  their enclosing quotes.
- Inline comments in unquoted values require whitespace before `#`. After a
  recognized closing quote, only whitespace and an enabled inline comment are
  allowed.
- The lowercase `export` keyword followed by whitespace can prefix an assignment.
- Values are always strings. `NAME=` supplies an empty string. A bare `NAME`
  without `=` is an error, not an unset operation.
- Configuration keys are case-insensitive. Double underscores become `:`, so
  `ConnectionStrings__Default` maps to `ConnectionStrings:Default`.
- The last assignment wins, including collisions after key normalization.
- Invalid entries fail with a line-numbered `FormatException`; file-loading
  exceptions follow the normal `FileConfigurationProvider` handling.

Single quotes, double quotes, inline comments, and export prefixes are
individually configurable through the source:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentFiles;

IConfiguration configuration = new ConfigurationBuilder()
    .AddEnvironmentFile(source =>
    {
        source.Path = ".env";
        source.Optional = true;
        source.Syntax = EnvironmentFileSyntax.SingleQuotedValues |
                        EnvironmentFileSyntax.DoubleQuotedValues;
    })
    .Build();
```

All four optional syntax features are enabled by default. When quote recognition
is disabled, those quote characters are literal. Disabling inline comments
preserves unquoted `#` characters and requires recognized quoted values to end
after their closing quote. Full-line comments remain supported in every mode.
An export prefix is an invalid variable name when its feature is disabled.

## Deliberately deferred

Interpolation, escape-sequence processing, multiline values, dialect presets,
prefix filtering, configurable key transformations, and a standalone public
parser are not implemented. Dollar expressions and backslashes are literal;
for example `${HOST}` and `\n` are not expanded. A recognized quoted value must
close on the same line and cannot escape its enclosing quote.

The provider never calls `Environment.SetEnvironmentVariable`. Parser output
keeps file order and raw names separate from configuration-key normalization,
leaving room for later opt-in transformations or interpolation without changing
this initial behavior.

## Main types

- `EnvironmentFileConfigurationExtensions`
- `EnvironmentFileConfigurationSource`
- `EnvironmentFileConfigurationProvider`
- `EnvironmentFileSyntax`
