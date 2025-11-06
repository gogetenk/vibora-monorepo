# Firebase Cloud Messaging Setup Guide

This guide documents the Firebase Cloud Messaging (FCM) implementation for Vibora's push notification system.

## Overview

Firebase Cloud Messaging provides a secure, cross-platform solution for sending push notifications to web clients. The implementation supports both foreground and background notification handling via Service Workers.

## Files Created

### Frontend Configuration

1. **lib/firebase-config.ts** - Firebase initialization
   - Initializes Firebase app with configuration from environment variables
   - Exports messaging instance for use in React components
   - Includes SSR-safe guards (checks for `typeof window`)
   - Provides getToken and onMessage exports

2. **lib/hooks/use-firebase-notifications.ts** - React Hook for FCM
   - Manages notification permissions
   - Handles device token registration
   - Listens for foreground messages
   - Integrates with existing Zustand notification store
   - API: `{ permission, deviceToken, isLoading, error, requestPermission, unsubscribe }`

3. **public/firebase-messaging-sw.js** - Service Worker
   - Handles background notifications
   - Displays native notifications when app is closed/backgrounded
   - Handles notification clicks (opens URL if provided)
   - Logs notification events for debugging

4. **components/service-worker-register.tsx** - SW Registration
   - Client component that registers firebase-messaging-sw.js on app startup
   - Handles registration errors gracefully
   - Scope set to "/" for all routes

5. **app/layout.tsx** - Updated
   - Added `<ServiceWorkerRegister />` component
   - Ensures Service Worker is registered before notifications can be received

## Environment Variables

Add to `.env.local`:

```bash
# Firebase Web App Configuration
NEXT_PUBLIC_FIREBASE_API_KEY=your_firebase_api_key_here
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your_firebase_project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your_firebase_project_id_here
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your_firebase_messaging_sender_id_here
NEXT_PUBLIC_FIREBASE_APP_ID=your_firebase_app_id_here

# Firebase Web Push Certificate
NEXT_PUBLIC_FIREBASE_VAPID_KEY=your_firebase_vapid_key_here
```

See `.env.local.example` for the complete template with instructions.

## Setup Steps

### 1. Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Create a project" or select existing project
3. Project name: "Vibora" (or your app name)
4. Region: Select closest to your users (typically EU for France)
5. Enable Analytics (optional)

### 2. Register Web App

1. In Firebase Console, go to Project Settings
2. Click "Your apps" section
3. Click web icon (</>)
4. Register app with name "Vibora Web"
5. Copy the Firebase configuration

### 3. Configure Cloud Messaging

1. In Firebase Console, go to Cloud Messaging
2. Click "Web Configuration"
3. Generate a key pair if needed
4. Copy the VAPID public key
5. Save the messaging sender ID (same as in Web App config)

### 4. Update Environment Variables

```bash
# From Firebase Console > Project Settings > Your apps > Web app
NEXT_PUBLIC_FIREBASE_API_KEY=AIzaSyA...
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=vibora-mvp.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=vibora-mvp
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=123456789
NEXT_PUBLIC_FIREBASE_APP_ID=1:123456789:web:abc123...

# From Cloud Messaging > Web Push certificates
NEXT_PUBLIC_FIREBASE_VAPID_KEY=BKe5...
```

### 5. Deploy Service Worker Configuration

The firebase-messaging-sw.js is already configured but uses mock keys for development. Update with real credentials for production:

```javascript
// public/firebase-messaging-sw.js
const firebaseConfig = {
  apiKey: 'YOUR_REAL_API_KEY', // Replace for production
  // ... other config
}
```

## Usage in Components

### Request Notification Permission

```typescript
'use client'

import { useFirebaseNotifications } from '@/lib/hooks/use-firebase-notifications'

export function NotificationPrompt() {
  const { permission, requestPermission, isLoading, error } = useFirebaseNotifications()

  return (
    <div>
      {permission === 'default' && (
        <button onClick={requestPermission} disabled={isLoading}>
          Enable Push Notifications
        </button>
      )}
      {permission === 'granted' && <span>Notifications enabled</span>}
      {permission === 'denied' && <span>Notifications blocked</span>}
      {error && <span>Error: {error.message}</span>}
    </div>
  )
}
```

## Notification Flow

### Foreground (App Open)
1. FCM message received → Service Worker
2. Service Worker passes to app via `onMessage`
3. React hook displays toast via Zustand store
4. Toast shown using existing notification system

