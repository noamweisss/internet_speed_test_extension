using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace SpeedTest.Extension;

/// <summary>
/// Declares the user settings. Command Palette renders the settings page and persists the values;
/// this class only defines them and reads them back. Setting ids are persisted strings: never rename them.
/// </summary>
internal sealed class SettingsManager
{
    public const string MeterView = "meter";
    public const string DetailsView = "details";
    private const string DefaultViewKey = "defaultView";

    public SettingsManager()
    {
        Settings.Add(new ChoiceSetSetting(
            DefaultViewKey,
            "Default view",
            "Which view opens when you run Internet Speed Test",
            new List<ChoiceSetSetting.Choice>
            {
                new("Meter dashboard", MeterView),
                new("Detailed results", DetailsView),
            }));
    }

    public Settings Settings { get; } = new();

    /// <summary>True when the user chose the details list. Anything else (including no value yet) means the meter.</summary>
    public bool DefaultsToDetails => Settings.GetSetting<string>(DefaultViewKey) == DetailsView;
}
