// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace internet_speed_test_extension;

public partial class internet_speed_test_extensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public internet_speed_test_extensionCommandsProvider()
    {
        DisplayName = "Internet Speed Test";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new internet_speed_test_extensionPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
