# Stream dashboard — architectuurnotities

## Visie

Een login-beveiligd dashboard op itsspekkie.com waarmee ik tijdens (en buiten) streams instellingen kan aanpassen, de marathon timer kan bedienen, en veelgebruikte controls bij de hand heb.

---

## Architectuur

### Bot-side: minimale ASP.NET API
- Voeg een minimale HTTP API toe aan de bestaande .NET host (naast de bestaande hosted services)
- Endpoints zoals `GET /timer`, `POST /timer/add`, `POST /timer/set`, later uitbreidbaar
- Beveiligd met een secret API key in de request header (bijv. `X-Api-Key`)
- De bestaande `IEventTimerService` singleton is direct injecteerbaar

### Verbinding: Cloudflare Tunnel
- Gratis, geen port forwarding of statisch IP nodig
- SSL automatisch afgehandeld
- Persistent zolang de bot draait
- Bot registreert een tunnel bij opstart → vaste publieke URL

### Website-side: itsspekkie.com dashboard
- Dashboard roept de bot-API aan met de secret key
- Login via **Twitch OAuth** (logisch voor een streamer-dashboard, Twitch-infra is al aanwezig)
- Dashboard is alleen functioneel als de bot draait — voor stream-gebruik prima

---

## Afweging: offline state

Huidige aanpak werkt alleen als de bot online is. Als je het dashboard ook wil gebruiken als de bot uit staat (bijv. instellingen alvast klaarzetten):
- Sla gewenste state op in een cloud-database (bijv. Supabase of een simpele Postgres)
- Bot leest die state bij opstart in
- Meer werk, maar geeft volledige ontkoppeling

Voor nu: niet nodig.

---

## Volgorde van bouwen

1. Minimale ASP.NET API toevoegen aan bot (timer endpoints)
2. Cloudflare Tunnel opzetten en configureren
3. Twitch OAuth login op itsspekkie.com
4. Dashboard frontend (timer display + controls)
5. Uitbreiden met andere instellingen naar behoefte
