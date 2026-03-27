const fs = require("fs");
const f = "BdStockOMS.Client/src/App.tsx";
let c = fs.readFileSync(f, "utf8");

// Add import
if (!c.includes("RMSManagementPage")) {
  c = c.replace(
    "import BOManagementPage",
    "import RMSManagementPage   from '@/pages/RMSManagementPage';\nimport BOManagementPage"
  );
}

// Replace placeholder route
c = c.replace(
  "import { RMSPage } from '@/pages/PlaceholderPages'",
  "// RMSPage replaced by RMSManagementPage"
);
c = c.replace(
  "<Route path=\"/rms\" element={<RMSPage />} />",
  "<Route path=\"/rms\" element={<RMSManagementPage />} />"
);

fs.writeFileSync(f, c);
console.log("App.tsx wired");
