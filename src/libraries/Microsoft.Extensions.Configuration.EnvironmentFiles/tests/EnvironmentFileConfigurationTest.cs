// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles.Tests;

public class EnvironmentFileConfigurationTest
{
    private const EnvironmentFileSyntax DefaultSyntax =
        EnvironmentFileSyntax.SingleQuotedValues | EnvironmentFileSyntax.DoubleQuotedValues |
        EnvironmentFileSyntax.InlineComments | EnvironmentFileSyntax.ExportPrefix;

    [Theory]
    [InlineData("KEY=value", "value")]
    [InlineData("  KEY \t= value  ", "value")]
    [InlineData("KEY=", "")]
    [InlineData("KEY=   ", "")]
    [InlineData("KEY=\"\"", "")]
    [InlineData("KEY=''", "")]
    [InlineData("KEY=value=with=equals", "value=with=equals")]
    [InlineData("KEY=\"  value  \"", "  value  ")]
    [InlineData("KEY='  value  '", "  value  ")]
    [InlineData("KEY='a\"b'", "a\"b")]
    [InlineData("KEY=\"a'b\"", "a'b")]
    [InlineData("KEY=abc\"def\"ghi", "abc\"def\"ghi")]
    [InlineData("KEY=value # comment", "value")]
    [InlineData("KEY=value\t# comment", "value")]
    [InlineData("KEY=value#fragment", "value#fragment")]
    [InlineData("KEY=#fragment", "#fragment")]
    [InlineData("KEY= # comment", "")]
    [InlineData("KEY='a # b' # comment", "a # b")]
    [InlineData("KEY=\"a # b\"# comment", "a # b")]
    [InlineData("export KEY=value", "value")]
    [InlineData("export\t KEY = value", "value")]
    [InlineData("KEY=${OTHER:-default}", "${OTHER:-default}")]
    [InlineData("KEY=\"${OTHER}\"", "${OTHER}")]
    [InlineData("KEY=\"a\\nb\\tc\"", "a\\nb\\tc")]
    [InlineData("KEY=\"C:\\data\\\"", "C:\\data\\")]
    public void LoadsSupportedValues(string input, string expected)
    {
        using EnvironmentFileConfigurationProvider provider = Load(input);
        Assert.True(provider.TryGet("key", out string? value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void IgnoresBlankLinesAndFullLineComments(string newline)
    {
        using EnvironmentFileConfigurationProvider provider = Load(
            string.Join(newline, " ", "# comment", "\t# another", "KEY=value", ""));
        Assert.Equal(new[] { "KEY" }, provider.GetChildKeys(Array.Empty<string>(), parentPath: null));
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("value", value);
    }

    [Fact]
    public void ReadsUtf8ByteOrderMarkAndUnicodeValues()
    {
        byte[] content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("KEY=\u017elu\u0165ou\u010dk\u00fd \U0001F642")).ToArray();
        using var provider = new EnvironmentFileConfigurationProvider(new());
        provider.Load(new MemoryStream(content));
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("\u017elu\u0165ou\u010dk\u00fd \U0001F642", value);
    }

    [Theory]
    [InlineData(EnvironmentFileSyntax.SingleQuotedValues, "KEY=' value '", "' value '")]
    [InlineData(EnvironmentFileSyntax.DoubleQuotedValues, "KEY=\" value \"", "\" value \"")]
    [InlineData(EnvironmentFileSyntax.InlineComments, "KEY=value # comment", "value # comment")]
    public void SyntaxFeaturesCanBeDisabledIndependently(EnvironmentFileSyntax feature, string input, string expected)
    {
        using EnvironmentFileConfigurationProvider provider = Load(input, DefaultSyntax & ~feature);
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void ExportPrefixCanBeDisabled()
    {
        Assert.Throws<FormatException>(() => Load("export KEY=value", DefaultSyntax & ~EnvironmentFileSyntax.ExportPrefix));
        using EnvironmentFileConfigurationProvider provider = Load("exportKEY=value", EnvironmentFileSyntax.None);
        Assert.True(provider.TryGet("exportKEY", out string? value));
        Assert.Equal("value", value);
    }

    [Theory]
    [InlineData("KEY=\"value\" # comment")]
    [InlineData("KEY='value'# comment")]
    public void DisabledInlineCommentsAreRejectedAfterQuotedValues(string input)
    {
        Assert.Throws<FormatException>(() => Load(input, DefaultSyntax & ~EnvironmentFileSyntax.InlineComments));
    }

    [Theory]
    [InlineData("KEY")]
    [InlineData("=value")]
    [InlineData("   =value")]
    [InlineData("1KEY=value")]
    [InlineData("K EY=value")]
    [InlineData("K-EY=value")]
    [InlineData("K.EY=value")]
    [InlineData("K:EY=value")]
    [InlineData("\u017d=value")]
    [InlineData("export KEY")]
    [InlineData("export =value")]
    [InlineData("KEY=\"unclosed")]
    [InlineData("KEY='unclosed")]
    [InlineData("KEY=\"value\"extra")]
    [InlineData("KEY='value' extra")]
    [InlineData("KEY=\"line1\nline2\"")]
    [InlineData("KEY='line1\nline2'")]
    [InlineData("KEY=\"escaped\\\"quote\"")]
    public void RejectsInvalidEntriesWithLineNumbers(string invalidLine)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Load("# heading\n" + invalidLine));
        Assert.Contains("line 2", exception.Message);
    }

    [Fact]
    public void ErrorsDoNotIncludeConfigurationValues()
    {
        FormatException exception = Assert.Throws<FormatException>(() => Load("KEY=\"DO_NOT_LOG\"suffix"));
        Assert.DoesNotContain("DO_NOT_LOG", exception.Message);
    }

    [Theory]
    [InlineData("KEY=first\nKEY=second\nKEY=last")]
    [InlineData("KEY=first\nkey=second\nKEY=last")]
    public void LastAssignmentWinsInFileOrder(string input)
    {
        using EnvironmentFileConfigurationProvider provider = Load(input);
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("last", value);
        Assert.Single(provider.GetChildKeys(Array.Empty<string>(), parentPath: null));
    }

    [Fact]
    public void NormalizesNestedKeysAndCaseCollisionsInFileOrder()
    {
        using EnvironmentFileConfigurationProvider provider = Load("Section__Key=first\nsection__key=second\nSection__Key=last");
        Assert.True(provider.TryGet("SECTION:KEY", out string? value));
        Assert.Equal("last", value);
        Assert.False(provider.TryGet("Section__Key", out _));
    }

    [Fact]
    public void SyntaxNonePreservesQuotesAndInlineCommentText()
    {
        using EnvironmentFileConfigurationProvider provider = Load("# full line\nKEY='a # b'", EnvironmentFileSyntax.None);
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("'a # b'", value);
    }

    [Fact]
    public void DoesNotModifyProcessEnvironment()
    {
        string name = "DOTNET_ENVFILE_TEST_" + Guid.NewGuid().ToString("N");
        Assert.Null(Environment.GetEnvironmentVariable(name));
        using EnvironmentFileConfigurationProvider provider = Load(name + "=file-value");
        Assert.True(provider.TryGet(name, out string? value));
        Assert.Equal("file-value", value);
        Assert.Null(Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void FailedStreamLoadPreservesPreviouslyLoadedData()
    {
        using EnvironmentFileConfigurationProvider provider = Load("KEY=old");
        Assert.Throws<FormatException>(() => provider.Load(TextStream("KEY=new\ninvalid")));
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("old", value);
    }

    [Fact]
    public void StreamLoadClosesTheStream()
    {
        using var stream = TextStream("KEY=value");
        using var provider = new EnvironmentFileConfigurationProvider(new());
        provider.Load(stream);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public void CapturesSyntaxWhenProviderIsBuilt()
    {
        var source = new EnvironmentFileConfigurationSource { Syntax = EnvironmentFileSyntax.None };
        using var provider = new EnvironmentFileConfigurationProvider(source);
        source.Syntax = DefaultSyntax;
        provider.Load(TextStream("KEY='value'"));
        Assert.True(provider.TryGet("KEY", out string? value));
        Assert.Equal("'value'", value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void RejectsUnknownSyntaxFlags(int value)
    {
        var source = new EnvironmentFileConfigurationSource();
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => source.Syntax = (EnvironmentFileSyntax)value);
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ValidatesNullArguments()
    {
        Assert.Throws<ArgumentNullException>("source", () => new EnvironmentFileConfigurationProvider(null));
        using var provider = new EnvironmentFileConfigurationProvider(new());
        Assert.Throws<ArgumentNullException>("stream", () => provider.Load(null));
        Assert.Throws<ArgumentNullException>("builder", () => EnvironmentFileConfigurationExtensions.AddEnvironmentFile(null, ".env"));
        Assert.Throws<ArgumentNullException>("builder", () => EnvironmentFileConfigurationExtensions.AddEnvironmentFile(null, _ => { }));
        Assert.Throws<ArgumentNullException>("builder", () => new EnvironmentFileConfigurationSource().Build(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RejectsEmptyFilePaths(string path)
    {
        Assert.Throws<ArgumentException>("path", () => new ConfigurationBuilder().AddEnvironmentFile(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingFileHonorsOptional(bool optional)
    {
        using var files = new TestFileProvider(null);
        var builder = new ConfigurationBuilder().AddEnvironmentFile(files, ".env", optional, reloadOnChange: false);
        if (optional)
        {
            using var root = (ConfigurationRoot)builder.Build();
            Assert.Empty(root.GetChildren());
        }
        else
        {
            Assert.Throws<FileNotFoundException>(() => builder.Build());
        }
    }

    [Fact]
    public void UsesBuilderFileProviderAndConfiguredSource()
    {
        using var files = new TestFileProvider("KEY='value'");
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .SetFileProvider(files)
            .AddEnvironmentFile(source =>
            {
                source.Path = "custom.env";
                source.Syntax = EnvironmentFileSyntax.None;
            })
            .Build();
        Assert.Equal("custom.env", files.LastPath);
        Assert.Equal("'value'", root["KEY"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadsPhysicalDotFileAndCanRebuild(bool absolutePath)
    {
        string directory = Path.Combine(Path.GetTempPath(), "env-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, ".env");
        try
        {
            File.WriteAllText(path, "KEY=value");
            var builder = new ConfigurationBuilder().SetBasePath(directory)
                .AddEnvironmentFile(absolutePath ? path : ".env");
            using (var first = (ConfigurationRoot)builder.Build())
            {
                Assert.Equal("value", first["KEY"]);
            }
            using var second = (ConfigurationRoot)builder.Build();
            Assert.Equal("value", second["KEY"]);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    [Theory]
    [InlineData(ExclusionFilters.None, true)]
    [InlineData(ExclusionFilters.Sensitive, false)]
    public void HonorsExplicitPhysicalFileProviderFilters(ExclusionFilters filters, bool expected)
    {
        string directory = Path.Combine(Path.GetTempPath(), "env-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, ".env");
        try
        {
            File.WriteAllText(path, "KEY=value");
            using var files = new PhysicalFileProvider(directory, filters);
            using var root = (ConfigurationRoot)new ConfigurationBuilder()
                .AddEnvironmentFile(files, ".env", optional: true, reloadOnChange: false)
                .Build();
            Assert.Equal(expected ? "value" : null, root["KEY"]);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void InitializedBuilderWatcherDoesNotOverrideNewWatcherDefaults()
    {
        using var builderFiles = new PhysicalFileProvider(Path.GetTempPath())
        {
            UsePollingFileWatcher = true,
            UseActivePolling = true,
        };
        builderFiles.Watch("env-file-existing-" + Guid.NewGuid().ToString("N"));
        using var defaults = new PhysicalFileProvider(Path.GetTempPath(), ExclusionFilters.None);
        using var provider = (EnvironmentFileConfigurationProvider)new EnvironmentFileConfigurationSource
        {
            Path = ".env",
            Optional = true,
        }.Build(new ConfigurationBuilder().SetFileProvider(builderFiles));

        PhysicalFileProvider actual = Assert.IsType<PhysicalFileProvider>(provider.Source.FileProvider);
        Assert.NotSame(builderFiles, actual);
        Assert.Equal(defaults.UsePollingFileWatcher, actual.UsePollingFileWatcher);
        Assert.Equal(defaults.UseActivePolling, actual.UseActivePolling);
    }

    [Fact]
    public void LaterProvidersOverrideFileValues()
    {
        using var files = new TestFileProvider("KEY=file\nEMPTY=");
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["KEY"] = "before", ["EMPTY"] = "before", ["KEEP"] = "before" })
            .AddEnvironmentFile(files, ".env", optional: false, reloadOnChange: false)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["KEY"] = "after" })
            .Build();
        Assert.Equal("after", root["KEY"]);
        Assert.Equal("", root["EMPTY"]);
        Assert.Equal("before", root["KEEP"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReloadAndDisposalFollowFileProviderSemantics(bool reloadOnChange)
    {
        using var files = new TestFileProvider("KEY=first\nREMOVED=value");
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .AddEnvironmentFile(source =>
            {
                source.FileProvider = files;
                source.Path = ".env";
                source.ReloadOnChange = reloadOnChange;
                source.ReloadDelay = 0;
            })
            .Build();

        IChangeToken changed = root.GetReloadToken();
        files.SetContent("KEY=second");
        Assert.Equal(reloadOnChange, changed.HasChanged);
        Assert.Equal(reloadOnChange ? "second" : "first", root["KEY"]);
        Assert.Equal(reloadOnChange ? null : "value", root["REMOVED"]);

        root.Reload();
        Assert.Equal("second", root["KEY"]);
        Assert.Null(root["REMOVED"]);

        root.Dispose();
        int readCount = files.ReadCount;
        files.SetContent("KEY=third");
        Assert.Equal(readCount, files.ReadCount);
    }

    [Fact]
    public void ReloadedMissingFileFallsBackToEarlierProvider()
    {
        using var files = new TestFileProvider("KEY=file");
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["KEY"] = "fallback" })
            .AddEnvironmentFile(source =>
            {
                source.FileProvider = files;
                source.Path = ".env";
                source.ReloadOnChange = true;
                source.ReloadDelay = 0;
            })
            .Build();
        Assert.Equal("file", root["KEY"]);
        files.SetContent(null);
        Assert.Equal("fallback", root["KEY"]);
    }

    [Fact]
    public void FileLoadErrorsUseExistingExceptionCallback()
    {
        using var files = new TestFileProvider("invalid");
        FileLoadExceptionContext captured = null;
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .AddEnvironmentFile(source =>
            {
                source.FileProvider = files;
                source.Path = ".env";
                source.OnLoadException = context =>
                {
                    captured = context;
                    context.Ignore = true;
                };
            })
            .Build();
        Assert.NotNull(captured);
        InvalidDataException exception = Assert.IsType<InvalidDataException>(captured.Exception);
        Assert.IsType<FormatException>(exception.InnerException);
        Assert.Empty(root.GetChildren());
    }

    [Fact]
    public void MalformedAutomaticReloadExposesFallbackAndRecovers()
    {
        using var files = new TestFileProvider("KEY=file");
        FileLoadExceptionContext captured = null;
        using var root = (ConfigurationRoot)new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["KEY"] = "fallback" })
            .AddEnvironmentFile(source =>
            {
                source.FileProvider = files;
                source.Path = ".env";
                source.ReloadOnChange = true;
                source.ReloadDelay = 0;
                source.OnLoadException = context =>
                {
                    captured = context;
                    context.Ignore = true;
                };
            })
            .Build();
        Assert.Equal("file", root["KEY"]);

        IChangeToken failedReload = root.GetReloadToken();
        files.SetContent("KEY=partial\ninvalid");
        Assert.NotNull(captured);
        InvalidDataException exception = Assert.IsType<InvalidDataException>(captured.Exception);
        Assert.Contains("line 2", Assert.IsType<FormatException>(exception.InnerException).Message);
        Assert.True(failedReload.HasChanged);
        Assert.Equal("fallback", root["KEY"]);

        IChangeToken recoveredReload = root.GetReloadToken();
        files.SetContent("KEY=recovered");
        Assert.True(recoveredReload.HasChanged);
        Assert.Equal("recovered", root["KEY"]);
    }

    private static EnvironmentFileConfigurationProvider Load(string text, EnvironmentFileSyntax syntax = DefaultSyntax)
    {
        var provider = new EnvironmentFileConfigurationProvider(new() { Syntax = syntax });
        provider.Load(TextStream(text));
        return provider;
    }

    private static MemoryStream TextStream(string text) => new(Encoding.UTF8.GetBytes(text));

    private sealed class TestFileProvider(string content) : IFileProvider, IDisposable
    {
        private string _content = content;
        private CancellationTokenSource _changes = new();

        public string LastPath { get; private set; }
        public int ReadCount { get; private set; }

        public IFileInfo GetFileInfo(string subpath)
        {
            LastPath = subpath;
            return _content is null ? new NotFoundFileInfo(subpath) : new TestFileInfo(this, _content);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
        public IChangeToken Watch(string filter) => new CancellationChangeToken(_changes.Token);
        public void Dispose() => _changes.Dispose();

        public void SetContent(string content)
        {
            _content = content;
            CancellationTokenSource previous = _changes;
            _changes = new();
            previous.Cancel();
            previous.Dispose();
        }

        private sealed class TestFileInfo(TestFileProvider owner, string content) : IFileInfo
        {
            public bool Exists => true;
            public long Length => Encoding.UTF8.GetByteCount(content);
            public string PhysicalPath => null;
            public string Name => ".env";
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;
            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                owner.ReadCount++;
                return TextStream(content);
            }
        }
    }
}
