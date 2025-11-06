import { useState, useEffect } from 'react'
import { getToken, onMessage } from 'firebase/messaging'
import { messaging } from '@/lib/firebase-config'
import { viboraApi } from '@/lib/api/vibora-client'
import { useToast } from '@/components/ui/use-toast'

export function useNotifications() {
  const [permission, setPermission] = useState<NotificationPermission>('default')
  const [deviceToken, setDeviceToken] = useState<string | null>(null)
  const [isSupported, setIsSupported] = useState(false)
  const { toast } = useToast()

  useEffect(() => {
    // Check if notifications are supported
    setIsSupported(typeof window !== 'undefined' && 'Notification' in window && messaging !== null)

    // Get current permission state
    if (typeof window !== 'undefined' && 'Notification' in window) {
      setPermission(Notification.permission)
    }
  }, [])

  const requestPermission = async () => {
    if (!isSupported || !messaging) {
      toast({
        title: "Non supporté",
        description: "Les notifications ne sont pas supportées sur ce navigateur",
        variant: "destructive"
      })
      return
    }

    try {
      const perm = await Notification.requestPermission()
      setPermission(perm)

      if (perm === 'granted') {
        const token = await getToken(messaging, {
          vapidKey: process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY || "mock-vapid-key"
        })
        setDeviceToken(token)

        // Register token with backend
        await viboraApi.users.registerDeviceToken({ deviceToken: token })

        toast({
          title: "Notifications activées",
          description: "Vous recevrez désormais des notifications push"
        })
      }
    } catch (error) {
      console.error('Error requesting notification permission:', error)
      toast({
        title: "Erreur",
        description: "Impossible d'activer les notifications",
        variant: "destructive"
      })
    }
  }

  // Listen for foreground messages
  useEffect(() => {
    if (messaging && permission === 'granted') {
      const unsubscribe = onMessage(messaging, (payload) => {
        console.log('Foreground notification received:', payload)

        // Show in-app notification toast
        toast({
          title: payload.notification?.title || 'Nouvelle notification',
          description: payload.notification?.body || ''
        })
      })

      return unsubscribe
    }
  }, [permission, messaging, toast])

  return {
    permission,
    deviceToken,
    isSupported,
    requestPermission
  }
}
