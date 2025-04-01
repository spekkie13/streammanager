using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Commands;
using TwitchLib.Client.Models;

namespace CommandService.CommandHandlers;

public class TextCommandHandler
{
    private List<TextCommand> _Commands = LoadTextCommands();
    private readonly List<SpecialVariable> _SpecialVariables = LoadSpecialVariables();

    private static List<TextCommand> LoadTextCommands()
    {
        string commandLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/TextCommands.json";
        string json = File.ReadAllText(commandLocation);

        TextCommands comm = JsonConvert.DeserializeObject<TextCommands>(json) ?? new TextCommands();
        List<TextCommand> commands = comm.Commands;
        return commands;
    }

    private static List<SpecialVariable> LoadSpecialVariables()
    {
        string variableLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/SpecialVariables.json";
        string json = File.ReadAllText(variableLocation);
        
        SpecialVariables vars = JsonConvert.DeserializeObject<SpecialVariables>(json) ?? new SpecialVariables();
        List<SpecialVariable> specialVariables = vars.Variables;
        return specialVariables;
    }
    
    public string HandleCommand(ChatCommand command)
    {
        string reply = "Unknown Command";
        
        foreach (TextCommand comm in _Commands.Where(comm => comm.Command == $"!{command.CommandText}"))
        {
            reply = comm.Response ?? "";
        }
        reply = ReplaceSpecialVariables(reply, command);
        return reply;
    }

    private string ReplaceSpecialVariables(string message, dynamic e)
    {
        string response = message; // Start with the original message

        foreach (SpecialVariable specialVar in _SpecialVariables)
        {
            if (response.Contains(specialVar.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                string variableValue = specialVar.Value;

                // Check if the special variable is referring to an actual expression like "{e.Command.ChatMessage.DisplayName}"
                if (variableValue.Contains("e.Command.ChatMessage.DisplayName"))
                {
                    // Replace it with the actual value dynamically (assuming e is the context object)
                    variableValue = e.ChatMessage.DisplayName;
                }

                response = response.Replace(specialVar.Name, variableValue, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        return response;
    }

    public void AddCommand(string command, string response)
    {
        TextCommand comm = new TextCommand
        {
            Command = $"{command}",
            Response = response
        };
        
        _Commands.Add(comm);
        TextCommands commands = new TextCommands
        {
            Commands = _Commands
        };
        string json = JsonConvert.SerializeObject(commands);
        string commandLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/TextCommands.json";
        File.WriteAllText(commandLocation, json);
        _Commands = LoadTextCommands();
    }

    public void AddSpecialVariable(string variable, string response)
    {
        SpecialVariable var = new SpecialVariable
        {
            Name = variable,
            Value = response
        };
        _SpecialVariables.Add(var);
        string json = JsonConvert.SerializeObject(_SpecialVariables);
        string variableLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/SpekkieTwitchBot/Settings/TextCommands.json";
        File.WriteAllText(variableLocation, json);
    }

    public List<TextCommand> GetTextCommands()
    {
        return _Commands;
    }
}