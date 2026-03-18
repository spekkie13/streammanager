using System.Net;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileWriter(ITextFileWriter fileWriter)
{
    private const string OutputDir = "/Output/Spotify";
    
    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public virtual void WriteSongFile(string text)
    {
        string title = text.Split(" by ")[0];
        string titleDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        fileWriter.Write(titleDir, title);

        string artist = text.Split(" by ")[1];
        string artistDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        fileWriter.Write(artistDir, artist);
    }

    public void WriteNowPlayingHtml(string title, string artist, int reloadDelayMs)
    {
        string safeTitle = WebUtility.HtmlEncode(title);
        string safeArtist = WebUtility.HtmlEncode(artist);

        string html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                  background: transparent;
                  overflow: hidden;
                  animation: fadeIn 0.4s ease-in;
                }
                @keyframes fadeIn {
                  from { opacity: 0; transform: translateY(6px); }
                  to   { opacity: 1; transform: translateY(0); }
                }
                .card {
                  display: flex;
                  align-items: center;
                  gap: 12px;
                  padding: 10px;
                  background: rgba(15, 15, 15, 0.88);
                  border: 1px solid rgba(255, 255, 255, 0.10);
                  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.55);
                  border-radius: 10px;
                  width: 336px;
                }
                .art {
                  width: 64px;
                  height: 64px;
                  border-radius: 6px;
                  object-fit: cover;
                  flex-shrink: 0;
                }
                .info { display: flex; flex-direction: column; gap: 4px; }
                .clip { width: 240px; overflow: hidden; }
                .text {
                  font-family: sans-serif;
                  white-space: nowrap;
                  display: inline-block;
                }
                .title { font-size: 16px; font-weight: bold; color: #ffffff; }
                .artist { font-size: 13px; color: #cccccc; }
                @keyframes marquee {
                  0%,  15% { transform: translateX(0); }
                  85%, 100% { transform: translateX(var(--scroll-dist)); }
                }
                .scrolling { animation: marquee 8s ease-in-out infinite; }
              </style>
            </head>
            <body>
              <div class="card">
                <img class="art" src="currentsong.png" onerror="this.style.display='none'">
                <div class="info">
                  <div class="clip"><span class="text title">{{safeTitle}}</span></div>
                  <div class="clip"><span class="text artist">{{safeArtist}}</span></div>
                </div>
              </div>
              <script>
                document.querySelectorAll('.text').forEach(span => {
                  const overflow = span.scrollWidth - span.parentElement.clientWidth;
                  if (overflow > 0) {
                    span.style.setProperty('--scroll-dist', `-${overflow}px`);
                    span.classList.add('scrolling');
                  }
                });
                setTimeout(() => location.reload(), {{reloadDelayMs}});
              </script>
            </body>
            </html>
            """;

        string path = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}nowplaying.html";
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.Write(html);
    }

    public void WriteCurrentSongImage(byte[] imgBytes)
    {
        string imgPath = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentsong.png";

        try
        {
            using FileStream fileStream = new(imgPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            fileStream.Write(imgBytes, 0, imgBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}