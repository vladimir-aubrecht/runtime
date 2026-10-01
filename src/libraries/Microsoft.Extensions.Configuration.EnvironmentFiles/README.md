# Microsoft.Extensions.Configuration.EnvironmentFiles

Prototype environment-file provider for `Microsoft.Extensions.Configuration`.
The public API is intended for API review and is not an approved shipping contract.

## Contribution Bar

- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

## Scope

This optional package reads `.env` files into configuration without modifying
process environment variables or changing the default host configuration.
It reuses `Microsoft.Extensions.Configuration.FileExtensions` for file access,
optional files, reload notifications, and exception handling.

See [the package documentation](src/PACKAGE.md) for the supported syntax and usage.

Build and run the tests with the repository's `build-and-test` workflow.
