// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Extension.Pages;

namespace SpeedTest.Extension;

/// <summary>
/// Registers one top-level command, "Internet Speed Test", that opens the view chosen in settings.
/// Both views share one <see cref="SpeedTestSession"/>, so switching never restarts a running test.
/// </summary>
public partial class SpeedTestCommandsProvider : CommandProvider
{
    private readonly SettingsManager _settingsManager = new();
    private readonly SpeedTestSession _session = new();
    private readonly MeterPage _meterPage;
    private readonly DetailsPage _detailsPage;

    public SpeedTestCommandsProvider()
    {
        DisplayName = "Internet Speed Test";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Settings = _settingsManager.Settings;

        _meterPage = new MeterPage(_session);
        _detailsPage = new DetailsPage(_session);
        _meterPage.Commands = ViewCommands.For(_session, _detailsPage);
        _detailsPage.ItemCommands = ViewCommands.For(_session, _meterPage);

        _settingsManager.Settings.SettingsChanged += (_, _) => RaiseItemsChanged();
    }

    public override ICommandItem[] TopLevelCommands()
    {
        var defaultPage = _settingsManager.DefaultsToDetails ? (Page)_detailsPage : _meterPage;
        var otherPage = _settingsManager.DefaultsToDetails ? (Page)_meterPage : _detailsPage;
        return [
            new CommandItem(defaultPage)
            {
                Title = DisplayName,
                Subtitle = "Measure download, upload and latency",
                MoreCommands = [new CommandContextItem(otherPage)],
            },
        ];
    }

    public override void Dispose()
    {
        _session.Dispose();
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
