# Day 85 - FIX Order Types

## What was built

### FIX/FIXCertScenarios.cs (new)
FIXCertScenario enum - 12 certification scenarios (S1-S12)
FIXCertResult: Scenario, Passed, ScenarioName, Description, Steps, Errors, RawFIXMessage, TestedAt
FIXCertScenarioRunner.RunAsync - runs individual scenario against any IFIXConnector
FIXCertScenarioRunner.RunAllAsync - runs all 12 scenarios

Scenarios:
- S1: Market Order (35=D OrdType=1)
- S2: Limit Order (35=D OrdType=2 with Price=44)
- S3: MarketAtBest (35=D OrdType=P, DSE-specific)
- S4: IOC Limit (TimeInForce=3)
- S5: FOK Limit (TimeInForce=4)
- S6: Private/Hidden Order
- S7: Iceberg Order (DisplayQty tag 1138)
- S8: MinQty Order (tag 110)
- S9: Cancel Pending Order (35=F)
- S10: Amend Pending Order (35=G)
- S11: Partial Fill Then Cancel
- S12: Reject Invalid Order (zero qty validation)

### FIX/FIXOrderTypeValidator.cs (new)
Static validator for FIX order requests before sending to exchange.
Rules enforced:
- Qty > 0 required
- Limit orders require Price
- MarketAtBest should not have Price (warning)
- FOK + Market not allowed on DSE
- DisplayQty <= Qty (iceberg)
- MinQty <= Qty
- Private orders must be Limit
- Block/BuyIn/SPublic boards: Limit only
- IOC + MarketAtBest non-standard (warning)
Tag mapping helpers:
- GetOrdType: Market=1, Limit=2, MarketAtBest=P
- GetTIF: Day=0, IOC=3, FOK=4
- GetSide: Buy=1, Sell=2

### Controllers/FIXCertController.cs (new)
GET  /api/fix/cert/scenarios - list all 12 scenarios
POST /api/fix/cert/run/{scenarioId} - run single scenario
POST /api/fix/cert/run-all - run all 12 scenarios with summary
POST /api/fix/cert/validate - validate FIXOrderRequest

## Tests - Day85Tests.cs - 50 tests
- FIXCertScenario has 12 values
- All 12 scenario ordinals correct (Theory)
- Validator: valid market, limit, MarketAtBest pass
- Validator: limit without price fails
- Validator: zero quantity fails
- Validator: FOK+Market fails
- Validator: IOC+Limit passes with tag 3
- Validator: FOK+Limit passes with tag 4
- Validator: MarketAtBest has tag P
- Validator: DisplayQty > Qty fails
- Validator: valid iceberg passes
- Validator: MinQty > Qty fails
- Validator: valid MinQty passes
- Validator: Private+Market fails
- Validator: Block board Market fails
- Validator: MarketAtBest with price warns
- GetOrdType tag mapping (Theory x3)
- GetTIF tag mapping (Theory x3)
- GetSide tag mapping (Theory x2)
- S1-S12 all cert scenarios pass
- RunAll returns 12 results
- RunAll all scenarios pass

## Test Results
- Previous: 1,288 passing
- Day 85: 1,338 passing (+50)
- Failed: 0

## Phase A Complete
Days 79-85 all complete. Total: 1,338 tests passing.

## Branch
day-85-fix-order-types (from day-84-fix-connector)

## Next - Phase B (Day 86+)
Settlement engine, contract notes, T+2 settlement, compliance reporting
