# Day 87 - Contract Notes

## What was built

### Controllers/ContractNoteController.cs (rewritten)
POST /api/contract-notes/generate/{orderId} - generate contract note
GET  /api/contract-notes/{id} - get by ID
GET  /api/contract-notes/order/{orderId} - get by order
GET  /api/contract-notes/client/{clientId} - get by client with date range
GET  /api/contract-notes/my - investor's own contract notes
GET  /api/contract-notes/date/{date} - get by trade date
POST /api/contract-notes/regenerate/{orderId} - void + regenerate
POST /api/contract-notes/{id}/void - void with reason
GET  /api/contract-notes/{id}/export - export as text file
GET  /api/contract-notes/stats - daily stats (buy/sell counts, totals)
POST /api/contract-notes/generate-bulk - bulk generate for multiple orders

### Services/ContractNoteAutoGenerateService.cs (new)
IContractNoteAutoGenerateService interface
GenerateOnFillAsync(orderId): auto-generates when order is filled/completed
GeneratePendingAsync(brokerageHouseId): finds all filled orders without notes and generates
Safe: skips if note already exists, logs warnings on failure

### Program.cs (updated)
Registered: IContractNoteAutoGenerateService

## Fee Structure (Bangladesh DSE)
- Commission: 0.5% of gross amount
- CDSC fee: 0.05% of gross amount
- Levy charge: 0.03% of gross amount
- VAT on commission: 15% of commission
- Buy: Net = Gross + all fees
- Sell: Net = Gross - all fees

## Tests - Day87Tests.cs - 21 tests
- ContractNote defaults (Status=Generated, IsVoid=false)
- ContractNote save/retrieve
- Contract note number format (CN-YYYYMMDD-NNNNNN)
- Number padded to 6 digits
- Commission calculation (0.5%)
- CDSC fee (0.05%)
- Levy charge (0.03%)
- VAT on commission (15%)
- Net amount buy (adds fees)
- Net amount sell (deducts fees)
- Net sell < gross
- Net buy > gross
- Contract note can be voided
- Voided excluded from active count
- ContractNoteResult defaults
- ContractNoteDto creation
- ContractNoteSummary creation
- Multiple notes per client
- Settlement date after trade date
- Settlement date is T+2
- Export text contains required fields

## Test Results
- Previous: 1,369 passing
- Day 87: 1,390 passing (+21)
- Failed: 0

## Branch
day-87-contract-notes (from day-86-settlement-engine)

## Next - Day 88
Compliance reporting: AML checks, large trade alerts, suspicious pattern detection
