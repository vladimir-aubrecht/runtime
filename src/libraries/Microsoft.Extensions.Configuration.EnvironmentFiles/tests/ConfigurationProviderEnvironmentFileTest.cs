// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles.Tests;

public class ConfigurationProviderEnvironmentFileTest : ConfigurationProviderTestBase
{
    protected override bool SupportNullValues => false;

    protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(TestSection testConfig)
    {
        var values = new List<KeyValuePair<string, string>>();
        SectionToValues(testConfig, "", values);
        var content = new StringBuilder();
        foreach (KeyValuePair<string, string> entry in values)
        {
            content.Append(entry.Key.Replace(":", "__")).Append("=\"").Append(entry.Value).AppendLine("\"");
        }

        var provider = new EnvironmentFileConfigurationProvider(new() { Optional = true });
        return (provider, () => provider.Load(new MemoryStream(Encoding.UTF8.GetBytes(content.ToString()))));
    }

    public override void Load_from_single_provider_with_duplicates_throws() =>
        AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesTestConfig)));

    public override void Load_from_single_provider_with_differing_case_duplicates_throws() =>
        AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.DuplicatesDifferentCaseTestConfig)));
}
