namespace CommandService.CommandHandlers;

public class TextCommandHandler
{
    /*
     Compose a json file storing all commands and their response => TextCommands.json
     Store any special variables (like follower count, sub count, etc.) in a separate json file => SpecialVariables.json
     Write logic to replace any special variables with their actual values if the command response contains their names
     
     Figure out a way to execute functions at runtime from a json response & replace action
     */
    
    private Dictionary<string, string> _Commands = new ();
    private Dictionary<string, string> _SpecialVariables = new ();

    public TextCommandHandler()
    {
        LoadSpecialVariables();
        LoadTextCommands();
    }

    private void LoadTextCommands()
    {
        _Commands = new Dictionary<string, string>();
        //read TextCommands.json
        //transcribe it into a Dictionary<string, string>
    }

    private void LoadSpecialVariables()
    {
        _SpecialVariables = new Dictionary<string, string>();
        //read SpecialVariables.json
        //transcribe it into a Dictionary<string, string>
    }
    
    public string HandleCommand(string key)
    {
        string response = _Commands.GetValueOrDefault(key, "Unknown command");
        bool containsSpecialVariables = false;
        foreach (string variable in _SpecialVariables.Keys)
        {
            if (response.Contains(variable))
                containsSpecialVariables = true;
        }

        if (containsSpecialVariables)
        {
            response = response.Replace(_SpecialVariables[key], "");
        }
        
        return response;
    }

    public void AddCommand(string command, string response)
    {
        _Commands.Add(command, response);
    }

    public void AddSpecialVariable(string variable, string response)
    {
        _SpecialVariables.Add(variable, response);
    }
    
    public static string HandleGetYouTubeCommand()
    {
        //Added in TextCommands.json
        return "Checkout my YouTube Channel: https://youtube.com/@spekkieclashes";
    }

    public static string HandleGetDiscordCommand()
    {
        //Added in TextCommands.json
        return 
            "Wanna connect off-stream? Join my discord server: https://discord.gg/8Ez2dZNxeV";
    }

    public static string HandleGetTwitterCommand()
    {
        //Added in TextCommands.json
        return "Checkout my Twitter: https://twitter.com/CSpekkie";
    }

    public static string HandleGetCocTagCommand()
    {
        //Added in TextCommands.json
        return "My in-game tag is #QYCCULVY";
    }

    public static string HandleLurkCommand(string username)
    {
        return $"Enjoy your lurk! {username}";
    }

    public static string HandleHelloCommand()
    {
        //Added in TextCommands.json
        return "Hello World";
    }
}