### Background (App Closed)
1. FCM message received → Service Worker
2. Service Worker calls `showNotification()` (native OS notification)
3. User clicks notification → Service Worker opens URL or focuses window

## Backend Integration

### 1. Endpoint: Register Device Token

Create endpoint to store device tokens:

```csharp
[HttpPost("api/users/register-device-token")]
public async Task<IActionResult> RegisterDeviceToken(
    [FromBody] RegisterDeviceTokenRequest request)
{
    // Validate token format
    // Store in database: Users.DeviceTokens table
    // Associate with current user
    return Ok();
}
```

### 2. Sending Notifications

Use Firebase Admin SDK from backend:

```csharp
var message = new Message()
{
    Token = deviceToken,
    Notification = new Notification()
    {
        Title = "Game Updated",
        Body = "A player joined your game!",
    },
    Data = new Dictionary<string, string>
    {
        { "gameId", gameId },
        { "url", $"/games/{gameId}" },
    },
};

var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

### 3. Bulk Sending (Topic-based)

For sending to multiple users:

```csharp
// Subscribe user to topic
await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
    new[] { deviceToken },
    "game_updates"
);

// Send to all subscribers
var message = new Message()
{
    Topic = "game_updates",
    Notification = new Notification()
    {
        Title = "Game Alert",
        Body = "A new game is available!",
    },
};

await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

## Development vs Production

### Development (Mock Keys)
- Uses mock Firebase configuration
- Service Worker still registers correctly
- Notifications won't actually be received
- Safe for local testing without real credentials

### Production (Real Keys)
- Update all NEXT_PUBLIC_FIREBASE_* variables
- Update firebase-config.ts to use real values
- Deploy to Vercel (set env vars in project settings)
- Enable HTTPS (required for Service Workers and FCM)

## Troubleshooting

### Service Worker Not Registering
- Check browser console for errors
- Verify firebase-messaging-sw.js exists in `public/` directory
- Ensure app is served over HTTPS (required in production)
- Check service worker scope (should be "/")

### Notifications Not Showing
- Verify notification permission: `Notification.permission` should be "granted"
- Check browser notification settings
- Verify Firebase credentials are correct
- Check browser console for FCM errors

### Device Token Not Registering
- Ensure `/api/users/register-device-token` endpoint exists on backend
- Verify authentication headers are sent
- Check network tab in DevTools for API errors
- Ensure token has valid format from Firebase

### Foreground Messages Not Displayed
- Verify `onMessage` listener is attached in hook
- Check that Zustand store `addNotification` is working
- Verify message payload has `notification.title` or `notification.body`
- Check console for JavaScript errors

## Testing

### Manual Testing

1. Open app in browser (HTTPS required in production)
2. Request notification permission
3. Note the device token in console
4. Send test message from Firebase Console:
   - Go to Cloud Messaging
   - Click "Send your first message"
   - Select "Web" platform
   - Enter message title/body
   - Target: Single device (paste device token)
   - Send

### Automated Testing

```typescript
// Example test (using jest-mock-firebase)
jest.mock('firebase/messaging', () => ({
  getToken: jest.fn().mockResolvedValue('mock-token-123'),
  onMessage: jest.fn((cb) => cb({
    notification: { title: 'Test', body: 'Test body' }
  }))
}))

test('should request and store notification permission', async () => {
  const { requestPermission } = renderHook(() => useFirebaseNotifications())
  await requestPermission()
  // Assert permission stored
})
```

## Security Considerations

1. **VAPID Key**: Keep public key private, don't hardcode
2. **Device Tokens**: Store securely, associate with user ID
3. **Validation**: Verify token ownership before sending
4. **Rate Limiting**: Implement per-user notification limits
5. **HTTPS Required**: Service Workers and FCM require HTTPS

## Performance Notes

- Service Worker runs in separate thread (non-blocking)
- Foreground notifications use existing React toast system
- Background notifications are OS-native (minimal overhead)
- Device tokens cached locally for 1 hour (browser optimization)

## References

- [Firebase Cloud Messaging Web Documentation](https://firebase.google.com/docs/cloud-messaging/js/client)
- [Service Worker API](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)
- [Web Push Protocol](https://datatracker.ietf.org/doc/html/draft-thomson-webpush-protocol)

## Next Steps

1. Set up Firebase project with real credentials
2. Create backend endpoint for device token registration
3. Implement notification sending logic on backend
4. Test in development environment
5. Deploy to production with real Firebase credentials
6. Monitor FCM delivery rates and errors
