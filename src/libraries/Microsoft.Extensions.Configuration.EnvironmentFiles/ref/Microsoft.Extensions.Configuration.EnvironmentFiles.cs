// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class EnvironmentFileConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider? provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.EnvironmentFiles.EnvironmentFileConfigurationSource>? configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.EnvironmentFiles
{
    public partial class EnvironmentFileConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
    {
        public EnvironmentFileConfigurationProvider(Microsoft.Extensions.Configuration.EnvironmentFiles.EnvironmentFileConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.FileConfigurationSource)) { }
        protected override void Dispose(bool disposing) { }
        public override void Load(System.IO.Stream stream) { }
    }
    public partial class EnvironmentFileConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
    {
        public EnvironmentFileConfigurationSource() { }
        public Microsoft.Extensions.Configuration.EnvironmentFiles.EnvironmentFileSyntax Syntax { get { throw null; } set { } }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
    [System.FlagsAttribute]
    public enum EnvironmentFileSyntax
    {
        None = 0,
        SingleQuotedValues = 1,
        DoubleQuotedValues = 2,
        InlineComments = 4,
        ExportPrefix = 8,
    }
}
