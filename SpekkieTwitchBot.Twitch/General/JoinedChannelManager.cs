using System.Collections.Concurrent;
using TwitchLib.Client.Models;

namespace TwitchAuthService.General;

public class JoinedChannelManager
{
    private readonly ConcurrentDictionary<string, JoinedChannel> _JoinedChannels;

    public JoinedChannelManager()
    {
        _JoinedChannels = new ConcurrentDictionary<string, JoinedChannel>(StringComparer.OrdinalIgnoreCase);
    }

    public void AddJoinedChannel(JoinedChannel channel)
    {
        _JoinedChannels.TryAdd(channel.Channel, channel);
    }

    public JoinedChannel? GetJoinedChannel(string channel)
    {
        _JoinedChannels.TryGetValue(channel, out JoinedChannel? joinedChannel);
        return joinedChannel;
    }

    public List<JoinedChannel> GetJoinedChannels()
    {
        return _JoinedChannels.Values.ToList();
    }

    public void RemoveJoinedChannel(string channel)
    {
        _JoinedChannels.TryRemove(channel, out JoinedChannel _);
    }

    public void Clear()
    {
        _JoinedChannels.Clear();
    }
}