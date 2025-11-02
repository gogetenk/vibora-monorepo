"use client"

import { useState, useEffect } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { motion } from "framer-motion"
import { ArrowLeft, Users, User } from "lucide-react"
import { GameFormFields } from "@/components/forms/GameFormFields"
import { Button } from "@/components/ui/button"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import {
  VInput,
  VForm,
  VFormSection,
  VLocationInput,
  VLevelSelector,
  VButton,
} from "@/components/ui/vibora-form"
import { viboraApi } from "@/lib/api/vibora-client"
import { getSession } from "@/lib/auth/supabase-auth"
import { setGuestAuth } from "@/lib/auth/guest-auth"
import { useToast } from "@/components/ui/use-toast"
import { FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"

export default function CreateGame() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const { toast } = useToast()

  // Form state
  const [location, setLocation] = useState("")
  const [coordinates, setCoordinates] = useState<{ lat: number; lng: number } | null>(null)
  const [selectedDate, setSelectedDate] = useState(
    new Date().toISOString().split("T")[0]
  )
  const [selectedTime, setSelectedTime] = useState("18:00")
  const [skillLevel, setSkillLevel] = useState("5")
  const [maxPlayers, setMaxPlayers] = useState("4")
  const [guestName, setGuestName] = useState("")
  const [guestPhone, setGuestPhone] = useState("")
  const [guestToken, setGuestToken] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)

  // Pre-fill form from URL params (when coming from search page)
  useEffect(() => {
    const whenParam = searchParams.get('when')
    const whereParam = searchParams.get('where')
    const levelParam = searchParams.get('level')

    if (whenParam) {
      try {
        // Parse ISO date: "2024-11-02T20:00:00.000Z"
        const date = new Date(whenParam)
        setSelectedDate(date.toISOString().split('T')[0])
        setSelectedTime(date.toTimeString().slice(0, 5)) // HH:mm
      } catch (e) {
        console.error('Error parsing date:', e)
      }
    }

    if (whereParam) {
      setLocation(whereParam)
    }

    if (levelParam && levelParam !== '') {
      setSkillLevel(levelParam)
    }
  }, [searchParams])

  // Check authentication on mount
  useEffect(() => {
    const checkAuth = async () => {
      const { session } = await getSession()
      setIsAuthenticated(!!session)
    }
    checkAuth()
  }, [])

  // Validation
  const canSubmit =
    location.trim().length > 0 &&
    selectedDate &&
    selectedTime &&
    (isAuthenticated || guestName.trim().length > 0) // Nom requis si invité

  const handleCreateGame = async () => {
    if (!canSubmit) {
      toast({
        variant: "destructive",
        title: "Formulaire incomplet",
        description: "Veuillez remplir tous les champs requis",
      })
      return
    }

    setIsCreating(true)

    try {
      let tokenToUse: string | null = null

      // Si pas authentifié, créer un guest user d'abord
      if (!isAuthenticated) {
        console.log("Creating guest user:", { guestName, guestPhone, skillLevel })

        const { data: guestData, error: guestError } = await viboraApi.users.createGuestUser({
          name: guestName.trim(),
          skillLevel: parseInt(skillLevel),
          phoneNumber: guestPhone.trim() || undefined,
        })

        if (guestError) {
          toast({
            variant: "destructive",
            title: "Erreur",
            description: "Impossible de créer l'utilisateur invité",
          })
          return
        }

        console.log("Guest user created:", guestData)

        // Store the guest token for this request AND persist it
        if (guestData?.token) {
          tokenToUse = guestData.token
          setGuestToken(tokenToUse)

          // Persist guest auth for future requests
          setGuestAuth({
            externalId: guestData.externalId,
            name: guestData.name,
            skillLevel: guestData.skillLevel,
            token: guestData.token,
          })

          console.log("Guest token stored and persisted:", tokenToUse)
        }
      }

      // Combine date + time in ISO format
      const dateTimeISO = `${selectedDate}T${selectedTime}:00.000Z`

      const { data, error } = await viboraApi.games.createGame(
        {
          dateTime: dateTimeISO,
          location: location.trim(),
          skillLevel: parseInt(skillLevel) || null,
          maxPlayers: parseInt(maxPlayers),
          latitude: coordinates?.lat || null,
          longitude: coordinates?.lng || null,
        },
        tokenToUse // Pass the guest token if available
      )

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message || "Impossible de créer la partie",
        })
        return
      }

      if (data) {
        toast({
          title: "🎉 Partie créée !",
          description: "Votre partie a été créée avec succès",
        })

        // Redirect to game detail
        router.push(`/games/${data.id}`)
        router.refresh()
      }
    } catch (err) {
      console.error("Create game error:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur inattendue s'est produite",
      })
    } finally {
      setIsCreating(false)
    }
  }

  // Get min date (today)
  const minDate = new Date().toISOString().split("T")[0]

  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center gap-3 h-14">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => router.back()}
              className="rounded-full hover:bg-muted/40 transition-all -ml-2"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <h1 className="text-base font-semibold tracking-tight">Créer une partie</h1>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <motion.div
          variants={FADE_IN_ANIMATION_VARIANTS}
          initial="hidden"
          animate="show"
          className="max-w-md mx-auto px-4"
        >
          <VForm>
                <VStack spacing="lg">
                  {/* Reusable form fields */}
                  <GameFormFields
                    selectedDate={selectedDate}
                    onDateChange={setSelectedDate}
                    minDate={minDate}
                    selectedTime={selectedTime}
                    onTimeChange={setSelectedTime}
                    multiSelect={false}
                    location={location}
                    onLocationChange={(value, coords) => {
                      setLocation(value)
                      setCoordinates(coords || null)
                    }}
                    coordinates={coordinates}
                    skillLevel={skillLevel}
                    onSkillLevelChange={setSkillLevel}
                    isAuthenticated={isAuthenticated}
                  />

                  {/* Divider */}
                  <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="border-t border-border/20" />

                  {/* Guest Info Section - Only if not authenticated */}
                  {!isAuthenticated && (
                    <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-3.5">
                      <h2 className="text-base font-semibold">Vos informations</h2>
                      <VStack spacing="md">
                        <VInput
                          label="Votre nom"
                          type="text"
                          value={guestName}
                          onChange={(e) => setGuestName(e.target.value)}
                          placeholder="Ex: Jean Dupont"
                          required
                        />

                        <VInput
                          label="Téléphone (optionnel)"
                          type="tel"
                          value={guestPhone}
                          onChange={(e) => setGuestPhone(e.target.value)}
                          placeholder="Ex: 06 12 34 56 78"
                          containerClassName="mb-0"
                        />
                        <p className="text-xs text-muted-foreground -mt-2">
                          Pour que les autres joueurs puissent vous contacter
                        </p>
                      </VStack>
                    </motion.div>
                  )}

                  {/* Divider */}
                  <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="border-t border-border/20" />

                  {/* Max Players Section */}
                  <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-3.5">
                    <h2 className="text-base font-semibold">Nombre de joueurs</h2>
                    <div className="grid grid-cols-2 gap-3">
                      {["2", "4"].map((count) => (
                        <VButton
                          key={count}
                          type="button"
                          variant={maxPlayers === count ? "primary" : "outline"}
                          onClick={() => setMaxPlayers(count)}
                          size="lg"
                        >
                          <Users className="w-4 h-4 mr-2" />
                          {count} joueurs
                        </VButton>
                      ))}
                    </div>
                  </motion.div>

                  {/* Submit Button */}
                  <VButton
                    onClick={handleCreateGame}
                    disabled={!canSubmit || isCreating}
                    size="form"
                    variant="primary"
                    loading={isCreating}
                  >
                    {isCreating ? "Création en cours..." : "Créer la partie"}
                  </VButton>
                </VStack>
              </VForm>
        </motion.div>
      </VMain>
    </VPage>
  )
}
