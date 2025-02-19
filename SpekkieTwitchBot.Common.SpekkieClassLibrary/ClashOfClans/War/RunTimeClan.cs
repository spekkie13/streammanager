#nullable disable

using SpekkieClassLibrary.ClashOfClans.Common;

namespace SpekkieClassLibrary.ClashOfClans.War;

public class RunTimeClan
{
    public double DestructionPercentage { get; set; }
    public string Tag { get; set; }
    public string Name { get; set; }
    public BadgeUrls BadgeUrls { get; set; }
    public int ClanLevel { get; set; }
    public int Attacks { get; set; }
    public int Stars { get; set; }
    public List<RunTimeMember> Members { get; set; }
}