namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// Self-contained spotlight state published as active-player.json. Fully renderable on its own — the
/// frontend never has to join it against war-roster.json. When <see cref="Active"/> is false only
/// <see cref="UpdatedAt"/> is meaningful; the optional fields are omitted from the JSON.
/// </summary>
public class ActivePlayer
{
    public bool Active { get; init; }
    public string UpdatedAt { get; init; } = "";
    public string? WarId { get; init; }
    public string? Team { get; init; }
    public int? MapPosition { get; init; }
    public string? Tag { get; init; }
    public string? Name { get; init; }
    public int? TownHall { get; init; }
    public SpotlightAttack? Attack { get; init; }
    public SpotlightAttack? Defense { get; init; }
    public SpotlightCareer? Career { get; init; }

    public static ActivePlayer Inactive(string updatedAt) => new()
    {
        Active = false,
        UpdatedAt = updatedAt
    };
}
