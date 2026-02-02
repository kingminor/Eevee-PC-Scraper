# 

# EEVEE PC SCRAPER 1.0

EEVEE PC Scraper is a background service that automatically monitors Pokémon Center for product updates. It detects new products and removed products, saves this data to JSON files, and sends notifications via Discord.

---

## Features

- Scrapes Pokémon Center sitemap for EN-US product links.

- Detects additions and removals in the product catalog.

- Saves product snapshots (`products.json`) and change history (`changes.json`).

- Sends updates to a Discord webhook.

- Provides a simple web UI to view all changes and individual change details.

- Runs as a background service and updates hourly.

---

## Setup

1. Clone the repository:
   
   `git clone <repository-url>`
   
   and
   
   `cd eevee-pc-scraper`

2. Rename the configuration template:
   
   `appsettings.json.template → appsettings.json`

3. Open `appsettings.json` and replace the `Discord:WebhookUrl` value with your Discord webhook URL.

4. I made this using dotnet 10.0 so I recommend using that.

---

## Running the App

Run the application using:

`dotnet run`

- The scraper will start running in the background.

- Product snapshots and change history are saved in the application folder.

- Discord notifications are sent whenever there are changes.

---

## Web UI

- **Home:** `/` — overview and description.

- **All Changes:** `/changes` — view all recorded changes.

- **Single Change:** `/change/{id}` — view details of a specific change.

- The web UI formats product names nicely and provides clickable links.

---

## License

This project is released under the MIT License.
