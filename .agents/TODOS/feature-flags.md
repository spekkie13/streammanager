# Feature Flags — TODO

## Stap 1: `IFeatureFlagService` interface aanmaken

Maak `SpekkieTwitchBot.General.FileHandling/IFeatureFlagService.cs`:

```csharp
namespace SpekkieTwitchBot.General.FileHandling;

public interface IFeatureFlagService
{
    bool IsEnabled(string flag);
}
```

Verify: `dotnet build SpekkieTwitchBot.General.FileHandling`

---

## Stap 2: `FeatureFlagService` implementeren

Maak `SpekkieTwitchBot.General.FileHandling/FeatureFlagService.cs`.

Vereisten:
- Constructor: `(Logger logger)`
- Veld `_Flags`: `IReadOnlyDictionary<string, bool>` (initieel leeg)
- Methode `InitializeAsync(CancellationToken)`: leest `features.json`, start watcher
- `IsEnabled(string flag)`: thread-safe read van `_Flags`
- Watcher: `FileSystemWatcher` op `{BotPaths.BaseDir}/Settings`, filter `features.json`, `NotifyFilter = LastWrite`
- Debounce-patroon identiek aan `FollowSubFeature.OnGoalsConfigChanged` (CancellationTokenSource-wissel + 500ms delay)
- Bij reload-fout: huidige `_Flags` intact laten, fout loggen

Verify: `dotnet build SpekkieTwitchBot.General.FileHandling`

---

## Stap 3: `features.json` aanmaken

Maak `{BaseDir}/Settings/features.json`:

```json
{
  "Marathon": true,
  "War": true
}
```

Verify: bestand aanwezig op pad, geldige JSON (`cat features.json | python3 -m json.tool`)

---

## Stap 4: Registreren als singleton in `Program.cs`

In `ConfigureServices`, onder "Core":

```csharp
services.AddSingleton<FeatureFlagService>();
services.AddSingleton<IFeatureFlagService>(sp => sp.GetRequiredService<FeatureFlagService>());
```

`FeatureFlagService.InitializeAsync(...)` aanroepen in de startup-flow (of als `IHostedService` indien gewenst).

Verify: `dotnet build SpekkieTwitchBot`

---

## Stap 5: Guard in `MarathonTimerFeature`

Injecteer `IFeatureFlagService featureFlags` via primary constructor.

Voeg toe aan begin van `HandleSubAsync`, `HandleBitsAsync`, `HandleDonationAsync`:

```csharp
if (!featureFlags.IsEnabled("Marathon")) return;
```

Verify: `dotnet build SpekkieTwitchBot`

---

## Stap 6: Guard in `WarService`

Injecteer `IFeatureFlagService featureFlags` via primary constructor.

Voeg toe aan begin van `ProcessWar`:

```csharp
if (!featureFlags.IsEnabled("War")) return;
```

Verify: `dotnet build SpekkieTwitchBot`

---

## Stap 7: Unit tests schrijven

In `SpekkieTwitchBot.Tests`:

- `FeatureFlagServiceTests`: maak tijdelijk JSON-bestand, verifieer `IsEnabled` voor true/false/ontbrekend/ongeldig JSON.
- `MarathonTimerFeatureTests` (uitbreiden): mock `IFeatureFlagService` → `false`; verifieer dat `timer.AddTime` niet aangeroepen wordt.

Verify: `dotnet test SpekkieTwitchBot.Tests`

---

## Stap 8: Volledige build + tests

Verify: `dotnet build && dotnet test`
