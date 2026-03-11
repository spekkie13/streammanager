#nullable disable
namespace SpekkieClassLibrary.ClashOfClans.War;

public class RunTimeAttack
{
    public int Order { get; set; }
    public string AttackerTag { get; set; }
    public string DefenderTag { get; set; }
    public int Stars { get; set; }
    public double DestructionPercentage { get; set; }
    public double Duration { get; set; }
}