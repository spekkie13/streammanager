#nullable disable
namespace SpekkieClassLibrary.ClashOfClans.War;

public class RunTimeWar
{
    public RunTimeClan Clan { get; set; }
    public RunTimeClan Opponent { get; set; }
    public int TeamSize { get; set; }
    public int AttacksPerMember { get; set; }
    public string BattleModifier { get; set; }
    public string StartTime { get; set; }
    public string State { get; set; }
    public string EndTime { get; set; }
    public string PreparationStartTime { get; set; }
}