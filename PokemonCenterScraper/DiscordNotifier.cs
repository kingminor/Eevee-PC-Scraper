using System.Text.Json;

public class DiscordNotifier
{
    private readonly HttpClient _httpClient = new();
    private readonly string _webhookUrl;

    public DiscordNotifier(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
    }

    public async Task SendCombinedChangesAsync(
        List<string> added,
        List<string> removed,
        Guid changeId,
        int maxItems = 10
    )
    {
        if (!added.Any() && !removed.Any()) return;

        var lines = new List<string>();

        // Top link to view all changes
        lines.Add($"[🔗 View all changes](http://localhost:5196/change/{changeId})\n");

        // Added products
        if (added.Any())
        {
            lines.Add("🟢 **New Products:**");
            var addedDisplay = added.Take(maxItems)
                .Select(p =>
                {
                    var lastSegment = new Uri(p).Segments.Last().Trim('/');
                    var title = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                    .ToTitleCase(lastSegment.Replace("-", " "));
                    return $"[{title}]({p})";
                });
            lines.AddRange(addedDisplay);

            var remainingAdded = added.Count - maxItems;
            if (remainingAdded > 0)
                lines.Add($"+{remainingAdded} more...");
            lines.Add(""); // blank line
        }

        // Removed products
        if (removed.Any())
        {
            lines.Add("🔴 **Removed Products:**");
            var removedDisplay = removed.Take(maxItems)
                .Select(p =>
                {
                    var lastSegment = new Uri(p).Segments.Last().Trim('/');
                    var title = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                    .ToTitleCase(lastSegment.Replace("-", " "));
                    return $"[{title}]({p})";
                });
            lines.AddRange(removedDisplay);

            var remainingRemoved = removed.Count - maxItems;
            if (remainingRemoved > 0)
                lines.Add($"+{remainingRemoved} more...");
        }

        var embed = new
        {
            embeds = new[]
            {
                new
                {
                    title = "📦 Product Changes",
                    description = string.Join("\n", lines),
                    color = 0x00FF00
                }
            }
        };

        var json = JsonSerializer.Serialize(embed);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_webhookUrl, content);
        response.EnsureSuccessStatusCode();
    }

}
