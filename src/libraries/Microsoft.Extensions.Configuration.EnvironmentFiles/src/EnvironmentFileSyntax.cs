// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles;

/// <summary>
/// Specifies optional syntax recognized in environment files.
/// </summary>
[Flags]
public enum EnvironmentFileSyntax
{
    /// <summary>
    /// Recognizes assignments, blank lines, and full-line comments only.
    /// </summary>
    None = 0,

    /// <summary>
    /// Removes enclosing single quotes from values and preserves their contents literally.
    /// </summary>
    SingleQuotedValues = 1,

    /// <summary>
    /// Removes enclosing double quotes from values and preserves their contents literally.
    /// </summary>
    DoubleQuotedValues = 2,

    /// <summary>
    /// Recognizes comments introduced by <c>#</c> after whitespace in an unquoted value,
    /// or after the closing quote of a quoted value.
    /// </summary>
    InlineComments = 4,

    /// <summary>
    /// Recognizes and ignores the <c>export</c> keyword before an assignment.
    /// </summary>
    ExportPrefix = 8,
}
