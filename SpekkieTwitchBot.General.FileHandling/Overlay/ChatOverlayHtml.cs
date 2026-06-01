namespace SpekkieTwitchBot.General.FileHandling.Overlay;

/// <summary>
/// Self-contained OBS browser-source overlay that polls <c>chat-state.json</c> (written by
/// ChatOverlayService) and renders the most recent chat messages. Drop the generated
/// <c>Output/chat-overlay.html</c> into OBS as a browser source pointing at the local file.
/// </summary>
public static class ChatOverlayHtml
{
    public const string Content = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="UTF-8">
        <title>Chat Overlay</title>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }

          body {
            background: transparent;
            font-family: 'Segoe UI', Arial, sans-serif;
            color: #fff;
            width: 420px;
            height: 600px;
            overflow: hidden;
          }

          #chat {
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            display: flex;
            flex-direction: column;
            justify-content: flex-end;
            gap: 6px;
            padding: 12px;
            max-height: 600px;
            overflow: hidden;
          }

          .msg {
            background: rgba(7, 12, 38, 0.82);
            border-radius: 10px;
            padding: 7px 11px;
            font-size: 18px;
            line-height: 1.35;
            word-wrap: break-word;
            overflow-wrap: anywhere;
            text-shadow: 0 1px 2px rgba(0, 0, 0, 0.6);
            animation: fadein 0.25s ease-out;
          }

          .msg.highlighted { background: rgba(116, 75, 191, 0.85); }
          .msg.first { box-shadow: inset 3px 0 0 #00d2d3; }
          .msg.action .text { font-style: italic; }

          .reply {
            display: block;
            font-size: 13px;
            opacity: 0.7;
            margin-bottom: 2px;
          }

          .badge {
            display: inline-block;
            font-size: 11px;
            font-weight: 700;
            line-height: 1;
            padding: 2px 5px;
            margin-right: 4px;
            border-radius: 4px;
            vertical-align: middle;
            text-transform: uppercase;
            letter-spacing: 0.3px;
          }
          .badge.broadcaster { background: #e91916; }
          .badge.moderator   { background: #00ad03; }
          .badge.vip         { background: #e005b9; }
          .badge.subscriber  { background: #6441a5; }

          .msg .user { font-weight: 700; margin-right: 2px; }
          .msg .sep  { margin-right: 6px; opacity: 0.7; }
          .msg .text { color: #f1f3ff; }

          .emote { height: 1.5em; vertical-align: middle; margin: -2px 1px; }

          .bits {
            display: inline-block;
            margin-left: 6px;
            font-weight: 700;
            color: #ad79ff;
          }

          @keyframes fadein {
            from { opacity: 0; transform: translateY(6px); }
            to   { opacity: 1; transform: translateY(0); }
          }
        </style>
        </head>
        <body>
        <div id="chat"></div>
        <script>
          var COLORS = ["#ff6b6b","#feca57","#1dd1a1","#54a0ff","#5f27cd","#ff9ff3","#00d2d3","#ff9f43"];

          // Fallback color (hash of name) when Twitch sent no color tag.
          function colorFor(name) {
            var h = 0;
            for (var i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) | 0;
            return COLORS[Math.abs(h) % COLORS.length];
          }

          function escapeHtml(s) {
            var d = document.createElement("div");
            d.textContent = s == null ? "" : s;
            return d.innerHTML;
          }

          // Render text/emote segments (emotes as Twitch CDN images); fall back to plain text.
          function renderBody(m) {
            var segs = m.segments;
            if (!segs || !segs.length) return escapeHtml(m.text);
            var out = "";
            for (var i = 0; i < segs.length; i++) {
              var s = segs[i];
              if (s.type === "emote") {
                var url = "https://static-cdn.jtvnw.net/emoticons/v2/"
                  + encodeURIComponent(s.emoteId) + "/default/dark/2.0";
                out += '<img class="emote" src="' + url + '" alt="' + escapeHtml(s.text)
                  + '" title="' + escapeHtml(s.text) + '">';
              } else {
                out += escapeHtml(s.text);
              }
            }
            return out;
          }

          // Render the role badges we know how to style, in Twitch's usual order.
          function badgeChips(m) {
            var order = [
              ["broadcaster", m.isBroadcaster],
              ["moderator",   m.isMod],
              ["vip",         m.isVip],
              ["subscriber",  m.isSubscriber]
            ];
            var html = "";
            for (var i = 0; i < order.length; i++) {
              if (order[i][1]) {
                var label = order[i][0] === "subscriber" ? "sub" : order[i][0];
                html += '<span class="badge ' + order[i][0] + '">' + label + '</span>';
              }
            }
            return html;
          }

          function renderMsg(m) {
            var color = (m.color && m.color.length) ? m.color : colorFor(m.login || m.user || "");
            var cls = "msg";
            if (m.isHighlighted) cls += " highlighted";
            if (m.isFirstMessage) cls += " first";
            if (m.isAction) cls += " action";

            var html = '<div class="' + cls + '">';

            if (m.replyParent) {
              html += '<span class="reply">↳ @' + escapeHtml(m.replyParent.user)
                + ': ' + escapeHtml(m.replyParent.text) + '</span>';
            }

            html += badgeChips(m);
            html += '<span class="user" style="color:' + color + '">' + escapeHtml(m.user) + '</span>';

            // /me: no separator, whole line takes the user color.
            if (m.isAction) {
              html += '<span class="text" style="color:' + color + '"> ' + renderBody(m) + '</span>';
            } else {
              html += '<span class="sep">:</span><span class="text">' + renderBody(m) + '</span>';
            }

            if (m.bits && m.bits > 0) {
              html += '<span class="bits">✦ ' + m.bits + '</span>';
            }

            html += '</div>';
            return html;
          }

          function render(state) {
            var chat = document.getElementById("chat");
            var msgs = (state && state.messages) || [];
            var html = "";
            for (var i = 0; i < msgs.length; i++) html += renderMsg(msgs[i]);
            chat.innerHTML = html;
          }

          async function loadChat() {
            try {
              var res = await fetch("chat-state.json?_=" + Date.now());
              if (!res.ok) return;
              render(await res.json());
            } catch (_) {}
          }

          loadChat();
          setInterval(loadChat, 1000);
        </script>
        </body>
        </html>
        """;
}
