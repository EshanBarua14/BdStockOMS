# Day 74 - BOS XML Reconciliation Dashboard

## Route
/admin/bos -> BosReconciliationPage

## Tabs
- Upload: Clients/Positions XML + CTRL, MD5 verify, unmatched BO list
- Sessions: history table GET /api/bos/sessions/{id}
- Compliance: 10 checks, progress bar, force refresh
- Export: EOD positions XML download with MD5

## API (client.ts)
bosGetSessions, bosUploadClients, bosUploadPositions,
bosExportPositions, bosGetCompliance, bosRefreshCompliance

## Tests - Day74Tests.cs - 14 tests
MD5(5), ParseClients(2), ParsePositions(2), ExtractMd5(3), models(2)

## Next - Day 75
FIX Engine connector UI + message log viewer
