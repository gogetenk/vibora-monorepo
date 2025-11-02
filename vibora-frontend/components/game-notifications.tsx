"use client"

import { useEffect } from "react"
import { toast } from "@/components/ui/use-toast"
import { useNotifications } from "@/hooks/use-notifications"

interface GameNotificationsProps {
  gameId: string
}

export function GameNotifications({ gameId }: GameNotificationsProps) {
  const { addNotification } = useNotifications()

  // Simuler des notifications pour le jeu
  useEffect(() => {
    // Simuler une notification de nouveau joueur après 5 secondes
    const newPlayerTimer = setTimeout(() => {
      const notification = {
        type: "game_invite" as const,
        title: "Nouveau joueur",
        message: "Sophie Martin a rejoint votre partie de padel",
        time: "Aujourd'hui, 19:00",
        location: "Club Padel Paris",
        actionUrl: `/games/${gameId}`,
        actionLabel: "Voir la partie",
      }

      addNotification(notification)

      toast({
        title: notification.title,
        description: notification.message,
      })
    }, 5000)

    // Simuler une notification de rappel pour le jour précédent
    const reminderTimer = setTimeout(() => {
      const notification = {
        type: "tournament_reminder" as const,
        title: "Rappel de partie",
        message: "Votre partie de padel est prévue pour demain",
        time: "Demain, 19:00",
        location: "Club Padel Paris",
        actionUrl: `/games/${gameId}`,
        actionLabel: "Voir la partie",
      }

      addNotification(notification)
    }, 10000)

    return () => {
      clearTimeout(newPlayerTimer)
      clearTimeout(reminderTimer)
    }
  }, [gameId, addNotification])

  return null // Ce composant ne rend rien, il gère juste les notifications
}
