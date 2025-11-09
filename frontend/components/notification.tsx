"use client"

import type React from "react"

import { useState } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { Button } from "@/components/ui/button"
import { Bell, X, Calendar, MapPin, Clock, Users, ArrowRight } from "lucide-react"
import { cn } from "@/lib/utils"
import { useRouter } from "next/navigation"

export type NotificationType = "court_available" | "game_invite" | "tournament_reminder" | "payment_reminder"

export interface NotificationProps {
  id: string
  type: NotificationType
  title: string
  message: string
  time?: string
  location?: string
  actionUrl?: string
  actionLabel?: string
  secondaryActionUrl?: string
  secondaryActionLabel?: string
  onDismiss?: (id: string) => void
  onRead?: (id: string) => void
  read?: boolean
  inDropdown?: boolean
}

export function Notification({
  id,
  type,
  title,
  message,
  time,
  location,
  actionUrl,
  actionLabel,
  secondaryActionUrl,
  secondaryActionLabel,
  onDismiss,
  onRead,
  read = false,
  inDropdown = false,
}: NotificationProps) {
  const [isVisible, setIsVisible] = useState(true)
  const router = useRouter()

  const handleDismiss = (e: React.MouseEvent) => {
    e.stopPropagation()
    setIsVisible(false)
    if (onDismiss) {
      setTimeout(() => onDismiss(id), 300) // Allow animation to complete
    }
  }

  const handleAction = () => {
    if (actionUrl) {
      if (onRead && !read) {
        onRead(id)
      }
      router.push(actionUrl)
    }
  }

  const handleSecondaryAction = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (secondaryActionUrl) {
      if (onRead && !read) {
        onRead(id)
      }
      router.push(secondaryActionUrl)
    }
  }

  const handleNotificationClick = () => {
    if (onRead && !read) {
      onRead(id)
    }
  }

  const getTypeStyles = () => {
    switch (type) {
      case "court_available":
        return "bg-success/10 border-success/30"
      case "game_invite":
        return "bg-primary/10 border-primary/30"
      case "tournament_reminder":
        return "bg-secondary/10 border-secondary/30"
      case "payment_reminder":
        return "bg-destructive/10 border-destructive/30"
      default:
        return "bg-muted border-border"
    }
  }

  const getTypeIcon = () => {
    switch (type) {
      case "court_available":
        return <Calendar className="h-4 w-4 text-success" />
      case "game_invite":
        return <Users className="h-4 w-4 text-primary" />
      case "tournament_reminder":
        return <Bell className="h-4 w-4 text-secondary" />
      case "payment_reminder":
        return <Bell className="h-4 w-4 text-destructive" />
      default:
        return <Bell className="h-4 w-4 text-muted-foreground" />
    }
  }

  return (
    <AnimatePresence>
      {isVisible && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, height: 0, marginBottom: 0 }}
          transition={{ duration: 0.2 }}
          className={cn(
            "mb-2 rounded-lg border p-3 shadow-xs cursor-pointer",
            getTypeStyles(),
            !read && "border-l-4",
            inDropdown && "mb-2 last:mb-0",
          )}
          onClick={handleNotificationClick}
        >
          <div className="flex items-start justify-between gap-2">
            <div className="flex items-start gap-2">
              <div className="mt-0.5">{getTypeIcon()}</div>
              <div className="flex-1">
                <p className={cn("font-medium text-sm", !read && "font-semibold")}>{title}</p>
                <p className="text-sm text-muted-foreground">{message}</p>

                {(time || location) && (
                  <div className="mt-2 flex flex-wrap gap-3">
                    {time && (
                      <div className="flex items-center gap-1 text-xs text-muted-foreground">
                        <Clock className="h-3.5 w-3.5" />
                        <span>{time}</span>
                      </div>
                    )}
                    {location && (
                      <div className="flex items-center gap-1 text-xs text-muted-foreground">
                        <MapPin className="h-3.5 w-3.5" />
                        <span>{location}</span>
                      </div>
                    )}
                  </div>
                )}

                {(actionLabel || secondaryActionLabel) && (
                  <div className="mt-3 flex flex-wrap gap-2">
                    {actionLabel && (
                      <Button size="sm" className="h-8 text-xs" onClick={handleAction}>
                        {actionLabel}
                        <ArrowRight className="ml-1 h-3.5 w-3.5" />
                      </Button>
                    )}
                    {secondaryActionLabel && (
                      <Button size="sm" variant="outline" className="h-8 text-xs" onClick={handleSecondaryAction}>
                        {secondaryActionLabel}
                      </Button>
                    )}
                  </div>
                )}
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground"
              onClick={handleDismiss}
            >
              <X className="h-4 w-4" />
              <span className="sr-only">Fermer</span>
            </Button>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
