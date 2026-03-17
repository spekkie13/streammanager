using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TextCommandHandler : ITextCommandHandler
{
    private List<TextCommand> _Commands;

    public TextCommandHandler()
    {
        _Commands = LoadTextCommands();
    }

    private static List<TextCommand> LoadTextCommands()
    {
        string commandLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/TextCommands.json";
        string json = File.ReadAllText(commandLocation);

        TextCommands comm = JsonConvert.DeserializeObject<TextCommands>(json) ?? new TextCommands();
        List<TextCommand> commands = comm.Commands;
        return commands;
    }
    
    public string HandleCommand(ChatCommandReceived command)
    {
        string reply = "Unknown Command";
        
        foreach (TextCommand comm in _Commands.Where(comm => comm.Command == $"!{command.CommandText}"))
        {
            reply = comm.Response ?? "";
        }
        return reply;
    }
    
    public string AddCommand(string action, string command, string response)
    {
        TextCommand comm;
        string reply = "";
        switch (action)
        {
            case "add":
                comm = new TextCommand
                {
                    Command = $"{command}",
                    Response = response
                };
                reply = $"Command {command} is added.";
                _Commands.Add(comm);
                break;
            case "update":
                comm = _Commands.FirstOrDefault(c => c.Command == command) ?? new TextCommand();
                _Commands.Remove(comm);
                comm.Response = response;
                _Commands.Add(comm);
                reply = $"Command {command} is updated.";
                break;
            case "delete":
                comm = _Commands.FirstOrDefault(c => c.Command == command) ?? new TextCommand();
                _Commands.Remove(comm);
                reply = $"Command {command} is deleted.";
                break;
        }
        
        TextCommands commands = new ()
        {
            Commands = _Commands
        };
        string json = JsonConvert.SerializeObject(commands);
        string commandLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/TextCommands.json";
        File.WriteAllText(commandLocation, json);
        _Commands = LoadTextCommands();
        return reply;
    }
    
    public List<TextCommand> GetTextCommands()
    {
        return _Commands;
    }
}