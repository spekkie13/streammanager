# Marathon timer — ontwerpbeslissingen

## Context

Deel 2 van het 7-jaar jubileum is een marathon stream, startend op 28 juni (maximaal live tot 12 juli). De stream begint met 7 uur op de timer. Kijkers kunnen tijd toevoegen aan de timer via monetisatie support. Deel 1 (18 mei) bevat al een segment waarbij kijkers tijd kunnen opbouwen voor deel 2.

---

## Tijdstabel (herzien)

| Actie | Tijd toegevoegd |
|---|---|
| 1 Sub (Tier 1) | +7 min |
| 1 Sub (Tier 2) | +14 min |
| 1 Sub (Tier 3) | +28 min |
| 5 Gift Subs | +42 min |
| 10 Gift Subs | +84 min |
| 20 Gift Subs | +175 min |
| 50 Gift Subs | +420 min |
| 100 Bits | +77 sec |
| 500 Bits | +7 min |
| 1.000 Bits | +13 min |
| 2.500 Bits | +49 min |
| 5.000 Bits | +77 min |
| 10.000 Bits | +140 min |
| €5 Donatie | +7 min |
| €10 Donatie | +14 min |
| €20 Donatie | +28 min |
| €50 Donatie | +77 min |
| €100 Donatie | +140 min |

> De tabel beschrijft de basistijden voor exacte drempelwaarden. Voor afwijkende bedragen geldt het B+ algoritme hieronder.

---

## Tier-factoren voor subs

Gebaseerd op werkelijke prijsverhoudingen, afgerond voor inzichtelijkheid:

| Tier | Factor | Werkelijke prijsverhouding |
|---|---|---|
| Tier 1 | 1× | basis (€4,99/maand) |
| Tier 2 | 2× | werkelijk 1,601× (€7,99/maand) — bewust licht voordelig |
| Tier 3 | 4× | werkelijk 4,007× (€19,99/maand) — vrijwel exact |

Tier 2 krijgt een lichte bonus boven de exacte verhouding als erkenning voor de keuze voor een hogere tier. Tier 3 ligt vrijwel exact op de werkelijke prijsverhouding.

---

## B+ algoritme voor afwijkende bedragen

Voor bedragen die niet exact op een drempelwaarde vallen wordt het volgende algoritme toegepast, zodat er nooit tijd verloren gaat.

### Werking

1. Sorteer de drempelwaarden van hoog naar laag
2. Loop door de drempels: bereken voor elke drempel hoeveel keer die er **heel** in past (`floor`), voeg die tijd toe, trek het verbruikte bedrag af van het resterende bedrag
3. Bij de **laagste drempel**: gebruik een fractionele berekening (`resterend / laagste_drempel`) zodat ook het laatste stukje wordt benut

### Voorbeeld: 750 bits

| Stap | Drempel | Berekening | Toegevoegd |
|---|---|---|---|
| 1 | 10.000 bits | 0× | — |
| 2 | 5.000 bits | 0× | — |
| 3 | 2.500 bits | 0× | — |
| 4 | 1.000 bits | 0× | — |
| 5 | 500 bits | 1× | +7 min, rest = 250 |
| 6 | 100 bits (laagste) | 250/100 = 2,5× | +192,5 sec |
| **Totaal** | | | **~10,2 min** |

### Voorbeeld: 15 gifted subs (Tier 1)

| Stap | Drempel | Berekening | Toegevoegd |
|---|---|---|---|
| 1 | 50 gifted | 0× | — |
| 2 | 20 gifted | 0× | — |
| 3 | 10 gifted | 1× | +84 min, rest = 5 |
| 4 | 5 gifted | 1× | +42 min, rest = 0 |
| **Totaal** | | | **126 min** |

### Fractioneel vs heel

- **Bits en donaties**: fractionele berekening op de laagste drempel (omdat bedragen vrij gekozen worden)
- **Gifted subs**: nooit fractioneel nodig, want de laagste drempel is 1 sub en aantallen zijn altijd heel

---

## Tier-factoren toepassen op gifted subs

Voordat het B+ algoritme wordt uitgevoerd op gifted subs, worden de aantallen omgerekend naar Tier 1 equivalenten:

```
equivalenten = aantal_gifted × tier_factor
```

Daarna wordt B+ uitgevoerd op het equivalent aantal.

### Voorbeeld: 5× Tier 2 gifted

- 5 × factor 2 = 10 Tier 1 equivalenten
- B+ op 10: 1× de 10-drempel = **+84 min**

### Voorbeeld: 3× Tier 2 gifted

- 3 × factor 2 = 6 Tier 1 equivalenten
- B+ op 6: 1× de 5-drempel + 1× de 1-drempel = 42 + 7 = **+49 min**

---

## Databronnen

| Type | Bron | Status |
|---|---|---|
| Subs (Tier 1/2/3) | Twitch EventSub (`channel.subscribe`, `channel.subscription.message`) | Gedeeltelijk aanwezig |
| Gifted subs | Twitch EventSub (`channel.subscription.gift`) | Gedeeltelijk aanwezig |
| Bits | Twitch EventSub (`channel.cheer`) | Ontbreekt — nog toe te voegen |
| Donaties | StreamElements WebSocket API | Ontbreekt — nog te bouwen |

---

## Nog te bouwen

- `channel.cheer` subscription toevoegen aan `TwitchEventSubClient`
- Timer-logica aansluiten op bestaande sub/bits events (B+ algoritme implementeren)
- StreamElements WebSocket client bouwen voor donation events
- Timer persisteren tussen sessies (timer.txt doet dit al)
- Timerwaarde opbouwen tijdens deel 1 (18 mei) en bewaren voor deel 2 (28 juni)