"use client"

import React from "react"
import { motion, useMotionValue, useTransform, PanInfo } from "framer-motion"
import { Card, CardContent } from "@/components/ui/card"
import { Bell, Users, UserX, CheckCircle2, XCircle, Clock } from "lucide-react"
import { cn } from "@/lib/utils"
import { formatDistanceToNow } from "date-fns"
import { fr } from "date-fns/locale"

export type NotificationType = "player_joined" | "player_left" | "game_full" | "reminder" | "game_cancelled"

export interface NotificationData {
  id: string
  type: NotificationType
  title: string
  message: string
  gameId: string
  gameName: string
  createdAt: Date
  isRead: boolean
}

interface NotificationCardProps {
  notification: NotificationData
  onTap?: () => void
  onMarkAsRead?: (id: string) => void
  onDelete?: (id: string) => void
  className?: string
}

const notificationConfig: Record<NotificationType, { icon: React.ElementType; color: string; bgColor: string }> = {
  player_joined: {
    icon: Users,
    color: "text-emerald-500",
    bgColor: "bg-emerald-500/10"
  },
  player_left: {
    icon: UserX,
    color: "text-amber-500",
    bgColor: "bg-amber-500/10"
  },
  game_full: {
    icon: CheckCircle2,
    color: "text-blue-500",
    bgColor: "bg-blue-500/10"
  },
  reminder: {
    icon: Clock,
    color: "text-purple-500",
    bgColor: "bg-purple-500/10"
  },
  game_cancelled: {
    icon: XCircle,
    color: "text-red-500",
    bgColor: "bg-red-500/10"
  },
}

export function NotificationCard({
  notification,
  onTap,
  onMarkAsRead,
  onDelete,
  className,
}: NotificationCardProps) {
  const x = useMotionValue(0)
  const opacity = useTransform(x, [-100, 0, 100], [0, 1, 0])
  const scale = useTransform(x, [-100, 0, 100], [0.8, 1, 0.8])

  const config = notificationConfig[notification.type]
  const Icon = config.icon
  const timeAgo = formatDistanceToNow(notification.createdAt, {
    addSuffix: true,
    locale: fr
  })

  const handleDragEnd = (event: MouseEvent | TouchEvent | PointerEvent, info: PanInfo) => {
    const threshold = 100

    if (info.offset.x > threshold && onMarkAsRead && !notification.isRead) {
      onMarkAsRead(notification.id)
    } else if (info.offset.x < -threshold && onDelete) {
      onDelete(notification.id)
    }

    x.set(0)
  }

  const handleTap = () => {
    if (onTap) {
      onTap()
    }
  }

  return (
    <motion.div
      className="relative overflow-hidden"
      drag="x"
      dragConstraints={{ left: 0, right: 0 }}
      dragElastic={0.2}
      onDragEnd={handleDragEnd}
      style={{ x }}
    >
      {/* Swipe actions background */}
      <div className="absolute inset-0 flex items-center justify-between px-6 z-0">
        <div className="flex items-center gap-2 text-emerald-500">
          <CheckCircle2 className="w-5 h-5" />
          <span className="text-sm font-medium">Marquer lu</span>
        </div>
        <div className="flex items-center gap-2 text-red-500">
          <span className="text-sm font-medium">Supprimer</span>
          <XCircle className="w-5 h-5" />
        </div>
      </div>

      {/* Notification card */}
      <motion.div
        style={{ opacity, scale }}
        whileTap={{ scale: 0.98 }}
        onClick={handleTap}
        className="cursor-pointer relative z-10 bg-white dark:bg-gray-950"
      >
        <Card
          className={cn(
            "overflow-hidden transition-all duration-300 border shadow-sm hover:shadow-lg hover:scale-[1.01]",
            !notification.isRead ? "border-primary/20 bg-primary/[0.02]" : "border-border/40",
            className
          )}
        >
          <CardContent className="flex items-start gap-4 p-5">
            {/* Icon */}
            <div className={cn(
              "flex items-center justify-center w-11 h-11 rounded-xl shrink-0 transition-transform duration-200",
              config.bgColor,
              !notification.isRead && "ring-2 ring-primary/20"
            )}>
              <Icon className={cn("w-5 h-5", config.color)} />
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-3 mb-1.5">
                <h3 className="font-bold text-[15px] leading-snug text-foreground">
                  {notification.title}
                </h3>
                {!notification.isRead && (
                  <div className="w-2.5 h-2.5 rounded-full bg-primary shrink-0 mt-1.5 shadow-lg shadow-primary/50 animate-pulse" />
                )}
              </div>

              <p className="text-[13px] leading-relaxed text-muted-foreground line-clamp-2 mb-3">
                {notification.message}
              </p>

              <div className="flex items-center justify-between gap-2">
                <span className="text-xs text-muted-foreground/70 font-medium">
                  {timeAgo}
                </span>
                <span className="text-xs font-semibold text-primary px-2.5 py-1 bg-primary/8 rounded-md">
                  {notification.gameName}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.div>
    </motion.div>
  )
}

// Skeleton loader
export function NotificationCardSkeleton() {
  return (
    <Card className="overflow-hidden border border-border/40 shadow-sm bg-white dark:bg-gray-950">
      <CardContent className="flex items-start gap-4 p-5">
        <div className="w-11 h-11 rounded-xl bg-muted/50 animate-pulse shrink-0" />
        <div className="flex-1 space-y-3">
          <div className="h-4 bg-muted/50 rounded-md animate-pulse w-3/4" />
          <div className="h-3 bg-muted/50 rounded-md animate-pulse w-full" />
          <div className="h-3 bg-muted/50 rounded-md animate-pulse w-11/12" />
          <div className="flex items-center justify-between mt-2">
            <div className="h-3 bg-muted/50 rounded-md animate-pulse w-20" />
            <div className="h-5 bg-muted/50 rounded-md animate-pulse w-28" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
