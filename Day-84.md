# Day 84 - FIX Connector Architecture

## What was built

### FIX/IFIXConnector.cs (new)
Core FIX interface and types:
- FIXSessionState enum: Disconnected, Connecting, Logon, Active, Logout
- FIXMsgType enum: NewOrderSingle, OrderCancelRequest, OrderCancelReplaceRequest, ExecutionReport, Heartbeat, Logon, Logout, Reject
- FIXOrderRequest: all FIX 4.4 order fields (ClOrdID, Symbol, Side, OrdType, TIF, Qty, Price, MinQty, DisplayQty, IsPrivate)
- FIXExecutionReport: ExecType, OrdStatus, CumQty, LeavesQty, AvgPx, TrdMatchID
- FIXSendResult: Success, Message, ClOrdID, RawFIXMessage
- IFIXConnector interface: SendNewOrderAsync, SendCancelAsync, SendAmendAsync, ConnectAsync, DisconnectAsync, GetSessionStatusAsync, ExecutionReportReceived event

### FIX/SimulatedFIXConnector.cs (new)
Production-faithful simulation of FIX 4.4 protocol:
- Builds real FIX raw messages with pipe-delimited fields (| = SOH)
- NewOrderSingle (35=D): Side, OrdType, TIF, Price, MinQty(110), DisplayQty(1138)
- OrderCancelReplaceRequest (35=G): OrigClOrdID, new Qty/Price
- OrderCancelRequest (35=F): ClOrdID, OrigClOrdID
- Fires ExecutionReportReceived event on order acceptance
- Logs all messages to FIXMessageLogs table
- ConnectorName: SimulatedFIX-{exchange}
- IsSimulated: always true

### FIX/FIXConnectorFactory.cs (new)
Config-driven factory pattern:
- Reads FIX:{exchange} config section (SenderCompID, TargetCompID, Host)
- Returns SimulatedFIXConnector when real FIX not configured
- Future-ready: QuickFIXConnector drops in with zero order flow changes
- IsRealFIXConfigured(exchange): checks if real FIX credentials exist

### FIX/FIXOrderService.cs (new)
Bridges order DB to FIX connector:
- PlaceViaFIXAsync: loads order, builds FIXOrderRequest, sends via connector, updates order.Status=Submitted
- CancelViaFIXAsync: sends cancel, updates OrigClOrdID/ClOrdID, Status=CancelRequested
- AmendViaFIXAsync: sends replace, updates Qty/Price/ClOrdID, Status=EditRequested
- Auto-connects if session not active

### Models/FIXMessageLog.cs (new)
Persists every FIX message sent/received:
Fields: BrokerageHouseId, MsgType, Direction(IN/OUT), ClOrdID, Symbol, OrdStatus, RawMessage, MsgSeqNum, IsProcessed, ErrorMessage, SentAt

### Controllers/FIXController.cs (new)
POST /api/fix/connect?exchange= - connect FIX session
POST /api/fix/disconnect?exchange= - disconnect
GET  /api/fix/status?exchange= - session state + connector info
POST /api/fix/orders/{id}/submit - submit order via FIX
POST /api/fix/orders/{id}/cancel - cancel via FIX
POST /api/fix/orders/{id}/amend - amend via FIX
GET  /api/fix/messages - paginated FIX message log
GET  /api/fix/messages/{id}/raw - raw FIX message with SOH

### AppDbContext + Program.cs (updated)
Added: FIXMessageLogs DbSet
Registered: IFIXConnectorFactory (Singleton), IFIXOrderService (Scoped)

### Migration: Day84_FIXConnector
Creates FIXMessageLogs table

## FIX Message Type Tags (4.4)
D=NewOrderSingle, F=CancelRequest, G=CancelReplaceRequest
8=ExecutionReport, 0=Heartbeat, A=Logon, 5=Logout, 3=Reject

## Tests - Day84Tests.cs - 17 tests
- FIXSessionState all 5 values exist
- FIXMsgType all 8 values exist
- FIXOrderRequest creation with all fields
- FIXOrderRequest iceberg fields
- FIXExecutionReport creation
- FIXSendResult success and failure cases
- SimulatedFIXConnector: IsSimulated=true, ConnectorName, initial state
- SimulatedFIXConnector: Connect sets Active
- SimulatedFIXConnector: SendNewOrder returns success with ClOrdID
- SimulatedFIXConnector: SendCancel returns success
- SimulatedFIXConnector: Disconnect sets Disconnected
- SimulatedFIXConnector: ExecutionReport event fires on send
- FIXMessageLog save/retrieve
- FIXMessageLog IN/OUT direction counts
- FIX raw message SOH conversion
- FIX message type tag mapping (D/F/G/8)

## Test Results
- Previous: 1,271 passing
- Day 84: 1,288 passing (+17)
- Failed: 0

## Branch
day-84-fix-connector (from day-83-rms-v2)

## Next - Day 85
FIX order types: MarketAtBest/IOC/FOK/Private/Iceberg/MinQty full flow, FIX cert scenarios S1-S12
