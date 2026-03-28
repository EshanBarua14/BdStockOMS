const fs = require("fs");
const f = "BdStockOMS.API/Program.cs";
let c = fs.readFileSync(f, "utf8");

if (!c.includes("IDseScraperService")) {
  c = c.replace(
    "builder.Services.AddScoped<IBrokerManagementService, BrokerManagementService>();",
    "builder.Services.AddScoped<IBrokerManagementService, BrokerManagementService>();\nbuilder.Services.AddScoped<IDseScraperService, DseScraperService>();\nbuilder.Services.AddHttpClient(\"DSE\", client => {\n    client.Timeout = TimeSpan.FromSeconds(15);\n    client.DefaultRequestHeaders.Add(\"User-Agent\", \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36\");\n    client.DefaultRequestHeaders.Add(\"Accept\", \"text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\");\n    client.DefaultRequestHeaders.Add(\"Accept-Language\", \"en-US,en;q=0.5\");\n});"
  );
  c = c.replace(
    "builder.Services.AddHostedService<StockPriceUpdateService>();",
    "// builder.Services.AddHostedService<StockPriceUpdateService>(); // replaced by real data\nbuilder.Services.AddHostedService<RealMarketDataService>();"
  );
  fs.writeFileSync(f, c);
  console.log("Program.cs updated");
} else {
  console.log("Already registered");
}
