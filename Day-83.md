# Day 83 - RMS v2

## What was built

### Models/RMSLimitV2.cs (new)
RMSLevelV2 enum - 6 cascade levels: Client(1), User(2), BOGroup(3), Basket(4), Branch(5), Broker(6)
RMSLimitType enum - 8 limit types: DayBuyValue, DaySellValue, DayNetValue, MaxOrderValue, MaxExposure, ConcentrationPct, MarginUtilization, EDRThreshold
RMSLimitV2 model: Level, LimitType, EntityId, EntityType, BrokerageHouseId, LimitValue, WarnAt(80%), ActionOnBreach, Priority, IsActive
EDRSnapshot model: InvestorId, TotalEquity, TotalDebt, EDRRatio, MarginUsed, MarginLimit, MarginUtilPct, CalculatedAt

### Services/EDRService.cs (new)
EDRResult: TotalEquity, TotalDebt, EDRRatio, MarginUsed, MarginLimit, MarginUtilPct, MarginTier, Warnings, IsBreached
3-tier margin thresholds:
- Safe:     < 50% utilization
- Watch:    50-75% - warn
- Warning:  75-90% - alert
- Critical: > 90%  - block trading
EDR ratio threshold: < 1.5 = breached, < 2.0 = approaching
SaveSnapshotAsync: persists EDR calculation to EDRSnapshots table

### Services/RMSCascadeService.cs (new)
6-level cascade validation: Client -> User -> BOGroup -> Basket -> Branch -> Broker
Each level checks MaxOrderValue, DayBuyValue, MaxExposure with 80% warn threshold
BOGroup lookup: finds investor's BOGroup membership automatically
EDR check at end of cascade for buy orders
Default limits per level (no DB config needed):
- Client:  5M / 20M / 50M
- User:    3M / 15M / 40M
- BOGroup: 50M / 200M / 500M
- Basket:  10M / 50M / 100M
- Branch:  500M / 2B / 5B
- Broker:  5B / 20B / 50B
SetLimitAsync: upsert RMS limit for any level

### Controllers/RMSv2Controller.cs (new)
POST /api/rms/v2/validate - cascade validation
GET  /api/rms/v2/edr/{investorId} - EDR for investor
GET  /api/rms/v2/edr/my - own EDR
POST /api/rms/v2/edr/{investorId}/snapshot - save EDR snapshot
GET  /api/rms/v2/cascade/{investorId} - all cascade limits
POST /api/rms/v2/limits - set RMS limit
GET  /api/rms/v2/margin-tiers - tier definitions

### AppDbContext + Program.cs (updated)
Added: RMSLimitsV2, EDRSnapshots DbSets
Registered: IEDRService, IRMSCascadeService

### Migration: Day83_RMSv2
Creates RMSLimitsV2 and EDRSnapshots tables

## Tests - Day83Tests.cs - 39 tests
- RMSLevelV2 has 6 levels with correct ordinals
- RMSLimitType has 8 types with correct ordinals
- RMSLimitV2 defaults, save/retrieve, all 6 levels persist
- EDRSnapshot save/retrieve
- Margin tier thresholds (Theory x8: Safe/Watch/Warning/Critical)
- EDR ratio: below threshold breached, above not, no debt=999, with debt calculates
- CascadeCheckResult defaults and violation handling
- Cascade level ordering (Client first, Broker last)
- Broker limit larger than Client limit
- Multi-tenant isolation
- MarginUtilPct calculation, zero limit returns zero

## Test Results
- Previous: 1,232 passing
- Day 83: 1,271 passing (+39)
- Failed: 0

## Branch
day-83-rms-v2 (from day-82-multi-exchange)

## Next - Day 84
FIX connector architecture: IFIXConnector, SimulatedFIXConnector, FIXConnectorFactory, FIX message DB log
