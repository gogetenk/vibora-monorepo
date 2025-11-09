"use client"

import { useEffect } from "react"
import { Notification } from "@/components/notification"
import { useNotifications } from "@/hooks/use-notifications"
import { Button } from "@/components/ui/button"
import { Check } from "lucide-react"

interface NotificationsContainerProps {
  initialNotifications?: any[]
  inDropdown?: boolean
}

export function NotificationsContainer({ initialNotifications = [], inDropdown = false }: NotificationsContainerProps) {
  const { notifications, removeNotification, addNotification, markAsRead, markAllAsRead } = useNotifications()

  // Add initial notifications to the store
  useEffect(() => {
    if (initialNotifications.length > 0) {
      initialNotifications.forEach((notification) => {
        addNotification(notification)
      })
    }
  }, [initialNotifications, addNotification])

  if (notifications.length === 0) {
    return (
      <div className="py-6 text-center">
        <p className="text-sm text-gray-500">Aucune notification</p>
      </div>
    )
  }

  const sortedNotifications = [...notifications].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  )

  return (
    <div className="space-y-2 p-3">
      {inDropdown && notifications.some((n) => !n.read) && (
        <div className="flex justify-end pb-1">
          <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => markAllAsRead()}>
            <Check className="mr-1 h-3.5 w-3.5" />
            Tout marquer comme lu
          </Button>
        </div>
      )}

      {sortedNotifications.map((notification) => (
        <Notification
          key={notification.id}
          {...notification}
          onDismiss={removeNotification}
          onRead={markAsRead}
          read={notification.read}
          inDropdown={inDropdown}
        />
      ))}
    </div>
  )
}
