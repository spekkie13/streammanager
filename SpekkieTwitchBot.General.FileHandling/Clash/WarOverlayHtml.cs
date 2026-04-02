namespace SpekkieTwitchBot.General.FileHandling.Clash;

internal static class WarOverlayHtml
{
    internal const string Content = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="UTF-8">
        <title>War Stats Overlay</title>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }

          body {
            background: transparent;
            font-family: 'Segoe UI', 'Arial Black', Arial, sans-serif;
            color: #fff;
            width: 1113px;
            height: 626px;
            overflow: hidden;
          }

          .card {
            width: 1113px;
            height: 626px;
            background: rgba(7, 12, 38, 0.93);
            border-radius: 16px;
            display: flex;
            flex-direction: column;
            overflow: hidden;
          }

          /* ─── HEADER SECTION ──────────────────────────────── */
          /* Slight gradient behind logos to lift them off the bg */
          .header-section {
            background: linear-gradient(180deg, rgba(255,255,255,0.04) 0%, transparent 100%);
            border-bottom: 1px solid rgba(255,255,255,0.08);
          }

          /* Logo + name stacked, one per side */
          .header {
            display: grid;
            grid-template-columns: 1fr auto 1fr;
            align-items: center;
            padding: 14px 48px 10px;
          }

          .team-block {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
          }
          .team-block.home { align-items: flex-end; }
          .team-block.away { align-items: flex-start; }

          .team-logo {
            width: 90px;
            height: 90px;
            object-fit: contain;
            border-radius: 50%;
          }
          .team-logo[src=""], .team-logo:not([src]) { opacity: 0; }

          .team-name {
            font-size: 20px;
            font-weight: 900;
            letter-spacing: 1px;
            text-transform: uppercase;
            color: #e2e8f0;
          }

          .vs-col {
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 0 32px;
            gap: 2px;
          }

          .vs-text {
            font-size: 26px;
            font-weight: 900;
            letter-spacing: 4px;
            color: #94a3b8;
          }

          /* ─── SCORE + PCT (inside header section) ─────────── */
          .score-pct {
            display: grid;
            grid-template-columns: 1fr auto 1fr;
            align-items: center;
            padding: 0 64px 12px;
          }

          .score-side {
            display: flex;
            flex-direction: column;
            gap: 0;
          }
          .score-side.home { align-items: flex-end; }
          .score-side.away { align-items: flex-start; }

          .score {
            font-size: 72px;
            font-weight: 900;
            line-height: 1;
          }

          .team-pct {
            font-size: 18px;
            font-weight: 600;
            color: #93c5fd;
            line-height: 1;
            margin-top: 2px;
          }

          .score-divider-col {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
            padding: 0 24px;
          }

          .star-icon {
            font-size: 28px;
            color: #fbbf24;
            line-height: 1;
          }

          .pct-label {
            font-size: 16px;
            font-weight: 700;
            color: #475569;
            line-height: 1;
          }

          /* ─── PLAYER ROWS ────────────────────────────────── */
          .players {
            display: flex;
            flex-direction: column;
            flex: 1;
            padding: 6px 12px 10px;
            gap: 3px;
          }

          .player-row {
            display: grid;
            grid-template-columns: 1fr 52px 1fr;
            align-items: center;
            flex: 1;
            border-radius: 8px;
            padding: 0 8px;
          }
          .player-row:nth-child(odd)  { background: rgba(255,255,255,0.04); }
          .player-row:nth-child(even) { background: rgba(255,255,255,0.015); }

          .side-home {
            display: flex;
            align-items: center;
            justify-content: flex-end;
            gap: 10px;
            overflow: hidden;
            padding-right: 6px;
          }

          .side-away {
            display: flex;
            align-items: center;
            justify-content: flex-start;
            gap: 10px;
            overflow: hidden;
            padding-left: 6px;
          }

          .player-name {
            font-size: 17px;
            font-weight: 700;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            color: #f1f5f9;
          }

          .attack-pct {
            font-size: 15px;
            font-weight: 800;
            white-space: nowrap;
            min-width: 48px;
          }
          .side-home .attack-pct { text-align: right; }
          .side-away .attack-pct { text-align: left; }

          /* Color by stars */
          .pct-3 { color: #4ade80; }
          .pct-2 { color: #a3e635; }
          .pct-1 { color: #fb923c; }
          .pct-0 { color: #f87171; }
          .pct-pending { color: #475569; }

          /* ─── Attack display ─────────────────────────────── */
          .stars {
            display: flex;
            gap: 2px;
            flex-shrink: 0;
          }
          .star {
            font-size: 19px;
            line-height: 1;
          }
          .star-filled { color: #fbbf24; }
          .star-empty  { color: #1e293b; }

          .pending-bar {
            width: 58px;
            height: 9px;
            background: rgba(245, 158, 11, 0.12);
            border: 1.5px solid rgba(245, 158, 11, 0.6);
            border-radius: 4px;
            flex-shrink: 0;
          }

          /* ─── Row number badge ───────────────────────────── */
          .row-num {
            width: 34px;
            height: 34px;
            border-radius: 50%;
            background: rgba(255,255,255,0.07);
            border: 1px solid rgba(255,255,255,0.1);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 15px;
            font-weight: 900;
            color: #64748b;
            margin: 0 auto;
            flex-shrink: 0;
          }
        </style>
        </head>
        <body>

        <div class="card">

          <!-- HEADER: logos, names, scores, pcts -->
          <div class="header-section">
            <div class="header">
              <div class="team-block home">
                <img class="team-logo" id="home-logo" src="" alt="">
                <span class="team-name" id="home-name"></span>
              </div>
              <div class="vs-col">
                <span class="vs-text">VS</span>
              </div>
              <div class="team-block away">
                <img class="team-logo" id="away-logo" src="" alt="">
                <span class="team-name" id="away-name"></span>
              </div>
            </div>

            <div class="score-pct">
              <div class="score-side home">
                <span class="score" id="home-score">0</span>
                <span class="team-pct" id="home-pct">0.00%</span>
              </div>
              <div class="score-divider-col">
                <span class="star-icon">&#9733;</span>
                <span class="pct-label">%</span>
              </div>
              <div class="score-side away">
                <span class="score" id="away-score">0</span>
                <span class="team-pct" id="away-pct">0.00%</span>
              </div>
            </div>
          </div>

          <!-- PLAYERS -->
          <div class="players" id="players-container"></div>

        </div>

        <script>
        function attackDisplay(stars) {
          if (stars === null || stars === undefined)
            return '<span class="pending-bar"></span>';
          let html = '';
          for (let i = 0; i < 3; i++)
            html += `<span class="star ${i < stars ? 'star-filled' : 'star-empty'}">&#9733;</span>`;
          return `<span class="stars">${html}</span>`;
        }

        function pctClass(stars) {
          if (stars === null || stars === undefined) return 'pct-pending';
          if (stars === 3) return 'pct-3';
          if (stars === 2) return 'pct-2';
          if (stars === 1) return 'pct-1';
          return 'pct-0';
        }

        function esc(str) {
          return String(str ?? "")
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        }

        function render(data) {
          const home = data.home;
          const away = data.away;

          document.getElementById("home-logo").src  = home.logo ?? "";
          document.getElementById("away-logo").src  = away.logo ?? "";
          document.getElementById("home-name").textContent = home.name ?? "";
          document.getElementById("away-name").textContent = away.name ?? "";
          document.getElementById("home-score").textContent = home.score ?? 0;
          document.getElementById("away-score").textContent = away.score ?? 0;
          document.getElementById("home-pct").textContent   = home.pct ?? "0.00%";
          document.getElementById("away-pct").textContent   = away.pct ?? "0.00%";

          const container = document.getElementById("players-container");
          container.innerHTML = "";

          const count = Math.max((home.players ?? []).length, (away.players ?? []).length);
          for (let i = 0; i < count; i++) {
            const hp = (home.players ?? [])[i] ?? { name: "", pct: null, stars: null };
            const ap = (away.players ?? [])[i] ?? { name: "", pct: null, stars: null };

            const hPctText = hp.pct ?? "—";
            const aPctText = ap.pct ?? "—";

            const row = document.createElement("div");
            row.className = "player-row";
            row.innerHTML = `
              <div class="side-home">
                <span class="player-name">${esc(hp.name)}</span>
                ${attackDisplay(hp.stars)}
                <span class="attack-pct ${pctClass(hp.stars)}">${esc(hPctText)}</span>
              </div>
              <div class="row-num">${i + 1}</div>
              <div class="side-away">
                <span class="attack-pct ${pctClass(ap.stars)}">${esc(aPctText)}</span>
                ${attackDisplay(ap.stars)}
                <span class="player-name">${esc(ap.name)}</span>
              </div>`;
            container.appendChild(row);
          }
        }

        async function loadData() {
          try {
            const res = await fetch("war-data.json?_=" + Date.now());
            if (!res.ok) return;
            render(await res.json());
          } catch (_) {}
        }

        loadData();
        setInterval(loadData, 5000);
        </script>

        </body>
        </html>
        """;
}