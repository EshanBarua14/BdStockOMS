# Day 17 — Notification System

**Branch:** day-17-notification-system
**Tests:** 172 passing (was 161, +11 new tests)

## What Was Built

### NotificationHub (SignalR)
- Users join personal group "user-{userId}" on connect
- Real-time push via ReceiveNotification event
- Auto-leave group on disconnect

### NotificationService
- CreateAsync — saves to DB + pushes via SignalR
- CreateForMultipleUsersAsync — bulk create for list of users
- MarkAsReadAsync — marks single notification read, records ReadAt
- MarkAllAsReadAsync — marks all unread for user, returns count
- DeleteAsync — removes notification (owner only)
- GetMyNotificationsAsync — paginated, optional unreadOnly filter
- GetUnreadCountAsync — fast count for badge display

### NotificationController (5 endpoints)
- GET /api/notifications — paginated, optional unreadOnly filter
- GET /api/notifications/unread-count — for UI badge
- PUT /api/notifications/{id}/read — mark single read
- PUT /api/notifications/read-all — mark all read
- DELETE /api/notifications/{id} — delete notification

### SignalR Hub Registration
- /hubs/notifications mapped in Program.cs

## Tests Added (NotificationServiceTests.cs — 11 tests)
- Create: valid notification saved, multiple users creates for each
- Mark as read: existing marks read, wrong user not found, already read succeeds
- Mark all as read: marks all unread notifications
- Delete: existing removed, wrong user not found
- Unread count: returns correct count after partial read
- Pagination: unread only filter, correct page size
