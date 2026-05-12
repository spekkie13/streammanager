# Feature Flags Spec

## Doel

Services runtime aan/uitzetten door `{BaseDir}/Settings/features.json` te wijzigen — zonder herstart van de bot.

## Non-Goals

- Geen gebruikersrechten of rollen per flag.
- Geen remote API om flags te wisselen.
- Geen audit trail van wijzigingen.
- Geen flags per kanaal of per gebruiker.
- `FeatureFlagService` verwijdert geen onbekende flags; hij leest puur wat er staat.

## Interfaces

### `IFeatureFlagService`

```csharp
// In: SpekkieTwitchBot.General.FileHandling
public interface IFeatureFlagService
{
    bool IsEnabled(string flag);
}
```

### `features.json`

```json
{
  "Marathon": true,
  "War": true
}
```

Pad: `{BotPaths.BaseDir}/Settings/features.json`

Ontbrekende sleutels → `IsEnabled` retourneert `false` (safe default).  
Ongeldig JSON → huidige waarden blijven intact; fout wordt gelogd.

## Key Decisions

- Implementatie: `FileSystemWatcher` + 500ms debounce (CancellationTokenSource-wissel), identiek aan `goals.json`-patroon in `FollowSubFeature`.
- Singleton geregistreerd in `Program.cs`.
- Interface in `SpekkieTwitchBot.General.FileHandling` zodat zowel het hoofdproject als `ClashOfClans.StatsBot` het kunnen consumeren.
- Interne state: `IReadOnlyDictionary<string, bool>` vervangen via `Interlocked.Exchange` (lock-free reads).

## Edge Cases en Failure Modes

| Scenario | Gedrag |
|---|---|
| `features.json` bestaat niet bij opstart | Alle flags `false`; watcher start zodra bestand aangemaakt wordt (of direct als map bestaat) |
| Ongeldig JSON na reload | Huidige dict blijft; `LogError` |
| Bestand locked (Windows dubbele write-event) | Debounce absorbeert; retry bij volgende event |
| Vlag ontbreekt in JSON | `IsEnabled` → `false` |
| Meerdere snelle saves | Debounce 500ms — alleen laatste reload telt |
| Bot shutdown tijdens debounce | `CancellationToken` annuleert `Task.Delay` — geen exception propagatie |

## Acceptance Criteria

- [ ] `IsEnabled("Marathon")` retourneert `true` als `features.json` `"Marathon": true` bevat.
- [ ] `IsEnabled("Marathon")` retourneert `false` als `features.json` `"Marathon": false` bevat.
- [ ] `IsEnabled("NonExistent")` retourneert `false`.
- [ ] Na opslaan van `features.json` reflecteert `IsEnabled` de nieuwe waarde binnen 600ms (500ms debounce + leesmarge).
- [ ] Bij ongeldig JSON na reload blijft de vorige state behouden en wordt een fout gelogd.
- [ ] `MarathonTimerFeature.HandleSubAsync/HandleBitsAsync/HandleDonationAsync` doet niets als `Marathon`-flag `false` is.
- [ ] `WarService.ProcessWar` slaat verwerking over als `War`-flag `false` is.
- [ ] Alle bestaande unit-tests blijven groen.

## Test Plan

- **Unit — `FeatureFlagServiceTests`**: inject een tijdelijk `features.json`; verifieer `IsEnabled`-uitkomsten voor true/false/missing/invalid-JSON.
- **Unit — `MarathonTimerFeatureTests`**: mock `IFeatureFlagService` terug `false`; verifieer dat `timer.AddTime` nooit aangeroepen wordt.
- **Unit — `WarServiceTests`**: mock `IFeatureFlagService` terug `false`; verifieer dat downstream write-calls uitblijven.
- Geen integratie- of end-to-end tests vereist voor deze feature.
