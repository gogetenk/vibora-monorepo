"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { motion } from "framer-motion"
import { ArrowLeft, Bell, Loader2, AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { useToast } from "@/components/ui/use-toast"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
} from "@/components/ui/vibora-layout"
import { viboraApi } from "@/lib/api/vibora-client"
import type { NotificationPreferencesDto } from "@/lib/api/vibora-types"
import { FADE_IN_ANIMATION_VARIANTS, STAGGER_CONTAINER_VARIANTS } from "@/lib/animation-variants"

// Notification Types Configuration
const NOTIFICATION_TYPES = [
  {
    id: "PlayerJoined",
    label: "Nouveau joueur rejoint",
    description: "Quand quelqu'un rejoint votre partie",
    default: true,
  },
  {
    id: "PlayerLeft",
    label: "Joueur a quitté",
    description: "Quand un joueur quitte votre partie",
    default: true,
  },
  {
    id: "GameCompleted",
    label: "Partie complète (4/4)",
    description: "Quand votre partie est complète",
    default: false,
  },
  {
    id: "GameStartingSoon",
    label: "Rappel 2h avant",
    description: "Rappel 2 heures avant une partie",
    default: true,
  },
  {
    id: "GameCancelled",
    label: "Partie annulée",
    description: "Quand une partie est annulée",
    default: true,
  },
]

