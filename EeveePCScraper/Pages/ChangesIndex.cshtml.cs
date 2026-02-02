using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;


    public class ChangesIndexModel : PageModel
    {
        private const string ChangesFile = "changes.json";
        public List<ChangeRecord> Changes { get; private set; } = new();

        public void OnGet()
        {
            if (!System.IO.File.Exists(ChangesFile)) return;

            var json = System.IO.File.ReadAllText(ChangesFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Changes = JsonSerializer.Deserialize<List<ChangeRecord>>(json, options) ?? new List<ChangeRecord>();
        }
    }