
import os

os.makedirs("BdStockOMS.Client/src/pages", exist_ok=True)
os.makedirs("BdStockOMS.Tests/Unit", exist_ok=True)
os.makedirs("docs", exist_ok=True)

# ── client.ts ─────────────────────────────────────────────────────────────────
cp  = "BdStockOMS.Client/src/api/client.ts"
src = open(cp, encoding="utf-8").read()
if "bosGetSessions" not in src:
    addon  = "\n// BOS XML Reconciliation\n"
    addon += "export const bosGetSessions       = (id: number) => fetch(\x27/api/bos/sessions/\x27+id,{headers:headers(),cache:\x27no-store\x27}).then(r=>handle(r))\n"
    addon += "export const bosUploadClients     = (dto: any)   => fetch(\x27/api/bos/upload/clients\x27,{method:\x27POST\x27,headers:headers(),body:JSON.stringify(dto)}).then(r=>handle(r))\n"
    addon += "export const bosUploadPositions   = (dto: any)   => fetch(\x27/api/bos/upload/positions\x27,{method:\x27POST\x27,headers:headers(),body:JSON.stringify(dto)}).then(r=>handle(r))\n"
    addon += "export const bosExportPositions   = (id: number) => fetch(\x27/api/bos/export/positions/\x27+id,{headers:headers(),cache:\x27no-store\x27}).then(r=>handle(r))\n"
    addon += "export const bosGetCompliance     = (id: number) => fetch(\x27/api/boscompliance/\x27+id,{headers:headers(),cache:\x27no-store\x27}).then(r=>handle(r))\n"
    addon += "export const bosRefreshCompliance = (id: number) => fetch(\x27/api/boscompliance/\x27+id+\x27/refresh\x27,{method:\x27POST\x27,headers:headers()}).then(r=>handle(r))\n"
    open(cp, "w", encoding="utf-8").write(src + addon)
    print("OK  client.ts")
else:
    print("SKIP client.ts")

# ── App.tsx route ─────────────────────────────────────────────────────────────
ap  = "BdStockOMS.Client/src/App.tsx"
src = open(ap, encoding="utf-8").read()
if "BosReconciliationPage" not in src:
    src = src.replace(
        "import AdminSettingsPage from \x27./pages/AdminSettingsPage\x27;",
        "import AdminSettingsPage from \x27./pages/AdminSettingsPage\x27;\nimport BosReconciliationPage from \x27@/pages/BosReconciliationPage\x27;"
    )
    src = src.replace(
        "<Route path=\\"/admin/fix\\"          element={<AdminPlaceholderPage title=\\"FIX Gateway\\" />} />",
        "<Route path=\\"/admin/fix\\"          element={<AdminPlaceholderPage title=\\"FIX Gateway\\" />} />\n          <Route path=\\"/admin/bos\\"          element={<BosReconciliationPage />} />"
    )
    open(ap, "w", encoding="utf-8").write(src)
    print("OK  App.tsx")
else:
    print("SKIP App.tsx")

print("Phase 1 done")
