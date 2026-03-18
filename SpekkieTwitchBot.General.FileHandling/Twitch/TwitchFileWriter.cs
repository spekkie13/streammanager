using System.Net;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileWriter(ITextFileWriter fileWriter) : ITwitchFileWriter
{
    private const string OutputDir = "/Output/Twitch";

    private string? _LatestFollower;
    private string? _LatestSub;

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public void WriteTwitchUserAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch-User.json";
        fileWriter.Write(dir, text);
    }

    public async Task WriteMostRecentFollowerAsync(string text, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentFollower.txt";
        await fileWriter.WriteAsync(dir, $"Most recent follower: {text}");
    }

    public async Task WriteTotalFollowersAsync(int totalFollowers, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalFollowers.txt";
        await fileWriter.WriteAsync(dir, totalFollowers.ToString());
    }

    public async Task WriteMostRecentSubscriberAsync(string text, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}RecentSubscriber.txt";
        await fileWriter.WriteAsync(dir, $"Most recent subscriber: {text}");
    }

    public async Task WriteTotalSubscribersAsync(int totalSubscribers, CancellationToken cancellationToken)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}TotalSubscribers.txt";
        await fileWriter.WriteAsync(dir, totalSubscribers.ToString());
    }

    public void WriteLatestFollowerHtml(string username)
    {
        _LatestFollower = username;
        WriteActivityHtml();
    }

    public void WriteLatestSubHtml(string subText)
    {
        _LatestSub = subText;
        WriteActivityHtml();
    }

    public void WriteSubGoalHtml(int current, int goal, int daysRemaining)
    {
        double pct = goal > 0 ? Math.Min(100.0, current * 100.0 / goal) : 0;
        string pctText = pct.ToString("F2");

        string html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { background: transparent; overflow: hidden; }
                .widget {
                  display: flex;
                  flex-direction: column;
                  gap: 6px;
                  width: 420px;
                }
                .bar-track {
                  width: 100%;
                  height: 36px;
                  background: rgba(0, 0, 0, 0.45);
                  border-radius: 6px;
                  overflow: hidden;
                  position: relative;
                }
                .bar-fill {
                  height: 100%;
                  width: {{pct.ToString("F4")}}%;
                  background: #3dba4e;
                  border-radius: 6px;
                  transition: width 0.6s ease;
                }
                .bar-label {
                  position: absolute;
                  inset: 0;
                  display: flex;
                  align-items: center;
                  justify-content: center;
                  font-family: sans-serif;
                  font-size: 15px;
                  font-weight: bold;
                  color: #ffffff;
                  text-shadow: 0 1px 3px rgba(0,0,0,0.7);
                }
                .meta {
                  display: flex;
                  justify-content: space-between;
                  align-items: center;
                  font-family: sans-serif;
                  font-size: 13px;
                  color: #dddddd;
                  padding: 0 2px;
                }
                .meta-center {
                  font-weight: bold;
                  text-transform: uppercase;
                  letter-spacing: 0.06em;
                  color: #ffffff;
                }
              </style>
            </head>
            <body>
              <div class="widget">
                <div class="bar-track">
                  <div class="bar-fill"></div>
                  <div class="bar-label">{{current}} ({{pctText}}%)</div>
                </div>
                <div class="meta">
                  <span>0</span>
                  <span class="meta-center">{{daysRemaining}} days to go</span>
                  <span>{{goal}}</span>
                </div>
              </div>
            </body>
            </html>
            """;

        string path = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}subgoal.html";
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.Write(html);
    }

    private void WriteActivityHtml()
    {
        string safeFollower = WebUtility.HtmlEncode(_LatestFollower ?? "—");
        string safeSub = WebUtility.HtmlEncode(_LatestSub ?? "—");

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
                }
                @keyframes fadeIn {
                  from { opacity: 0; transform: translateY(6px); }
                  to   { opacity: 1; transform: translateY(0); }
                }
                .animate { animation: fadeIn 0.5s ease-in; }
                .card {
                  display: flex;
                  flex-direction: row;
                  align-items: stretch;
                  gap: 0;
                  padding: 12px 16px;
                  background: rgba(15, 15, 15, 0.88);
                  border: 1px solid rgba(255, 255, 255, 0.10);
                  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.55);
                  border-radius: 10px;
                  width: 420px;
                }
                .divider {
                  width: 1px;
                  background: rgba(255, 255, 255, 0.10);
                  margin: 0 14px;
                  flex-shrink: 0;
                }
                .section { display: flex; flex-direction: column; gap: 4px; flex: 1; min-width: 0; }
                .label {
                  font-family: sans-serif;
                  font-size: 11px;
                  color: #aaaaaa;
                  text-transform: uppercase;
                  letter-spacing: 0.08em;
                }
                .clip { overflow: hidden; }
                .text {
                  font-family: sans-serif;
                  white-space: nowrap;
                  display: inline-block;
                }
                .name { font-size: 16px; font-weight: bold; color: #ffffff; }
                @keyframes marquee {
                  0%,  15% { transform: translateX(0); }
                  85%, 100% { transform: translateX(var(--scroll-dist)); }
                }
                .scrolling { animation: marquee 6s ease-in-out infinite; }
              </style>
            </head>
            <body>
              <div class="card">
                <div class="section">
                  <span class="label">Latest Follower</span>
                  <div class="clip"><span class="text name">{{safeFollower}}</span></div>
                </div>
                <div class="divider"></div>
                <div class="section">
                  <span class="label">Latest Subscriber</span>
                  <div class="clip"><span class="text name">{{safeSub}}</span></div>
                </div>
              </div>
              <script>
                const renderId = '{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}';
                const prevId = localStorage.getItem('activityRenderId');
                if (prevId !== renderId) {
                  document.body.classList.add('animate');
                  localStorage.setItem('activityRenderId', renderId);
                }
                document.querySelectorAll('.text').forEach(span => {
                  const overflow = span.scrollWidth - span.parentElement.clientWidth;
                  if (overflow > 0) {
                    span.style.setProperty('--scroll-dist', `-${overflow}px`);
                    span.classList.add('scrolling');
                  }
                });
                setTimeout(() => location.reload(), 10000);
              </script>
            </body>
            </html>
            """;

        string path = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}latestactivity.html";
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.Write(html);
    }
}