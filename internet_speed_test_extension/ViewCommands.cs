using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace SpeedTest.Extension;

/// <summary>The two actions every view offers: switch to the other view (Ctrl+L) and run again (Ctrl+R).</summary>
internal static class ViewCommands
{
    private const int VirtualKeyL = 0x4C;
    private const int VirtualKeyR = 0x52;

    public static IContextItem[] For(SpeedTestSession session, Page otherView) => [
        new CommandContextItem(otherView)
        {
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, alt: false, shift: false, win: false, vkey: VirtualKeyL, scanCode: 0),
        },
        new CommandContextItem(new AnonymousCommand(session.Start)
        {
            Name = "Run again",
            Icon = new IconInfo(""),
            Result = CommandResult.KeepOpen(),
        })
        {
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, alt: false, shift: false, win: false, vkey: VirtualKeyR, scanCode: 0),
        },
    ];
}
