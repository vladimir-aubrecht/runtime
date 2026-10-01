// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration.EnvironmentFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Provides extension methods for adding environment file configuration providers.
/// </summary>
public static class EnvironmentFileConfigurationExtensions
{
    /// <summary>
    /// Adds an environment file configuration provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The file path relative to the builder's base path.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder builder, string path) =>
        AddEnvironmentFile(builder, provider: null, path, optional: false, reloadOnChange: false);

    /// <summary>
    /// Adds an environment file configuration provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The file path relative to the builder's base path.</param>
    /// <param name="optional"><see langword="true"/> if the file is optional; otherwise, <see langword="false"/>.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder builder, string path, bool optional) =>
        AddEnvironmentFile(builder, provider: null, path, optional, reloadOnChange: false);

    /// <summary>
    /// Adds an environment file configuration provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">The file path relative to the builder's base path.</param>
    /// <param name="optional"><see langword="true"/> if the file is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="reloadOnChange"><see langword="true"/> to reload when the file changes; otherwise, <see langword="false"/>.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) =>
        AddEnvironmentFile(builder, provider: null, path, optional, reloadOnChange);

    /// <summary>
    /// Adds an environment file configuration provider using the specified file provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="provider">The file provider, or <see langword="null"/> to use the builder's default provider.</param>
    /// <param name="path">The file path relative to the file provider's root.</param>
    /// <param name="optional"><see langword="true"/> if the file is optional; otherwise, <see langword="false"/>.</param>
    /// <param name="reloadOnChange"><see langword="true"/> to reload when the file changes; otherwise, <see langword="false"/>.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException(SR.Error_InvalidFilePath, nameof(path));
        }

        return builder.AddEnvironmentFile(source =>
        {
            source.FileProvider = provider;
            source.Path = path;
            source.Optional = optional;
            source.ReloadOnChange = reloadOnChange;
        });
    }

    /// <summary>
    /// Adds an environment file configuration provider with the specified source configuration.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configureSource">The action that configures the source.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IConfigurationBuilder AddEnvironmentFile(this IConfigurationBuilder builder, Action<EnvironmentFileConfigurationSource>? configureSource)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(configureSource);
    }
}
