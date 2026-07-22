using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

// Running, restart-persistent totals of how much has been cheered (bits) and donated (euros)
// across the channel. Unlike the sub goal these have no target/perk — they are pure counters
// surfaced on the overlay so viewers can see the cumulative support.
public record SupportTotals(
    [property: JsonPropertyName("bitsTotal")] int BitsTotal,
    [property: JsonPropertyName("donationTotal")] decimal DonationTotal
);
