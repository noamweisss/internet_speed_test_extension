# Install

How to install a build of the extension without Visual Studio. Why it works this way:
`docs/decisions/ADR-0008-install-without-visual-studio.md`. Safety background: `docs/SAFETY-CONTRACT.md` §5.

You need Windows 10 or 11, x64. Windows 11 Pro can use Windows Sandbox (path A, recommended).

## 1. Download a build (on your PC)

Only use a build that CI made from a commit you can see on GitHub.

1. Open the repository on GitHub → **Actions** → workflow **CI**.
2. Pick a run with a green tick whose branch is **main** (or a branch of this repository you are testing).
   Never a run from a pull request opened from someone else's fork.
3. At the bottom of the run page, under **Artifacts**, click `internet-speed-test-extension-x64-<commit>`
   (the last part is the first 7 characters of the commit, shown on the run page).
   A `.zip` downloads. It holds two files: the package (`.msix`) and `Install-SpeedTestExtension.ps1`.

## 2A. Test in Windows Sandbox (recommended)

Sandbox is a disposable Windows. When you close it, everything inside is deleted. Your real PC is not touched.

One-time setup: Start → type **Turn Windows features on or off** → tick **Windows Sandbox** → OK → restart.

1. On your PC, also download PowerToys: <https://github.com/microsoft/PowerToys/releases> → latest release →
   **Assets** → `PowerToysUserSetup-<version>-x64.exe`.
2. Start → **Windows Sandbox**. Copy both downloaded files on your PC (Ctrl+C) and paste them onto the
   Sandbox desktop (Ctrl+V).
3. In Sandbox, run the PowerToys installer. When it finishes, PowerToys opens; check that **Command Palette**
   is enabled.
4. In Sandbox, turn on Developer Mode: Settings → System → For developers → **Developer Mode** → On.
5. Right-click the zip → **Extract All**. Open the extracted folder, right-click an empty area →
   **Open in Terminal**, and run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Install-SpeedTestExtension.ps1
   ```

6. Press **Win+Alt+Space**, run **Reload**, then search **Internet Speed Test**.
7. Run the checklist in `docs/TESTING.md`. Close Sandbox when done; nothing is kept.

If your PC runs a Windows Insider (preview) build, Sandbox runs that same preview Windows and may crash. That
happened on the owner's PC (a Windows bug, not the extension). Use a virtual machine instead (2C).

## 2B. Install on your real PC

Same as 2A steps 3–6, without Sandbox, with these precautions:

- Use PowerToys you installed from the official release page or the Microsoft Store.
- Turn Developer Mode on only for the install. If you turn it off afterwards and the extension stops loading,
  that is expected: turn it on again, or uninstall.
- Turning Developer Mode on may ask for administrator approval. The script never does; if it does, stop.

## 2C. Test in a Hyper-V virtual machine

Like Sandbox, but it keeps its state and has its own released (non-preview) Windows. Set it up once: Windows 11
in Hyper-V, PowerToys and Developer Mode inside, then save a checkpoint. For each build: copy the zip into the
VM, do 2A steps 5–7 there, save the logs, then revert to the checkpoint. The owner's VM, and the scripts that copy
files in and collect logs, live on the owner's laptop, not in this repository (`docs/SESSION-LOG.md`, session 2).

## Update or remove

- **Update**: download the newer build and run the same command. The old version is removed first.
- **Remove**: in the extracted folder, run

  ```powershell
  powershell -ExecutionPolicy Bypass -File .\Install-SpeedTestExtension.ps1 -Uninstall
  ```

## What the command does

- `-ExecutionPolicy Bypass` lets this one PowerShell command run a script file. Windows blocks script files by
  default. The setting ends when the command ends; it changes nothing permanently.
- The script checks Developer Mode, unpacks the `.msix` into `%LOCALAPPDATA%\InternetSpeedTestExtension`, and
  registers that folder as an app for your Windows user. It installs no certificate and changes no other setting.
- Read the script before running it: it is about 50 lines, in `install/Install-SpeedTestExtension.ps1`.

## If something goes wrong

| Message | Meaning |
|---------|---------|
| `Developer Mode is off` | Turn it on (step 4), run again. |
| `Expected exactly one .msix file` | Run the command inside the extracted folder, with only one `.msix` in it. |
| `...cannot be loaded because running scripts is disabled` | You ran `.\Install-...ps1` directly. Use the full command above. |
| `0x80073D02` (package in use) | Command Palette is using the extension. Exit PowerToys (tray icon → Exit), run again. |
| `The .msix is too large` / `is not this extension` / `has no AppxManifest.xml` | Not a build of this extension, or a broken download. Download it again from a green run. Nothing was changed. |
| Extension not listed after install | Run **Reload** in Command Palette, or restart PowerToys. |

Anything else: copy the red text into a GitHub issue.
