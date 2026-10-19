// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles;

/// <summary>
/// Provides configuration key-value pairs from an environment file without modifying process environment variables.
/// </summary>
/// <param name="source">The environment file configuration source.</param>
/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
/// <remarks>
/// Names are case-insensitive, and double underscores in names are replaced with the configuration key delimiter.
/// The last assignment to a configuration key wins. Values are not interpolated or unescaped.
/// </remarks>
public class EnvironmentFileConfigurationProvider(EnvironmentFileConfigurationSource source) : FileConfigurationProvider(source)
{
    private readonly EnvironmentFileSyntax _syntax = GetSyntax(source);
    private readonly IDisposable? _ownedFileProvider;
    private bool _hasLoaded;

    internal EnvironmentFileConfigurationProvider(EnvironmentFileConfigurationSource source, IDisposable? ownedFileProvider) : this(source)
    {
        _ownedFileProvider = ownedFileProvider;
    }

    /// <inheritdoc />
    /// <remarks>A failed initial file load releases change notifications and any owned file provider.</remarks>
    public override void Load()
    {
        try
        {
            base.Load();
            Volatile.Write(ref _hasLoaded, true);
        }
        catch
        {
            if (!Volatile.Read(ref _hasLoaded))
            {
                Dispose();
            }

            throw;
        }
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

    private static EnvironmentFileSyntax GetSyntax(EnvironmentFileConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Syntax;
    }
}
