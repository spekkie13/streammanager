using SpekkieTwitchBot.Twitch.General;

namespace SpekkieTwitchBot.Twitch;

public class PingSender
{
    private readonly IrcClient _Irc;
    private Thread? _PingThread;

    public PingSender(IrcClient ircClient)
    {
        _Irc = ircClient;
    }

    public void Start()
    {
        if(_PingThread != null)
        {
            _PingThread.IsBackground = true;
        }
    }

    public void SetPingThread(Thread pingThread)
    {
        _PingThread = pingThread;
    }

    public void Run()
    {
        while (true)
        {
            _Irc.SendIrcMessage("PING irc.twitch.tv");
            Thread.Sleep(5 * 60 * 1000);
        }
    }
}