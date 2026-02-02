namespace ScraperWebUI;

using System.Text.Json;

public class ProductService
{
    private readonly string _filePath = "products.json";

    public List<string> GetProducts()
    {
        if (!File.Exists(_filePath)) return new List<string>();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }
}
