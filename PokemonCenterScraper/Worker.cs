using System.Xml.Linq;
using System.Text.Json;
using System.Net.Http.Headers;

namespace ProductScraper;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly DiscordNotifier _discordNotifier;

    private const string XmlUrl = "https://www.pokemoncenter.com/sitemaps/products.xml";
    private const string SaveFile = "products.json";
    private const string ChangesFile = "changes.json";
    private readonly string DiscordWebhookUrl;
    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        DiscordWebhookUrl = configuration["Discord:WebhookUrl"];

        _discordNotifier = new DiscordNotifier(DiscordWebhookUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Product scraper worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("🔄 Starting scrape cycle at {time}", DateTimeOffset.Now);

            try
            {
                var currentProducts = await GetProductsAsyncWithRetries();

                var savedProducts = LoadSavedProducts();

                var added = currentProducts.Except(savedProducts).ToList();
                var removed = savedProducts.Except(currentProducts).ToList();

                _logger.LogInformation("📦 Added: {count}, Removed: {count}", added.Count, removed.Count);

                var changeId = SaveChanges(added, removed);

                if (changeId.HasValue)
                {
                    await _discordNotifier.SendCombinedChangesAsync(added, removed, changeId.Value);
                }



                // Save current product list
                SaveProducts(currentProducts);

                _logger.LogInformation("✅ Scrape cycle completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Worker cycle failed.");
            }

            _logger.LogInformation("⏳ Sleeping for 1 hour...\n");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("🛑 Product scraper worker stopping.");
    }


    // -----------------------------
    // Product Scraping
    // -----------------------------

    private async Task<List<string>> GetProductsAsyncWithRetries(int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await GetProductsAsync();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Attempt {attempt} failed: {message}. Retrying...", attempt, ex.Message);
                await Task.Delay(2000);
            }
        }

        // Last attempt
        return await GetProductsAsync();
    }

    private async Task<List<string>> GetProductsAsync()
    {
        _logger.LogInformation("📥 Downloading Pokemon Center sitemap...");

        using var response = await _httpClient.GetAsync(XmlUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var doc = XDocument.Load(stream);

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        var products = doc
            .Descendants(ns + "url")
            .Select(url =>
                url.Elements(xhtml + "link")
                   .FirstOrDefault(l =>
                       string.Equals((string?)l.Attribute("hreflang"), "en-us",
                                     StringComparison.OrdinalIgnoreCase))
                   ?.Attribute("href")?.Value
            )
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .Distinct()
            .ToList()!;

        _logger.LogInformation("✅ Extracted {count} EN-US product URLs.", products.Count);

        return products;
    }

    // -----------------------------
    // Save / Load Products
    // -----------------------------

    private List<string> LoadSavedProducts()
    {
        if (!File.Exists(SaveFile))
        {
            _logger.LogWarning("⚠️ No saved product file found. First run?");
            return new List<string>();
        }

        var json = File.ReadAllText(SaveFile);
        _logger.LogInformation("📖 Loaded product file from disk.");

        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private void SaveProducts(List<string> products)
    {
        if (File.Exists(SaveFile))
        {
            var existingJson = File.ReadAllText(SaveFile);
            var existingProducts = JsonSerializer.Deserialize<List<string>>(existingJson) ?? new List<string>();

            // Only save if there are changes
            if (products.SequenceEqual(existingProducts))
            {
                _logger.LogInformation("💾 No changes detected in product list. Skipping save.");
                return;
            }
        }

        var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SaveFile, json);
        _logger.LogInformation("💾 Product file updated. ({count} items)", products.Count);
    }

    private Guid? SaveChanges(List<string> added, List<string> removed)
    {
        if (!added.Any() && !removed.Any())
        {
            _logger.LogInformation("💾 No changes detected. Skipping change history update.");
            return null;
        }

        var changeId = Guid.NewGuid();

        var changeRecord = new
        {
            Id = changeId,
            Timestamp = DateTimeOffset.Now,
            Added = added,
            Removed = removed
        };

        List<object> history;

        if (File.Exists(ChangesFile))
        {
            var json = File.ReadAllText(ChangesFile);
            history = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
        }
        else
        {
            history = new List<object>();
        }

        history.Add(changeRecord);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ChangesFile, JsonSerializer.Serialize(history, options));

        _logger.LogInformation("💾 Change history updated. (+{added}, -{removed})", added.Count, removed.Count);

        return changeId;
    }
}