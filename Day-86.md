# Day 86 - Settlement Engine

## What was built

### Controllers/SettlementController.cs (new)
GET  /api/settlement/batches - paginated batch list with status filter
GET  /api/settlement/batches/{id} - batch with items
POST /api/settlement/batches/create - create batch for trade date
POST /api/settlement/batches/{id}/process - process batch
POST /api/settlement/batches/{id}/retry - reset failed items and reprocess
GET  /api/settlement/pending - all pending batches
GET  /api/settlement/calculate-date - T+2 date calculator
GET  /api/settlement/my - investor's own settlement items
GET  /api/settlement/stats - pending/processing/completed/failed counts

### BackgroundServices/AutoSettlementService.cs (new)
Runs every hour. Finds all pending batches whose SettlementDate <= today.
Auto-processes due batches via ISettlementService.ProcessBatchAsync.
Logs each batch result (status, item count).
Graceful error handling per batch - one failure doesn't stop others.

### Services/ISettlementService.cs (extended)
Added: GetBatchByIdAsync(batchId, brokerageHouseId)
Added: GetInvestorSettlementsAsync(investorId, brokerageHouseId)
Added: AutoCreateBatchesForTodayAsync(brokerageHouseId)

### Services/SettlementService.cs (extended)
Implemented 3 new interface methods.
AutoCreateBatchesForTodayAsync: creates DSE batch for yesterday if not exists.

### Program.cs (updated)
Registered: AutoSettlementService as hosted background service.

## T+2 Settlement Rules (Bangladesh)
- Trading days: Sunday to Thursday
- Weekend: Friday and Saturday (closed)
- T+2: skip Saturday and Sunday when counting 2 business days
- T0: same-day settlement (special cases only)
- Settlement date never falls on Saturday or Sunday

## Tests - Day86Tests.cs - 31 tests
- SettlementBatchStatus 4 values with correct ordinals
- SettlementItemStatus 3 values with correct ordinals
- SettlementType has T0 and T2
- CalculateSettlementDate: T0 same day
- CalculateSettlementDate: T2 Monday returns Wednesday
- CalculateSettlementDate: T2 Thursday skips weekend
- CalculateSettlementDate: T2 never lands on weekend (Theory x5)
- CalculateSettlementDate: T2 always after trade date
- SettlementBatch defaults and save/retrieve
- SettlementItem defaults and save/retrieve
- GetPendingBatches returns pending only
- NetObligations buy minus sell
- NetObligations net seller is negative
- Multi-tenant isolation
- Failed item can be retried (reset to Pending)
- Bangladesh market Sat/Sun are closed
- Trading days are Sun-Thu

## Test Results
- Previous: 1,338 passing
- Day 86: 1,369 passing (+31)
- Failed: 0

## Branch
day-86-settlement-engine (from day-85-fix-order-types)

## Next - Day 87
Contract notes: ContractNote generation on fill, PDF-ready DTO, ContractNoteController
