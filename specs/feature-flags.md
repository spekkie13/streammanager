# Feature flags — runtime schakelbaar via bestand

## Doel

Services aan- en uitzetten zonder de bot te herstarten, door een JSON-bestand te wijzigen.

---

## Bestand: `{BaseDir}/Settings/features.json`

```json
{
  "Marathon": true,
  "War": true
}
```

Sla het bestand op → de bot herlaadt de flags binnen ~500ms.

---

## Ontwerp: `FeatureFlagService`

Singleton service die:

- `features.json` inleest bij opstart
- Een `FileSystemWatcher` bijhoudt op dat bestand
- Bij wijziging herlaadt met debounce (500ms), conform het bestaande `goals.json` patroon in `FollowSubFeature`
- Een methode `IsEnabled(string flag)` exposeert

```csharp
featureFlags.IsEnabled("Marathon") // true of false
```

---

## Integratiepunten

| Flag | Service | Gedrag bij `false` |
|---|---|---|
| `Marathon` | `MarathonTimerFeature` | Events worden genegeerd; timer zelf blijft draaien |
| `War` | `WarService` | Fetch-cyclus slaat verwerking over; OBS-overlay ongewijzigd |

Meer flags kunnen later worden toegevoegd zonder wijzigingen aan `FeatureFlagService` zelf.

---

## Implementatiestappen

1. `FeatureFlagService` aanmaken met `FileSystemWatcher` + debounce
2. Registreren als singleton in `Program.cs`
3. Injecteren in `MarathonTimerFeature` → guard in `HandleSubAsync`, `HandleBitsAsync`, `HandleDonationAsync`
4. Injecteren in `WarService` → guard in `ProcessWar`
5. `features.json` aanmaken in `{BaseDir}/Settings/`
