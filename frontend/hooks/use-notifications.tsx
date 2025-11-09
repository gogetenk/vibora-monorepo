import { create } from "zustand"
import type { NotificationProps } from "@/components/notification"

export interface NotificationItem extends Omit<NotificationProps, "id"> {
  id: string
  read: boolean
  createdAt: Date
}

interface NotificationsState {
  notifications: NotificationItem[]
  addNotification: (notification: Omit<NotificationProps, "id">) => void
  removeNotification: (id: string) => void
  markAsRead: (id: string) => void
  markAllAsRead: () => void
  clearAllNotifications: () => void
  unreadCount: () => number
}

export const useNotifications = create<NotificationsState>((set, get) => ({
  notifications: [],
  addNotification: (notification) =>
    set((state) => ({
      notifications: [
        ...state.notifications,
        {
          ...notification,
          id: `notification-${Date.now()}`,
          read: false,
          createdAt: new Date(),
        },
      ],
    })),
  removeNotification: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((notification) => notification.id !== id),
    })),
  markAsRead: (id) =>
    set((state) => ({
      notifications: state.notifications.map((notification) =>
        notification.id === id ? { ...notification, read: true } : notification,
      ),
    })),
  markAllAsRead: () =>
    set((state) => ({
      notifications: state.notifications.map((notification) => ({ ...notification, read: true })),
    })),
  clearAllNotifications: () => set({ notifications: [] }),
  unreadCount: () => {
    return get().notifications.filter((notification) => !notification.read).length
  },
}))
