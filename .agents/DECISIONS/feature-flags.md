# Feature Flags — beslissingen

## Vergeleken opties

| Optie | Complexiteit | Testbaarheid | Persistentie | Tijd tot demo |
|---|---|---|---|---|
| **1. FileSystemWatcher singleton** (gekozen) | Laag | Interface mockbaar | Ja (JSON) | ~1 uur |
| 2. `IOptionsMonitor<T>` + `AddJsonFile(reloadOnChange)` | Medium | Zwaarder | Ja | ~45 min |
| 3. In-memory chatcommando toggle | Laag | Triviaal | Nee | ~30 min |

## Keuze: Optie 1

Zelfde patroon als `goals.json` in `FollowSubFeature` — `FileSystemWatcher` + 500ms debounce via `CancellationTokenSource`-wissel. Het team kent dit patroon al. `IOptionsMonitor<T>` voegt cognitive overhead toe zonder wezenlijk voordeel; in-memory toggle overleeft geen herstart.

## Interface-locatie

`IFeatureFlagService` + `FeatureFlagService` leven in `SpekkieTwitchBot.General.FileHandling`.  
Reden: zowel `SpekkieTwitchBot` als `SpekkieTwitchBot.ClashOfClans.StatsBot` refereren aan dat project; de interface moet door beide consumeerbaar zijn zonder circulaire dependency.

## Verworpen aannames

- Flags hoeven geen enum te zijn — string-keys zijn flexibeler en voorkomen recompile bij elke nieuwe flag.
- `FeatureFlagService` hoeft geen `IDisposable` te implementeren als hij als singleton leeft en de `CancellationToken` van de host used voor cleanup.
- Geen logging nodig bij reload behalve `LogInfo` — flags zijn niet security-sensitief.
