import { useEffect, useState, useCallback } from 'react'
import { messaging, getToken, onMessage } from '@/lib/firebase-config'
import { useNotifications } from '@/hooks/use-notifications'

interface UseFirebaseNotificationsReturn {
  permission: NotificationPermission
  deviceToken: string | null
  isLoading: boolean
  error: Error | null
  requestPermission: () => Promise<void>
  unsubscribe: (() => void) | null
}

export function useFirebaseNotifications(): UseFirebaseNotificationsReturn {
  const [permission, setPermission] = useState<NotificationPermission>('default')
  const [deviceToken, setDeviceToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [unsubscribe, setUnsubscribe] = useState<(() => void) | null>(null)

  const { addNotification } = useNotifications()

  const requestPermission = useCallback(async () => {
    if (!('Notification' in window)) {
      setError(new Error('This browser does not support notifications'))
      return
    }

    if (permission !== 'default') {
      return
    }

    try {
      setIsLoading(true)
      const perm = await Notification.requestPermission()
      setPermission(perm)

      if (perm === 'granted' && messaging) {
        const token = await getToken(messaging, {
          vapidKey: process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY || 'mock-vapid-key',
        })

        if (token) {
          setDeviceToken(token)

          try {
            await fetch('/api/users/register-device-token', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ deviceToken: token }),
              credentials: 'include',
            })
          } catch (err) {
            console.error('Failed to register device token:', err)
          }
        }
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to request permission')
      setError(error)
      console.error('Notification permission request failed:', error)
    } finally {
      setIsLoading(false)
    }
  }, [permission])

  useEffect(() => {
    if (typeof window !== 'undefined' && messaging && permission === 'granted') {
      try {
        const unsubscribeMessage = onMessage(messaging, (payload) => {
          console.log('Foreground notification received:', payload)

          const notification = {
            title: payload.notification?.title || 'Vibora',
            description: payload.notification?.body || 'Nouvelle notification',
            variant: 'default' as const,
          }

          addNotification(notification)

          if ('serviceWorker' in navigator) {
            navigator.serviceWorker.controller?.postMessage({
              type: 'NOTIFICATION_RECEIVED',
              payload,
            })
          }
        })

        setUnsubscribe(() => unsubscribeMessage)

        return () => {
          unsubscribeMessage()
        }
      } catch (err) {
        console.error('Failed to set up foreground message listener:', err)
      }
    }

    return () => {
      if (unsubscribe) {
        unsubscribe()
      }
    }
  }, [permission, messaging, addNotification, unsubscribe])

  return {
    permission,
    deviceToken,
    isLoading,
    error,
    requestPermission,
    unsubscribe,
  }
}
