using TwitchLib.Client.Enums.Internal;
using TwitchLib.Client.Models.Internal;

namespace SpekkieTwitchBot.Twitch.General;

public class IrcParser
{
    public IrcMessage ParseIrcMessage(string raw)
    {
        Dictionary<string, string> tags = new Dictionary<string, string>();
        ParserState index1 = ParserState.STATE_NONE;
        int[] numArray1 = new int[6];
        int[] numArray2 = new int[6];
        for (int index2 = 0; index2 < raw.Length; ++index2)
        {
            numArray2[(int)index1] = index2 - numArray1[(int)index1] - 1;
            if (index1 == ParserState.STATE_NONE && raw[index2] == '@')
            {
                index1 = ParserState.STATE_V3;
                numArray1[(int)index1] = ++index2;
                int startIndex = index2;
                string key = "";
                for (; index2 < raw.Length; ++index2)
                {
                    if (raw[index2] == '=')
                    {
                        key = raw.Substring(startIndex, index2 - startIndex);
                        startIndex = index2 + 1;
                    }
                    else if (raw[index2] == ';')
                    {
                        if (key == null)
                            tags[raw.Substring(startIndex, index2 - startIndex)] = "1";
                        else
                            tags[key] = raw.Substring(startIndex, index2 - startIndex);
                        startIndex = index2 + 1;
                    }
                    else if (raw[index2] == ' ')
                    {
                        if (key == null)
                        {
                            tags[raw.Substring(startIndex, index2 - startIndex)] = "1";
                            break;
                        }

                        tags[key] = raw.Substring(startIndex, index2 - startIndex);
                        break;
                    }
                }
            }
            else if (index1 < ParserState.STATE_PREFIX && raw[index2] == ':')
            {
                index1 = ParserState.STATE_PREFIX;
                numArray1[(int)index1] = ++index2;
            }
            else if (index1 < ParserState.STATE_COMMAND)
            {
                index1 = ParserState.STATE_COMMAND;
                numArray1[(int)index1] = index2;
            }
            else
            {
                if (index1 < ParserState.STATE_TRAILING &&
                    raw[index2] == ':')
                {
                    index1 = ParserState.STATE_TRAILING;
                    int num;
                    numArray1[(int)index1] = num = index2 + 1;
                    break;
                }

                if (index1 < ParserState.STATE_TRAILING &&
                    raw[index2] == '+' ||
                    index1 < ParserState.STATE_TRAILING &&
                    raw[index2] == '-')
                {
                    index1 = ParserState.STATE_TRAILING;
                    numArray1[(int)index1] = index2;
                    break;
                }

                if (index1 == ParserState.STATE_COMMAND)
                {
                    index1 = ParserState.STATE_PARAM;
                    numArray1[(int)index1] = index2;
                }
            }

            while (index2 < raw.Length && raw[index2] != ' ')
                ++index2;
        }

        numArray2[(int)index1] = raw.Length - numArray1[(int)index1];
        string str1 = raw.Substring(numArray1[3], numArray2[3]);
        IrcCommand command = IrcCommand.Unknown;
        switch (str1)
        {
            case "001":
                command = IrcCommand.RPL_001;
                break;
            case "002":
                command = IrcCommand.RPL_002;
                break;
            case "003":
                command = IrcCommand.RPL_003;
                break;
            case "004":
                command = IrcCommand.RPL_004;
                break;
            case "353":
                command = IrcCommand.RPL_353;
                break;
            case "366":
                command = IrcCommand.RPL_366;
                break;
            case "372":
                command = IrcCommand.RPL_372;
                break;
            case "375":
                command = IrcCommand.RPL_375;
                break;
            case "376":
                command = IrcCommand.RPL_376;
                break;
            case "CAP":
                command = IrcCommand.Cap;
                break;
            case "CLEARCHAT":
                command = IrcCommand.ClearChat;
                break;
            case "CLEARMSG":
                command = IrcCommand.ClearMsg;
                break;
            case "GLOBALUSERSTATE":
                command = IrcCommand.GlobalUserState;
                break;
            case "JOIN":
                command = IrcCommand.Join;
                break;
            case "MODE":
                command = IrcCommand.Mode;
                break;
            case "NICK":
                command = IrcCommand.Nick;
                break;
            case "NOTICE":
                command = IrcCommand.Notice;
                break;
            case "PART":
                command = IrcCommand.Part;
                break;
            case "PASS":
                command = IrcCommand.Pass;
                break;
            case "PING":
                command = IrcCommand.Ping;
                break;
            case "PONG":
                command = IrcCommand.Pong;
                break;
            case "PRIVMSG":
                command = IrcCommand.PrivMsg;
                break;
            case "RECONNECT":
                command = IrcCommand.Reconnect;
                break;
            case "ROOMSTATE":
                command = IrcCommand.RoomState;
                break;
            case "SERVERCHANGE":
                command = IrcCommand.ServerChange;
                break;
            case "USERNOTICE":
                command = IrcCommand.UserNotice;
                break;
            case "USERSTATE":
                command = IrcCommand.UserState;
                break;
            case "WHISPER":
                command = IrcCommand.Whisper;
                break;
        }

        string str2 = raw.Substring(numArray1[4], numArray2[4]);
        string str3 = raw.Substring(numArray1[5], numArray2[5]);
        string hostmask = raw.Substring(numArray1[2], numArray2[2]);
        return new IrcMessage(command, new string[2]
        {
            str2,
            str3
        }, hostmask, tags);
    }

    private enum ParserState
    {
        STATE_NONE,
        STATE_V3,
        STATE_PREFIX,
        STATE_COMMAND,
        STATE_PARAM,
        STATE_TRAILING,
    }
}