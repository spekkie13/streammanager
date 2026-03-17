#nullable disable
namespace SpekkieClassLibrary.ClashOfClans.War;

public class RunTimeMember
{
    public string Tag { get; set; }
    public string Name { get; set; }
    public int MapPosition { get; set; }
    public int TownhallLevel { get; set; }
    public int OpponentAttacks { get; set; }
    public RunTimeAttack BestOpponentAttack { get; set; }
    public List<RunTimeAttack> Attacks { get; set; }
}