export default function NotificationsPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [isMounted, setIsMounted] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [globalEnabled, setGlobalEnabled] = useState(true)
  const [typePrefs, setTypePrefs] = useState<Record<string, boolean>>({})

  // Avoid hydration errors
  useEffect(() => {
    setIsMounted(true)
  }, [])

  // Fetch preferences on mount
  useEffect(() => {
    if (!isMounted) return

    const fetchPreferences = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const { data, error: apiError } = await viboraApi.users.getNotificationPreferences()

        if (apiError) {
          setError(apiError.message)
          // Set defaults on error
          setGlobalEnabled(true)
          const defaultPrefs: Record<string, boolean> = {}
          NOTIFICATION_TYPES.forEach((type) => {
            defaultPrefs[type.id] = type.default
          })
          setTypePrefs(defaultPrefs)
          return
        }

        if (data) {
          setGlobalEnabled(data.pushEnabled)
          setTypePrefs(data.typePreferences || {})
        }
      } catch (err) {
        console.error("Error fetching notification preferences:", err)
        setError("Impossible de charger les préférences")
      } finally {
        setIsLoading(false)
      }
    }

    fetchPreferences()
  }, [isMounted])

  const handleGlobalToggle = async (enabled: boolean) => {
    setGlobalEnabled(enabled)
    setIsSaving(true)

    try {
      const { error: apiError } = await viboraApi.users.updateNotificationPreferences({
        pushEnabled: enabled,
        emailEnabled: false,
        smsEnabled: false,
        typePreferences: typePrefs,
      })

      if (apiError) {
        setError(apiError.message)
        toast({
          variant: "destructive",
          title: "Erreur",
          description: apiError.message,
        })
        return
      }

      toast({
        title: "Succès",
        description: enabled
          ? "Notifications activées"
          : "Notifications désactivées",
      })
    } catch (err) {
      console.error("Error updating global preference:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Impossible de mettre à jour les préférences",
      })
    } finally {
      setIsSaving(false)
    }
  }

  const handleTypeToggle = async (typeId: string, enabled: boolean) => {
    const newPrefs = { ...typePrefs, [typeId]: enabled }
    setTypePrefs(newPrefs)
    setIsSaving(true)

    try {
      const { error: apiError } = await viboraApi.users.updateNotificationPreferences({
        pushEnabled: globalEnabled,
        emailEnabled: false,
        smsEnabled: false,
        typePreferences: newPrefs,
      })

      if (apiError) {
        setError(apiError.message)
        // Revert on error
        setTypePrefs((prev) => {
          const reverted = { ...prev }
          delete reverted[typeId]
          return reverted
        })
        toast({
          variant: "destructive",
          title: "Erreur",
          description: apiError.message,
        })
        return
      }

      toast({
        title: "Succès",
        description: "Préférences mises à jour",
      })
    } catch (err) {
      console.error("Error updating type preference:", err)
      // Revert on error
      setTypePrefs((prev) => {
        const reverted = { ...prev }
        delete reverted[typeId]
        return reverted
      })
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Impossible de mettre à jour les préférences",
      })
    } finally {
      setIsSaving(false)
    }
  }

  // Show loader while mounting or loading to avoid hydration errors
  if (!isMounted || isLoading) {
    return (
      <VPage>
        <VHeader>
          <VContainer>
            <div className="flex items-center gap-4 h-16">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => router.back()}
                className="rounded-full hover:bg-muted/60 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold tracking-tight">Notifications</h1>
            </div>
          </VContainer>
        </VHeader>
        <VMain className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </VMain>
      </VPage>
    )
  }

  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center gap-4 h-16">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => router.back()}
              className="rounded-full hover:bg-muted/60 transition-colors"
            >
              <ArrowLeft className="w-5 h-5" />
            </Button>
            <h1 className="text-xl font-bold tracking-tight">Notifications</h1>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        {error && (
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
            className="mb-4 p-4 rounded-lg bg-destructive/10 border border-destructive/20 flex items-start gap-3"
          >
            <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
            <p className="text-sm text-destructive">{error}</p>
          </motion.div>
        )}

        <motion.div
          variants={STAGGER_CONTAINER_VARIANTS}
          initial="hidden"
          animate="show"
        >
          <VStack spacing="lg">
            {/* Global Enable/Disable Section */}
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="border border-border/50 rounded-2xl bg-card/50 backdrop-blur-sm shadow-sm p-6"
            >
              <div className="flex items-center justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <Bell className="w-5 h-5 text-primary" />
                    <h2 className="text-lg font-bold text-foreground">
                      Notifications Push
                    </h2>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    Recevez des alertes en temps réel sur votre appareil
                  </p>
                </div>
                <Switch
                  checked={globalEnabled}
                  onCheckedChange={handleGlobalToggle}
                  disabled={isSaving}
                  className="flex-shrink-0"
                />
              </div>
            </motion.div>

            {/* Individual Notification Types */}
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="border border-border/50 rounded-2xl bg-card/50 backdrop-blur-sm shadow-sm p-6"
            >
              <h3 className="text-lg font-bold text-foreground mb-4">
                Types de notifications
              </h3>
              <div className="space-y-4">
                {NOTIFICATION_TYPES.map((type) => (
                  <motion.div
                    key={type.id}
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    className={`flex items-start justify-between gap-4 p-4 rounded-xl transition-colors ${
                      !globalEnabled
                        ? "bg-muted/30 opacity-50"
                        : "hover:bg-muted/30"
                    }`}
                  >
                    <div className="flex-1">
                      <Label
                        className={`font-semibold cursor-pointer block ${
                          !globalEnabled ? "text-muted-foreground" : "text-foreground"
                        }`}
                      >
                        {type.label}
                      </Label>
                      <p
                        className={`text-xs mt-1 ${
                          !globalEnabled
                            ? "text-muted-foreground/50"
                            : "text-muted-foreground"
                        }`}
                      >
                        {type.description}
                      </p>
                    </div>
                    <Switch
                      checked={typePrefs[type.id] ?? type.default}
                      onCheckedChange={(v) => handleTypeToggle(type.id, v)}
                      disabled={!globalEnabled || isSaving}
                      className="flex-shrink-0"
                    />
                  </motion.div>
                ))}
              </div>
            </motion.div>

            {/* Info Helper */}
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              className="px-4 py-3 rounded-lg bg-primary/5 border border-primary/10"
            >
              <p className="text-xs text-muted-foreground leading-relaxed">
                <strong>Info:</strong> Les notifications sont essentielles pour rester informé des mises à jour de vos parties.
                Vous pouvez modifier ces préférences à tout moment.
              </p>
            </motion.div>
          </VStack>
        </motion.div>
      </VMain>
    </VPage>
  )
}
