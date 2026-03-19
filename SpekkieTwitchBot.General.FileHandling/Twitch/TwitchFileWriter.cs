using System.Globalization;
using System.Net;
using System.Text.Json;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Twitch;

public class TwitchFileWriter(ITextFileWriter fileWriter) : ITwitchFileWriter
{
    private const string OutputDir = "/Output/Twitch";

    private string? _LatestFollower;
    private string? _LatestSub;
    private int _TotalFollowers;
    private int _FollowerGoal;
    private int _SubGoalCurrent;
    private int _SubGoalGoal;

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

    public void WriteLatestFollowerHtml(string username, int totalFollowers)
    {
        _LatestFollower = username;
        _TotalFollowers = totalFollowers;
        WriteActivityHtml();
    }

    public void WriteLatestSubHtml(string subText, int totalSubs)
    {
        _LatestSub = subText;
        WriteActivityHtml();

        string displayPath = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}LatestSubDisplay.txt";
        using FileStream fs = new(displayPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter sw = new(fs);
        sw.Write(subText);
    }

    public void WriteSubGoalHtml(StreamGoalsConfig config)
    {
        _FollowerGoal = config.FollowerGoal;
        _SubGoalCurrent = config.SubGoal.CurrentAmount;
        _SubGoalGoal = config.SubGoal.Goal;
        WriteActivityHtml();

        string subHtml = $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { background: transparent; overflow: hidden; }
                .card {
                  display: inline-flex;
                  align-items: center;
                  gap: 12px;
                  padding: 16px 28px;
                  background: rgba(15, 15, 15, 0.88);
                  border: 1px solid rgba(255, 255, 255, 0.10);
                  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.55);
                  border-radius: 12px;
                }
                .label {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 13px;
                  color: #aaaaaa;
                  text-transform: uppercase;
                  letter-spacing: 0.08em;
                }
                .count {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 28px;
                  font-weight: bold;
                  color: #ffffff;
                }
                .slash {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 28px;
                  color: #ffcf00;
                }
              </style>
            </head>
            <body>
              <div class="card">
                <span class="label">Total Subs</span>
                <span class="count">{{config.SubGoal.CurrentAmount}}</span>
                <span class="slash">/</span>
                <span class="count">{{config.SubGoal.Goal}}</span>
              </div>
            </body>
            </html>
            """;

        string path = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}subgoal.html";
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.Write(subHtml);
    }

    public void WriteGoalsConfig(StreamGoalsConfig config)
    {
        string path = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}goals.json";
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.Write(json);
    }

    private void WriteActivityHtml()
    {
        string safeFollower = WebUtility.HtmlEncode(_LatestFollower ?? "—");
        string safeSub = WebUtility.HtmlEncode(_LatestSub ?? "—");
        string totalFollowers = _TotalFollowers.ToString("N0", CultureInfo.InvariantCulture);

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
                  padding: 16px 28px;
                  background: rgba(15, 15, 15, 0.88);
                  border: 1px solid rgba(255, 255, 255, 0.10);
                  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.55);
                  border-radius: 12px;
                  width: 1000px;
                }
                .divider {
                  width: 1px;
                  background: rgba(255, 255, 255, 0.10);
                  margin: 0 16px;
                  flex-shrink: 0;
                }
                .section { display: flex; flex-direction: column; gap: 6px; flex: 1; min-width: 0; }
                .label {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 13px;
                  color: #aaaaaa;
                  text-transform: uppercase;
                  letter-spacing: 0.08em;
                }
                .clip { overflow: hidden; }
                .text {
                  font-family: 'Supercell Magic', sans-serif;
                  white-space: nowrap;
                  display: inline-block;
                }
                .name { font-size: 20px; font-weight: bold; color: #ffffff; }
                .goal {
                  display: flex;
                  align-items: baseline;
                  gap: 4px;
                }
                .goal-num {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 22px;
                  font-weight: bold;
                  color: #ffffff;
                }
                .goal-slash {
                  font-family: 'Supercell Magic', sans-serif;
                  font-size: 22px;
                  color: #ffcf00;
                }
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
                  <span class="label">Follower Goal</span>
                  <div class="goal">
                    <span class="goal-num">{{totalFollowers}}</span>
                    <span class="goal-slash">/</span>
                    <span class="goal-num">{{_FollowerGoal}}</span>
                  </div>
                </div>
                <div class="divider"></div>
                <div class="section">
                  <span class="label">Latest Sub</span>
                  <div class="clip"><span class="text name">{{safeSub}}</span></div>
                </div>
                <div class="divider"></div>
                <div class="section">
                  <span class="label">Sub Goal</span>
                  <div class="goal">
                    <span class="goal-num">{{_SubGoalCurrent}}</span>
                    <span class="goal-slash">/</span>
                    <span class="goal-num">{{_SubGoalGoal}}</span>
                  </div>
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