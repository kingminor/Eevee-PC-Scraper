using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

    public class ChangeModel : PageModel
    {
        private const string ChangesFile = "changes.json";

        public ChangeRecord? Change { get; private set; }

        [FromRoute]
        public string? Id { get; set; }

        public void OnGet()
        {
            if (!System.IO.File.Exists(ChangesFile)) return;

            var json = System.IO.File.ReadAllText(ChangesFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var history = JsonSerializer.Deserialize<List<ChangeRecord>>(json, options);
            if (history == null || !history.Any()) return;

            if (string.IsNullOrEmpty(Id))
            {
                // Return latest change
                Change = history.Last();
            }
            else if (Guid.TryParse(Id, out var guid))
            {
                Change = history.FirstOrDefault(c => c.Id == guid);
            }
        }
    }

    public class ChangeRecord
    {
        public Guid Id { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public List<string> Added { get; set; } = new();
        public List<string> Removed { get; set; } = new();
    }
