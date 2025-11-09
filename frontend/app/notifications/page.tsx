"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { motion, AnimatePresence } from "framer-motion"
import { ArrowLeft, Bell, BellOff, Trash2, CheckCheck } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  NotificationCard,
  NotificationCardSkeleton,
  NotificationData,
  NotificationType
} from "@/components/ui/notification-card"
import {
  VPage,
  VHeader,
  VMain,
  VStack
} from "@/components/ui/vibora-layout"
import {
  FADE_IN_ANIMATION_VARIANTS,
  STAGGER_CONTAINER_VARIANTS
} from "@/lib/animation-variants"
import { cn } from "@/lib/utils"
import { useToast } from "@/components/ui/use-toast"
import { viboraApi } from "@/lib/api/vibora-client"
import type { NotificationHistoryDto } from "@/lib/api/vibora-types"

export default function NotificationsPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [notifications, setNotifications] = useState<NotificationData[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isMounted, setIsMounted] = useState(false)

  // Mount state
  useEffect(() => {
    setIsMounted(true)
  }, [])

  // Load notifications from API
  useEffect(() => {
    const loadNotifications = async () => {
      setIsLoading(true)
      const { data, error } = await viboraApi.notifications.getAll()

      if (error) {
        toast({
          title: "Erreur",
          description: error.message,
          variant: "destructive",
          duration: 3000
        })
        setIsLoading(false)
        return
      }

      // Map NotificationHistoryDto to NotificationData
      const mappedNotifications: NotificationData[] = (data || []).map((dto: NotificationHistoryDto) => ({
        id: dto.notificationId,
        type: mapNotificationType(dto.type),
        title: dto.title,
        message: dto.body,
        gameId: dto.gameId || "",
        gameName: dto.title, // Use title as fallback for gameName
        createdAt: new Date(dto.createdAt),
        isRead: dto.isRead
      }))

      // Sort by createdAt DESC
      const sorted = mappedNotifications.sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      )

      setNotifications(sorted)
      setIsLoading(false)
    }

    loadNotifications()
  }, [toast])

  // Map backend notification type to frontend NotificationType
  const mapNotificationType = (backendType: string): NotificationType => {
    const typeMap: Record<string, NotificationType> = {
      "player_joined": "player_joined",
      "reminder": "reminder",
      "game_cancelled": "game_cancelled",
      "game_full": "game_full",
      "player_left": "player_left",
      "default": "reminder"
    }
    return typeMap[backendType] || "reminder"
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  const handleNotificationTap = (notification: NotificationData) => {
    // Mark as read
    if (!notification.isRead) {
      handleMarkAsRead(notification.id)
    }

    // Navigate to game detail
    router.push(`/games/${notification.gameId}`)
  }

  const handleMarkAsRead = async (id: string) => {
    // Optimistic UI update
    setNotifications(prev =>
      prev.map(n => n.id === id ? { ...n, isRead: true } : n)
    )

    // Call API
    const { error } = await viboraApi.notifications.markAsRead(id)
    if (error) {
      // Rollback on error
      setNotifications(prev =>
        prev.map(n => n.id === id ? { ...n, isRead: false } : n)
      )
      toast({
        title: "Erreur",
        description: error.message,
        variant: "destructive",
        duration: 2000
      })
      return
    }

    toast({
      title: "Notification marquée comme lue",
      duration: 2000
    })
  }

  const handleDelete = async (id: string) => {
    // Optimistic UI update
    setNotifications(prev => prev.filter(n => n.id !== id))

    // Call API
    const { error } = await viboraApi.notifications.delete(id)
    if (error) {
      // Rollback: reload notifications
      const { data } = await viboraApi.notifications.getAll()
      if (data) {
        const mappedNotifications: NotificationData[] = data.map((dto: NotificationHistoryDto) => ({
          id: dto.notificationId,
          type: mapNotificationType(dto.type),
          title: dto.title,
          message: dto.body,
          gameId: dto.gameId || "",
          gameName: dto.title,
          createdAt: new Date(dto.createdAt),
          isRead: dto.isRead
        }))
        const sorted = mappedNotifications.sort(
          (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        )
        setNotifications(sorted)
      }
      toast({
        title: "Erreur",
        description: error.message,
        variant: "destructive",
        duration: 2000
      })
      return
    }

    toast({
      title: "Notification supprimée",
      duration: 2000
    })
  }

  const handleMarkAllAsRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))

    toast({
      title: "Toutes les notifications marquées comme lues",
      duration: 2000
    })
  }

  const handleClearAll = () => {
    const readNotifications = notifications.filter(n => n.isRead)
    setNotifications(readNotifications)

    toast({
      title: "Notifications lues supprimées",
      duration: 2000
    })
  }

  // Show skeleton while loading or not mounted
  if (!isMounted || isLoading) {
    return (
      <VPage animate={false}>
        <VHeader>
          <div className="container flex items-center justify-between h-16">
            <div className="flex items-center gap-3">
              <Button
                variant="ghost"
                size="icon"
                className="rounded-full"
              >
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold">Notifications</h1>
            </div>
          </div>
        </VHeader>

        <VMain>
          <VStack spacing="md" className="mt-4">
            {[1, 2, 3, 4].map(i => (
              <NotificationCardSkeleton key={i} />
            ))}
          </VStack>
        </VMain>
      </VPage>
    )
  }

  return (
    <VPage animate={false} className="pb-32">
      {/* Header */}
      <VHeader animate={false}>
        <div className="container flex items-center justify-between h-16">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => router.back()}
              className="rounded-full"
            >
              <ArrowLeft className="w-5 h-5" />
            </Button>
            <h1 className="text-xl font-bold">Notifications</h1>
          </div>

          {/* Actions */}
          {notifications.length > 0 && (
            <div className="flex items-center gap-2">
              {unreadCount > 0 && (
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleMarkAllAsRead}
                  className="rounded-full"
                >
                  <CheckCheck className="w-5 h-5 text-muted-foreground" />
                </Button>
              )}
              <Button
                variant="ghost"
                size="icon"
                onClick={handleClearAll}
                className="rounded-full"
              >
                <Trash2 className="w-5 h-5 text-muted-foreground" />
              </Button>
            </div>
          )}
        </div>
      </VHeader>

      {/* Main content */}
      <VMain>
        <motion.div
          initial="hidden"
          animate="show"
          variants={STAGGER_CONTAINER_VARIANTS}
          className="space-y-6"
        >
          {/* Unread count indicator */}
          {unreadCount > 0 && (
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="flex items-center gap-4 px-5 py-4 rounded-2xl bg-primary/[0.08] border border-primary/20 shadow-sm"
            >
              <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-primary/20 relative">
                <Bell className="w-5 h-5 text-primary" />
                <div className="absolute -top-1 -right-1 w-5 h-5 bg-primary text-white text-[10px] font-bold rounded-full flex items-center justify-center shadow-lg shadow-primary/50">
                  {unreadCount}
                </div>
              </div>
              <div>
                <p className="text-[15px] font-bold text-foreground">
                  Vous avez {unreadCount} notification{unreadCount > 1 ? 's' : ''}
                </p>
                <p className="text-xs text-muted-foreground/80 mt-0.5">
                  Appuyez pour voir les détails
                </p>
              </div>
            </motion.div>
          )}

          {/* Empty state */}
          {notifications.length === 0 && (
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="flex flex-col items-center justify-center py-20 text-center"
            >
              <div className="flex items-center justify-center w-24 h-24 mb-6 rounded-2xl bg-gradient-to-br from-muted/30 to-muted/10 shadow-sm">
                <BellOff className="w-12 h-12 text-muted-foreground/50" />
              </div>
              <h2 className="text-xl font-bold mb-2 text-foreground">Aucune notification</h2>
              <p className="text-sm text-muted-foreground max-w-xs font-medium">
                Vous êtes à jour ! 🎉
              </p>
              <p className="text-xs text-muted-foreground/70 mt-3 max-w-sm leading-relaxed">
                Les mises à jour de vos parties apparaîtront ici.
              </p>
            </motion.div>
          )}

          {/* Notifications list */}
          {notifications.length > 0 && (
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <VStack spacing="md">
                <AnimatePresence mode="popLayout">
                  {notifications.map((notification, index) => (
                    <motion.div
                      key={notification.id}
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, x: -100 }}
                      transition={{
                        type: "spring",
                        stiffness: 300,
                        damping: 30,
                        delay: index * 0.05
                      }}
                    >
                      <NotificationCard
                        notification={notification}
                        onTap={() => handleNotificationTap(notification)}
                        onMarkAsRead={handleMarkAsRead}
                        onDelete={handleDelete}
                      />
                    </motion.div>
                  ))}
                </AnimatePresence>
              </VStack>
            </motion.div>
          )}

          {/* Swipe hint - only show if there are unread notifications */}
          {unreadCount > 0 && notifications.length > 0 && (
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="flex flex-col items-center justify-center gap-2 pt-6 pb-2"
            >
              <div className="flex items-center gap-3 text-xs font-medium text-muted-foreground/70">
                <span className="px-3 py-1.5 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 rounded-full">
                  ← Marquer comme lu
                </span>
                <span className="px-3 py-1.5 bg-red-500/10 text-red-600 dark:text-red-400 rounded-full">
                  Supprimer →
                </span>
              </div>
            </motion.div>
          )}
        </motion.div>
      </VMain>
    </VPage>
  )
}
