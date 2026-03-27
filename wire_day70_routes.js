const fs = require("fs");
const f = "BdStockOMS.Client/src/App.tsx";
let c = fs.readFileSync(f, "utf8");

// Add imports
if (!c.includes("AccountsPage")) {
  c = c.replace(
    "import RMSManagementPage",
    "import AccountsPage  from '@/pages/AccountsPage';\nimport IPOPage       from '@/pages/IPOPage';\nimport TBondPage     from '@/pages/TBondPage';\nimport RMSManagementPage"
  );
}

// Replace placeholder routes
const replacements = [
  ["AccountsModule","AccountsPage"],
  ["accounts","AccountsPage"],
  ["ipo","IPOPage"],
  ["tbond","TBondPage"],
  ["deposit","AccountsPage"],
  ["withdrawal","AccountsPage"],
];

// Add routes if not present
if (!c.includes("/accounts")) {
  c = c.replace(
    '<Route path="/rms" element={<RMSManagementPage />} />',
    '<Route path="/rms" element={<RMSManagementPage />} />\n          <Route path="/accounts" element={<AccountsPage />} />\n          <Route path="/ipo" element={<IPOPage />} />\n          <Route path="/tbond" element={<TBondPage />} />'
  );
}

fs.writeFileSync(f, c);
console.log("Routes wired");
