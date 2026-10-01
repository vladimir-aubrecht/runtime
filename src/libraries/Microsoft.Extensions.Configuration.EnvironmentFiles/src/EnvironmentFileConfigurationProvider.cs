// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles;

/// <summary>
/// Provides configuration key-value pairs from an environment file without modifying process environment variables.
/// </summary>
/// <remarks>
/// Names are case-insensitive, and double underscores in names are replaced with the configuration key delimiter.
/// The last assignment to a configuration key wins. Values are not interpolated or unescaped.
/// </remarks>
public class EnvironmentFileConfigurationProvider : FileConfigurationProvider
{
    private readonly EnvironmentFileSyntax _syntax;
    private readonly IDisposable? _ownedFileProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentFileConfigurationProvider"/> class.
    /// </summary>
    /// <param name="source">The environment file configuration source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public EnvironmentFileConfigurationProvider(EnvironmentFileConfigurationSource source) : this(source, ownedFileProvider: null)
    {
    }

    internal EnvironmentFileConfigurationProvider(EnvironmentFileConfigurationSource source, IDisposable? ownedFileProvider) : base(source)
    {
        _syntax = source.Syntax;
        _ownedFileProvider = ownedFileProvider;
    }

    /// <summary>
    /// Loads environment file data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">An entry does not conform to the enabled syntax.</exception>
    /// <remarks>The stream is closed after reading. An empty assignment supplies an empty string, not a null value.</remarks>
    public override void Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> entry in EnvironmentFileParser.Parse(stream, _syntax))
        {
            data[entry.Key.Replace("__", ConfigurationPath.KeyDelimiter)] = entry.Value;
        }

        Data = data;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _ownedFileProvider?.Dispose();
        }
    }
}
