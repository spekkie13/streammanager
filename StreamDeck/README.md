# StreamDeck — Live Attack Spotlight

These buttons select which war player the **Live Attack Spotlight** shows. Each button writes one
value to `Settings/spotlight-selection.txt`; the bot reads it every tick and publishes
`Output/ClashOfClans/active-player.json`. The StreamDeck never needs to know anything else — it just
writes a value.

## Button map (5v5)

| Button label | Value written | Spotlights |
|--------------|---------------|------------|
| Home 1 | `home:1` | your map position 1 |
| Home 2 | `home:2` | your map position 2 |
| Home 3 | `home:3` | your map position 3 |
| Home 4 | `home:4` | your map position 4 |
| Home 5 | `home:5` | your map position 5 |
| Away 1 | `away:1` | enemy map position 1 |
| Away 2 | `away:2` | enemy map position 2 |
| Away 3 | `away:3` | enemy map position 3 |
| Away 4 | `away:4` | enemy map position 4 |
| Away 5 | `away:5` | enemy map position 5 |
| Off | `off` | hides the panel (inactive) |

Use `Output/ClashOfClans/war-roster.json` to see which position is which player (names + town halls),
so you can label the buttons.

## Setup — easiest (no plugin, no arguments)

The `buttons/` folder has one `.bat` per button. For each StreamDeck key:

1. Add a **System → Open** action.
2. Set **App / File** to the matching script, e.g. `buttons\spotlight-home-3.bat`.

Pressing the key runs the script, which writes the value. That's it.

## Setup — one script with arguments (if your StreamDeck/plugin passes args)

Use `set-spotlight.bat` and pass the value as an argument, e.g. App/File `set-spotlight.bat`,
Arguments `home:3`. (Works with the BarRaider *Advanced Launcher* plugin, or a multi-action that calls
`cmd /c set-spotlight.bat home:3`.)

## Where the file goes

The scripts write to `<base>\Settings\spotlight-selection.txt`, where `<base>` is:

- `%BOT_BASE_DIR%` if that environment variable is set, otherwise
- `%USERPROFILE%\Desktop\SpekkieTwitchBot`

If your install uses a different base, set `BOT_BASE_DIR` for the StreamDeck process (or edit the
scripts).

## PowerShell equivalents (for testing without StreamDeck)

```powershell
$set = "$env:USERPROFILE\Desktop\SpekkieTwitchBot\Settings"
Set-Content "$set\spotlight-selection.txt" "home:3"   # spotlight your player 3
Set-Content "$set\spotlight-selection.txt" "away:2"   # spotlight enemy player 2
Set-Content "$set\spotlight-selection.txt" "off"      # hide
```
