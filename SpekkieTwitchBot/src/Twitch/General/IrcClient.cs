using System.Net.Sockets;
using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch.Auth;
using SpekkieTwitchBot.Twitch.FileHandling;

namespace SpekkieTwitchBot.Twitch.General;

public class IrcClient
{
    private string _Username;
    private string _Channel;
    private string _OAuth;

    private readonly StreamReader _InputStream;
    private readonly StreamWriter _OutputStream;
    private readonly TwitchFileReader _TwitchFileReader;
    private readonly Logger _Logger;
    
    public IrcClient(TwitchFileReader twitchFileReader, Logger logger)
    {
        _Username = string.Empty;
        _Channel = string.Empty;
        _OAuth = string.Empty;
        _TwitchFileReader = twitchFileReader;
        _Logger = logger;

        FillAuthorizationInfo();

        var tcpClient = new TcpClient("irc.twitch.tv", 6667);
        _InputStream = new StreamReader(tcpClient.GetStream());
        _OutputStream = new StreamWriter(tcpClient.GetStream());
        
        Setup();
    }

    private void Setup()
    {
        _OutputStream.WriteLine("PASS " + _OAuth);
        _OutputStream.WriteLine("NICK " + _Username);
        _OutputStream.WriteLine("USER " + _Username + " 8 * :" + _Username);
        _OutputStream.WriteLine("JOIN " + _Channel);
        _OutputStream.Flush();
    }

    private void FillAuthorizationInfo()
    {
        string jsonData = _TwitchFileReader.ReadTwitchGeneralAuthFile();
        GeneralTwitchAuth auth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData) ?? new GeneralTwitchAuth();
        _Username = auth.BotName;
        _Channel = $"#{auth.BroadcasterName}";
        _OAuth = auth.Implicit_OAuth;
    }
    
    private void SendIrcMessage(string message)
    {
        try
        {
            _OutputStream.WriteLine(message);
            _OutputStream.Flush();
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex.Message);
        }
    }

    public void SendPublicChatMessage(string message)
    {
        try
        {
            SendIrcMessage($"PRIVMSG {_Channel} :{message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public string? ReadMessage()
    {
        try
        {
            string? message = _InputStream.ReadLine();
            return message;
        }
        catch (Exception ex)
        {
            return $"Error receiving message: {ex.Message}";
        }
    }
}