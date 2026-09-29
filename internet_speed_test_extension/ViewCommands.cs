using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Core;

namespace SpeedTest.Extension;

/// <summary>
/// The actions every view offers: switch to the other view (Ctrl+L), run again (Ctrl+R), copy the summary
/// (Ctrl+Shift+C) and copy it as a markdown table (Ctrl+Shift+M). The copy commands read the session's snapshot
/// when invoked, not when the page is built, so they copy what is on screen.
/// </summary>
internal static class ViewCommands
{
    private const int VirtualKeyC = 0x43;
    private const int VirtualKeyL = 0x4C;
    private const int VirtualKeyM = 0x4D;
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
        Copy(session, "Copy summary", ResultSummary.PlainText, VirtualKeyC),
        Copy(session, "Copy as markdown", ResultSummary.Markdown, VirtualKeyM),
    ];

    private static CommandContextItem Copy(SpeedTestSession session, string name, Func<SpeedTestSnapshot, string> render, int vkey) =>
        new(new AnonymousCommand(() => ClipboardHelper.SetText(render(session.Snapshot)))
        {
            Name = name,
            Icon = new IconInfo(""),
            Result = CommandResult.ShowToast("Copied"),
        })
        {
            RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, alt: false, shift: true, win: false, vkey: vkey, scanCode: 0),
        };
}
