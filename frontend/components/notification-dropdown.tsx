"use client"

import { Bell } from "lucide-react"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { NotificationsContainer } from "@/components/notifications-container"
import { useNotifications } from "@/hooks/use-notifications"
import { useEffect, useState } from "react"

export function NotificationDropdown() {
  const { unreadCount, markAllAsRead } = useNotifications()
  const [isOpen, setIsOpen] = useState(false)
  const count = unreadCount()

  // Mark all as read when dropdown is closed
  useEffect(() => {
    if (!isOpen && count > 0) {
      // Optional: You could automatically mark all as read when closing
      // markAllAsRead();
    }
  }, [isOpen, count, markAllAsRead])

  return (
    <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="relative h-8 w-8">
          <Bell className="h-4 w-4" />
          {count > 0 && (
            <span className="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-white">
              {count > 9 ? "9+" : count}
            </span>
          )}
          <span className="sr-only">Notifications</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-[350px] p-0">
        <div className="border-b px-4 py-3">
          <h3 className="text-sm font-medium">Notifications</h3>
        </div>
        <div className="max-h-[60vh] overflow-auto">
          <NotificationsContainer inDropdown />
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
