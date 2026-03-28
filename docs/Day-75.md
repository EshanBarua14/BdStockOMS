# Day 75 - FIX Engine Connector UI + Message Log Viewer

## Backend - AdminFixService.cs (new)
- Implements IAdminFixService (was unimplemented stub in AdminSettingsService)
- GetConfigAsync: loads FIX config from KV store, returns defaults if empty
- UpdateConfigAsync: persists config to KV store, logs ADMIN message
- GetStatusAsync: connected state, uptime, message counts, last 50 messages
- ConnectAsync: sets connected=true, logs Logon + Heartbeat messages
- DisconnectAsync: logs Logout message, sets connected=false
- In-memory message log (max 500 entries) with SeqNum, Direction, MsgType, Body, Timestamp
- FixMessageLog model added to AdminFixService.cs
- Namespace fix: added using BdStockOMS.API.Models.Admin for FIXConfigDto

## Frontend - FixEnginePage.tsx (new, 3 tabs)

### Status tab
- Live connection status card with green/red glowing dot
- Connect and Disconnect buttons (disabled when already in that state)
- 6 stat cards: Session, Uptime, Last Heartbeat, Sent, Received, Connected At
- Auto-refreshes every 5 seconds

### Config tab
- Full FIX config form: SenderCompID, TargetCompID, Host, Port, FIX Version
- Password field, Heartbeat interval, Reconnect interval, Max attempts, Queue size
- Toggle checkboxes: Enabled, Log Messages, Use SSL, Reset On Logon, Reset On Logout
- Save Config button with 2-second saved confirmation flash

### Messages tab
- Real-time message log auto-refreshes every 3 seconds
- Filter buttons: ALL / IN / OUT / ADMIN
- Search by body or message type
- Color coded rows: IN=blue tint, OUT=green tint, ADMIN=neutral
- Columns: SeqNum, Direction badge, MsgType, Body, Timestamp

## Route
/admin/fix -> FixEnginePage (replaced AdminPlaceholderPage)

## Service registration
AdminFixService registered in Program.cs alongside AdminSettingsService

## Tests - Day75Tests.cs - 10 tests
- FIXConfigDto default values correct
- FIXConfigDto password null by default
- FixMessageLog direction values IN/OUT/ADMIN
- FixMessageLog timestamp assignable
- HeartbeatIntervalSec is positive
- Port in valid range 1-65535
- MaxReconnectAttempts is positive
- SeqNum assignable
- FixVersion in known values list
- Body assignable and searchable

## Next - Day 76
Admin Settings full wiring
