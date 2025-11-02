"use client"

import { useState, useEffect } from "react"
import { toast } from "@/components/ui/use-toast"
import { useNotifications } from "@/hooks/use-notifications"

export type InvitationStatus = "pending" | "accepted" | "expired" | "declined"

export interface Invitation {
  id: string
  gameId: string
  playerId: string
  playerName: string
  playerAvatar?: string
  playerLevel: number
  status: InvitationStatus
  createdAt: string
  expiresAt: string
}

export interface GameInvitation {
  id: string
  gameId: string
  creatorId: string
  creatorName: string
  creatorAvatar?: string
  date: string
  time: string
  endTime: string
  clubName: string
  clubAddress: string
  level: number
  playersCount: number
  maxPlayers: number
  price: number
  expiresAt: string
}

export function useInvitations(gameId?: string) {
  const [sentInvitations, setSentInvitations] = useState<Invitation[]>([])
  const [receivedInvitations, setReceivedInvitations] = useState<GameInvitation[]>([])
  const { addNotification } = useNotifications()

  // Simuler le chargement des invitations
  useEffect(() => {
    if (gameId) {
      // Simuler un appel API pour récupérer les invitations envoyées pour ce jeu
      setTimeout(() => {
        const mockInvitations: Invitation[] = [
          {
            id: "inv-1",
            gameId,
            playerId: "player-1",
            playerName: "Sophie Martin",
            playerAvatar: "/placeholder.svg?height=40&width=40",
            playerLevel: 5,
            status: "pending",
            createdAt: new Date(Date.now() - 3600000).toISOString(), // 1 heure avant
            expiresAt: new Date(Date.now() + 82800000).toISOString(), // 23 heures après
          },
          {
            id: "inv-2",
            gameId,
            playerId: "player-2",
            playerName: "Jean Dupont",
            playerAvatar: "/placeholder.svg?height=40&width=40",
            playerLevel: 6,
            status: "accepted",
            createdAt: new Date(Date.now() - 7200000).toISOString(), // 2 heures avant
            expiresAt: new Date(Date.now() + 79200000).toISOString(), // 22 heures après
          },
          {
            id: "inv-3",
            gameId,
            playerId: "player-3",
            playerName: "Marie Leroy",
            playerAvatar: "/placeholder.svg?height=40&width=40",
            playerLevel: 4,
            status: "expired",
            createdAt: new Date(Date.now() - 86400000).toISOString(), // 24 heures avant
            expiresAt: new Date(Date.now() - 3600000).toISOString(), // 1 heure avant
          },
        ]
        setSentInvitations(mockInvitations)
      }, 1000)
    } else {
      // Simuler un appel API pour récupérer toutes les invitations reçues par l'utilisateur
      setTimeout(() => {
        const mockReceivedInvitations: GameInvitation[] = [
          {
            id: "inv-r-1",
            gameId: "game-1",
            creatorId: "user-2",
            creatorName: "Thomas Dubois",
            creatorAvatar: "/placeholder.svg?height=40&width=40",
            date: "2025-04-22",
            time: "19:00",
            endTime: "20:00",
            clubName: "Club Padel Paris",
            clubAddress: "123 Avenue des Sports, 75001 Paris",
            level: 5,
            playersCount: 2,
            maxPlayers: 4,
            price: 25,
            expiresAt: new Date(Date.now() + 82800000).toISOString(), // 23 heures après
          },
          {
            id: "inv-r-2",
            gameId: "game-2",
            creatorId: "user-3",
            creatorName: "Julie Petit",
            creatorAvatar: "/placeholder.svg?height=40&width=40",
            date: "2025-04-23",
            time: "18:00",
            endTime: "19:00",
            clubName: "Urban Padel",
            clubAddress: "45 Rue du Sport, 75002 Paris",
            level: 4,
            playersCount: 1,
            maxPlayers: 4,
            price: 22,
            expiresAt: new Date(Date.now() + 79200000).toISOString(), // 22 heures après
          },
        ]
        setReceivedInvitations(mockReceivedInvitations)
      }, 1000)
    }
  }, [gameId, addNotification])

  // Vérifier les invitations expirées toutes les minutes
  useEffect(() => {
    const checkExpiredInvitations = () => {
      const now = new Date()

      // Vérifier les invitations envoyées
      setSentInvitations((prev) =>
        prev.map((inv) => {
          if (inv.status === "pending" && new Date(inv.expiresAt) < now) {
            // Notification pour l'expiration
            addNotification({
              type: "game_invite",
              title: "Invitation expirée",
              message: `L'invitation envoyée à ${inv.playerName} a expiré`,
              time: new Date().toISOString(),
              actionUrl: `/games/${inv.gameId}`,
              actionLabel: "Voir la partie",
            })

            return { ...inv, status: "expired" }
          }
          return inv
        }),
      )

      // Vérifier les invitations reçues
      setReceivedInvitations((prev) => prev.filter((inv) => new Date(inv.expiresAt) > now))
    }

    // Vérifier immédiatement
    checkExpiredInvitations()

    // Puis vérifier toutes les minutes
    const interval = setInterval(checkExpiredInvitations, 60000)

    return () => clearInterval(interval)
  }, [addNotification])

  // Fonctions pour gérer les invitations
  const cancelInvitation = (invitationId: string) => {
    setSentInvitations((prev) => prev.filter((inv) => inv.id !== invitationId))

    toast({
      title: "Invitation annulée",
      description: "L'invitation a été annulée avec succès",
    })
  }

  const resendInvitation = (invitationId: string) => {
    setSentInvitations((prev) =>
      prev.map((inv) => {
        if (inv.id === invitationId) {
          const newExpiry = new Date()
          newExpiry.setHours(newExpiry.getHours() + 24)

          return {
            ...inv,
            status: "pending",
            createdAt: new Date().toISOString(),
            expiresAt: newExpiry.toISOString(),
          }
        }
        return inv
      }),
    )

    toast({
      title: "Invitation renvoyée",
      description: "L'invitation a été renvoyée avec succès",
    })
  }

  const acceptInvitation = (invitationId: string) => {
    // Dans une vraie application, cela déclencherait un appel API
    // et redirigerait vers le processus de paiement

    setReceivedInvitations((prev) => prev.filter((inv) => inv.id !== invitationId))

    toast({
      title: "Invitation acceptée",
      description: "Vous avez rejoint la partie avec succès",
    })
  }

  const declineInvitation = (invitationId: string) => {
    setReceivedInvitations((prev) => prev.filter((inv) => inv.id !== invitationId))

    toast({
      title: "Invitation déclinée",
      description: "Vous avez décliné l'invitation",
    })
  }

  return {
    sentInvitations,
    receivedInvitations,
    cancelInvitation,
    resendInvitation,
    acceptInvitation,
    declineInvitation,
  }
}
