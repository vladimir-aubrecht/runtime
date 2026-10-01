// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles;

/// <summary>
/// Represents an environment file as an <see cref="IConfigurationSource"/>.
/// </summary>
public class EnvironmentFileConfigurationSource : FileConfigurationSource
{
    private const EnvironmentFileSyntax SupportedSyntax =
        EnvironmentFileSyntax.SingleQuotedValues | EnvironmentFileSyntax.DoubleQuotedValues |
        EnvironmentFileSyntax.InlineComments | EnvironmentFileSyntax.ExportPrefix;

    private EnvironmentFileSyntax _syntax =
        EnvironmentFileSyntax.SingleQuotedValues | EnvironmentFileSyntax.DoubleQuotedValues |
        EnvironmentFileSyntax.InlineComments | EnvironmentFileSyntax.ExportPrefix;

    /// <summary>
    /// Gets or sets the optional syntax recognized in the environment file.
    /// </summary>
    /// <value>
    /// A bitwise combination of the enumeration values.
    /// The default enables single-quoted values, double-quoted values, inline comments, and export prefixes.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains unsupported syntax options.</exception>
    /// <remarks>The syntax options are captured when the provider is built.</remarks>
    public EnvironmentFileSyntax Syntax
    {
        get => _syntax;
        set
        {
            if ((value & ~SupportedSyntax) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, SR.Error_InvalidSyntax);
            }

            _syntax = value;
        }
    }

    /// <summary>
    /// Builds an <see cref="EnvironmentFileConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>An environment file configuration provider.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (FileProvider is not null)
        {
            EnsureDefaults(builder);
            return new EnvironmentFileConfigurationProvider(this);
        }

        string? path = Path;
        string directory;
        if (!string.IsNullOrEmpty(path) && System.IO.Path.IsPathRooted(path))
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            directory = System.IO.Path.GetDirectoryName(fullPath) ?? fullPath;
            path = System.IO.Path.GetFileName(fullPath);
        }
        else
        {
            IFileProvider defaultProvider = builder.GetFileProvider();
            if (defaultProvider.GetType() == typeof(PhysicalFileProvider))
            {
                directory = ((PhysicalFileProvider)defaultProvider).Root;
            }
            else
            {
                EnsureDefaults(builder);
                return new EnvironmentFileConfigurationProvider(this);
            }
        }

        // The default physical provider excludes dot-prefixed files, including .env.
        var fileProvider = new PhysicalFileProvider(directory, ExclusionFilters.None);
        try
        {
            var source = new EnvironmentFileConfigurationSource
            {
                FileProvider = fileProvider,
                Optional = Optional,
                ReloadOnChange = ReloadOnChange,
                ReloadDelay = ReloadDelay,
                OnLoadException = OnLoadException ?? builder.GetFileLoadExceptionHandler(),
                Syntax = Syntax,
            };
            if (path is not null)
            {
                source.Path = path;
            }

            return new EnvironmentFileConfigurationProvider(source, fileProvider);
        }
        catch
        {
            fileProvider.Dispose();
            throw;
        }
    }
}
