using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Commands;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TextCommandHandler : ITextCommandHandler
{
    private readonly string _filePath;
    private List<TextCommand> _commands;

    public TextCommandHandler(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(BotPaths.BaseDir, "Settings", "TextCommands.json");
        _commands = LoadTextCommands();
    }

    private List<TextCommand> LoadTextCommands()
    {
        string commandLocation = _filePath;
        using FileStream rfs = new(commandLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader sr = new(rfs);
        string json = sr.ReadToEnd();

        TextCommands comm = JsonConvert.DeserializeObject<TextCommands>(json) ?? new TextCommands();
        List<TextCommand> commands = comm.Commands;
        return commands;
    }
    
    public string HandleCommand(ChatCommandReceived command)
    {
        string reply = "Unknown Command";
        
        foreach (TextCommand comm in _commands.Where(comm => comm.Command == $"!{command.CommandText}"))
        {
            reply = comm.Response ?? "";
        }
        return reply;
    }
    
    public string AddCommand(string action, string command, string response)
    {
        TextCommand comm;
        string reply = "";
        switch (action.ToLowerInvariant())
        {
            case "add":
                comm = new TextCommand
                {
                    Command = $"{command}",
                    Response = response
                };
                reply = $"Command {command} is added.";
                _commands.Add(comm);
                break;
            case "update":
            case "edit":
                comm = _commands.FirstOrDefault(c => c.Command == command) ?? new TextCommand();
                _commands.Remove(comm);
                comm.Response = response;
                _commands.Add(comm);
                reply = $"Command {command} is updated.";
                break;
            case "delete":
            case "remove":
                comm = _commands.FirstOrDefault(c => c.Command == command) ?? new TextCommand();
                _commands.Remove(comm);
                reply = $"Command {command} is deleted.";
                break;
        }
        
        TextCommands commands = new ()
        {
            Commands = _commands
        };
        string json = JsonConvert.SerializeObject(commands);
        using (FileStream wfs = new(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        using (StreamWriter sw = new(wfs))
        {
            sw.Write(json);
        }
        _commands = LoadTextCommands();
        return reply;
    }
    
    public List<TextCommand> GetTextCommands()
    {
        return _commands;
    }
}