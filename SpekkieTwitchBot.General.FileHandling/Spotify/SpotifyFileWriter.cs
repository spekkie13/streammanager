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

    public void WriteNowPlayingHtml(string title, string artist)
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
                  background: rgba(0, 0, 0, 0.6);
                  border-radius: 10px;
                  width: fit-content;
                }
                .art {
                  width: 64px;
                  height: 64px;
                  border-radius: 6px;
                  object-fit: cover;
                  flex-shrink: 0;
                }
                .title {
                  font-family: sans-serif;
                  font-size: 16px;
                  font-weight: bold;
                  color: #ffffff;
                  white-space: nowrap;
                  max-width: 240px;
                  overflow: hidden;
                  text-overflow: ellipsis;
                }
                .artist {
                  font-family: sans-serif;
                  font-size: 13px;
                  color: #cccccc;
                  white-space: nowrap;
                  max-width: 240px;
                  overflow: hidden;
                  text-overflow: ellipsis;
                }
              </style>
            </head>
            <body>
              <div class="card">
                <img class="art" src="currentsong.png" onerror="this.style.display='none'">
                <div class="info">
                  <div class="title">{{safeTitle}}</div>
                  <div class="artist">{{safeArtist}}</div>
                </div>
              </div>
              <script>setTimeout(() => location.reload(), 5000);</script>
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