"use client"

import { useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import { toast } from "@/components/ui/use-toast"
import type {
  MagicLinkValidation,
  MagicLinkCreateResponse,
  GuestPlayerData,
  GuestJoinResponse,
  ConversionResponse
} from "@/types/magic-links"

/**
 * Hook pour valider un Magic Link
 */
export function useMagicLinkValidation() {
  const [validation, setValidation] = useState<MagicLinkValidation | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const validateToken = useCallback(async (token: string) => {
    setIsLoading(true)
    
    try {
      // Simuler la validation (remplacer par un vrai appel API)
      const response = await fetch(`/api/magic-links/validate/${token}`)
      const data = await response.json()

      if (!response.ok) {
        throw new Error(data.error || 'Validation failed')
      }

      setValidation(data.validation)
      return data.validation
    } catch (error) {
      const validationError: MagicLinkValidation = {
        isValid: false,
        isExpired: false,
        game: null,
        error: error instanceof Error ? error.message : 'Erreur de validation'
      }
      setValidation(validationError)
      return validationError
    } finally {
      setIsLoading(false)
    }
  }, [])

  return {
    validation,
    isLoading,
    validateToken
  }
}

/**
 * Hook pour créer des Magic Links
 */
export function useMagicLinkGeneration() {
  const [isGenerating, setIsGenerating] = useState(false)

  const createMagicLink = useCallback(async (gameId: string, options?: {
    expiresIn?: string
    maxUses?: number
  }): Promise<MagicLinkCreateResponse | null> => {
    setIsGenerating(true)

    try {
      const response = await fetch('/api/magic-links/generate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          gameId,
          expiresIn: options?.expiresIn || '7d',
          maxUses: options?.maxUses
        })
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(data.error || 'Failed to create magic link')
      }

      toast({
        title: "Lien créé !",
        description: "Votre lien d'invitation est prêt à être partagé"
      })

      return data
    } catch (error) {
      toast({
        title: "Erreur",
        description: "Impossible de créer le lien d'invitation",
        variant: "destructive"
      })
      return null
    } finally {
      setIsGenerating(false)
    }
  }, [])

  return {
    isGenerating,
    createMagicLink
  }
}

/**
 * Hook pour gérer la participation d'invités
 */
export function useGuestParticipation() {
  const [isJoining, setIsJoining] = useState(false)
  const router = useRouter()

  const joinAsGuest = useCallback(async (
    token: string,
    guestData: Omit<GuestPlayerData, 'gameId' | 'magicLinkToken'>
  ): Promise<boolean> => {
    setIsJoining(true)

    try {
      const response = await fetch('/api/magic-links/join-guest', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          token,
          guestData
        })
      })

      const data: GuestJoinResponse = await response.json()

      if (!response.ok) {
        throw new Error(data.error || 'Failed to join game')
      }

      toast({
        title: `Parfait ! Vous êtes dans la partie 🎾`,
        description: `${guestData.name}, votre place est réservée`
      })

      // Rediriger vers la page de succès
      router.push(data.redirectUrl)
      return true

    } catch (error) {
      toast({
        title: "Erreur",
        description: error instanceof Error ? error.message : "Impossible de rejoindre la partie",
        variant: "destructive"
      })
      return false
    } finally {
      setIsJoining(false)
    }
  }, [router])

  return {
    isJoining,
    joinAsGuest
  }
}

/**
 * Hook pour gérer la conversion des invités
 */
