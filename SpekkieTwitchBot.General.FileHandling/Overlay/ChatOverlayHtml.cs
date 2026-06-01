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

          .msg .user {
            font-weight: 700;
            margin-right: 6px;
          }

          .msg .text { color: #f1f3ff; }

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

          // Stable per-user color from a simple string hash.
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

          function render(state) {
            var chat = document.getElementById("chat");
            var msgs = (state && state.messages) || [];
            var html = "";
            for (var i = 0; i < msgs.length; i++) {
              var m = msgs[i];
              html += '<div class="msg">'
                + '<span class="user" style="color:' + colorFor(m.user || "") + '">'
                + escapeHtml(m.user) + '</span>'
                + '<span class="text">' + escapeHtml(m.text) + '</span>'
                + '</div>';
            }
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