export function useGuestConversion() {
  const [isConverting, setIsConverting] = useState(false)
  const router = useRouter()

  const convertGuestToUser = useCallback(async (
    email: string,
    guestData: {
      name: string
      phone: string
      level: string
      gameId: string
    }
  ): Promise<boolean> => {
    setIsConverting(true)

    try {
      const response = await fetch('/api/magic-links/convert-guest', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email,
          guestData
        })
      })

      const data: ConversionResponse = await response.json()

      if (!response.ok) {
        throw new Error(data.error || 'Conversion failed')
      }

      toast({
        title: "Compte créé avec succès ! 🎉",
        description: "Vous recevrez vos notifications de match par email"
      })

      // Rediriger vers l'onboarding
      if (data.onboardingUrl) {
        router.push(data.onboardingUrl)
      }

      return true

    } catch (error) {
      toast({
        title: "Erreur",
        description: error instanceof Error ? error.message : "Impossible de créer le compte",
        variant: "destructive"
      })
      return false
    } finally {
      setIsConverting(false)
    }
  }, [router])

  return {
    isConverting,
    convertGuestToUser
  }
}

/**
 * Hook pour gérer le partage de Magic Links
 */
export function useMagicLinkSharing() {
  const shareLink = useCallback(async (
    link: string,
    gameData: {
      date: string
      time: string
      club: { name: string }
      creator: { name: string }
      spotsLeft: number
    },
    method: 'whatsapp' | 'email' | 'native' | 'copy' = 'copy'
  ) => {
    const formatDate = (dateString: string) => {
      const date = new Date(dateString)
      return date.toLocaleDateString('fr-FR', { 
        weekday: 'long', 
        day: 'numeric', 
        month: 'long' 
      })
    }

    switch (method) {
      case 'whatsapp':
        const whatsappMessage = `Salut ! ${gameData.creator.name} t'invite à une partie de padel le ${formatDate(gameData.date)} à ${gameData.time} chez ${gameData.club.name}.\n\nPlus que ${gameData.spotsLeft} place${gameData.spotsLeft > 1 ? 's' : ''} !\n\nRéserve ta place : ${link}`
        const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(whatsappMessage)}`
        window.open(whatsappUrl, '_blank')
        break

      case 'email':
        const subject = `Invitation padel - ${gameData.club.name}`
        const body = `Salut !\n\n${gameData.creator.name} t'invite à une partie de padel :\n\n📅 ${formatDate(gameData.date)}\n⏰ ${gameData.time}\n📍 ${gameData.club.name}\n\nPlus que ${gameData.spotsLeft} place${gameData.spotsLeft > 1 ? 's' : ''} disponible${gameData.spotsLeft > 1 ? 's' : ''} !\n\nRéserve ta place en un clic : ${link}\n\nÀ bientôt sur le terrain ! 🎾`
        const emailUrl = `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
        window.location.href = emailUrl
        break

      case 'native':
        if (navigator.share) {
          try {
            await navigator.share({
              title: `Partie padel - ${gameData.club.name}`,
              text: `${gameData.creator.name} t'invite à une partie de padel le ${formatDate(gameData.date)} à ${gameData.time}`,
              url: link
            })
          } catch (error) {
            // L'utilisateur a annulé ou erreur, fallback vers copy
            await navigator.clipboard.writeText(link)
            toast({
              title: "Lien copié !",
              description: "Le lien d'invitation a été copié dans le presse-papier"
            })
          }
        } else {
          // Fallback vers copy si pas de support native share
          await navigator.clipboard.writeText(link)
          toast({
            title: "Lien copié !",
            description: "Le lien d'invitation a été copié dans le presse-papier"
          })
        }
        break

      case 'copy':
      default:
        try {
          await navigator.clipboard.writeText(link)
          toast({
            title: "Lien copié !",
            description: "Le lien d'invitation a été copié dans le presse-papier"
          })
        } catch (error) {
          toast({
            title: "Erreur",
            description: "Impossible de copier le lien",
            variant: "destructive"
          })
        }
        break
    }

    // Track analytics (optionnel)
    try {
      await fetch('/api/analytics/magic-link-share', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          link,
          method,
          gameId: gameData
        })
      })
    } catch (error) {
      // Analytics non critiques, ne pas faire échouer l'action
      console.warn('Failed to track share analytics:', error)
    }
  }, [])

  return { shareLink }
}