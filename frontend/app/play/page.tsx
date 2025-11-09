"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { motion, AnimatePresence } from "framer-motion"
import {
  ArrowLeft,
  Calendar as CalendarIcon,
  Clock,
  MapPin,
  Users,
  Search,
  AlertCircle,
  CheckCircle2,
  AlertTriangle,
  Plus
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Alert, AlertTitle, AlertDescription } from "@/components/ui/alert"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import { GameMatchCard, type SearchCriteria } from "@/components/ui/game-match-card"
import {
  VInput,
  VForm,
  VFormSection,
  VLocationInput,
  VLevelSelector,
  VButton,
} from "@/components/ui/vibora-form"
import { type TimeSlot } from "@/components/forms/TimeSlotSelector"
import { GameFormFields } from "@/components/forms/GameFormFields"
import { viboraApi } from "@/lib/api/vibora-client"
import { getSession } from "@/lib/auth/supabase-auth"
import { useToast } from "@/components/ui/use-toast"
import { usePolling } from "@/hooks/use-polling"
import {
  FADE_IN_ANIMATION_VARIANTS,
  STAGGER_CONTAINER_VARIANTS,
  SLIDE_UP_VARIANTS
} from "@/lib/animation-variants"
import { format, parseISO } from "date-fns"
import { fr } from "date-fns/locale"
import type { SearchGamesResponse, GameMatchDto, UserProfileDto } from "@/lib/api/vibora-types"

// Helper: Format date relative (Aujourd'hui, Demain, date)
const formatDateRelative = (dateStr: string) => {
  const date = parseISO(dateStr)
  const now = new Date()
  const tomorrow = new Date(now)
  tomorrow.setDate(tomorrow.getDate() + 1)

  if (date.toDateString() === now.toDateString()) {
    return "Aujourd'hui"
  } else if (date.toDateString() === tomorrow.toDateString()) {
    return "Demain"
  } else {
    return format(date, "EEEE d MMMM", { locale: fr })
  }
}

// Helper: Format time HH:mm
const formatTime = (dateStr: string) => {
  const date = parseISO(dateStr)
  return format(date, "HH:mm")
}

// Helper: Calculate time difference
const getTimeDifference = (dateStr1: string, dateStr2: string): string => {
  const date1 = parseISO(dateStr1)
  const date2 = parseISO(dateStr2)
  const diffMinutes = Math.abs(date2.getTime() - date1.getTime()) / (1000 * 60)
  const diffHours = Math.floor(diffMinutes / 60)

  if (diffHours === 0) return `${Math.floor(diffMinutes)} min`
  if (diffHours < 24) return `${diffHours}h`
  return `${Math.floor(diffHours / 24)}j`
}

// Types
type SearchState = "idle" | "searching" | "perfect" | "partial" | "empty"

export default function PlayPage() {
  const router = useRouter()
  const { toast } = useToast()

  // Auth state
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [currentUser, setCurrentUser] = useState<UserProfileDto | null>(null)

  // Form state
  const [selectedDate, setSelectedDate] = useState(
    new Date().toISOString().split("T")[0]
  )
  const [selectedTimeSlots, setSelectedTimeSlots] = useState<TimeSlot[]>(["evening"]) // Default to evening
  const [selectedTime, setSelectedTime] = useState("20:00") // Default evening time
  const [location, setLocation] = useState("")
  const [skillLevel, setSkillLevel] = useState("5")
  const [coordinates, setCoordinates] = useState<{ lat: number; lng: number } | null>(null)
  const [radius, setRadius] = useState(10) // Default 10km

  // Search state
  const [searchState, setSearchState] = useState<SearchState>("idle")
  const [searchResults, setSearchResults] = useState<SearchGamesResponse | null>(null)
  const [searchCriteria, setSearchCriteria] = useState<SearchCriteria | null>(null)

  // Guest join dialog state
  const [guestDialogOpen, setGuestDialogOpen] = useState(false)
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null)
  const [guestName, setGuestName] = useState("")
  const [guestPhone, setGuestPhone] = useState("")
  const [isJoining, setIsJoining] = useState(false)

  // Check authentication on mount
  useEffect(() => {
    const checkAuth = async () => {
      const { session } = await getSession()
      setIsAuthenticated(!!session)

      if (session) {
        const { data: profileData } = await viboraApi.users.getCurrentUserProfile()
        if (profileData) {
          setCurrentUser(profileData)
          // Pre-fill skill level if user is authenticated
          if (profileData.skillLevel) {
            setSkillLevel(profileData.skillLevel.toString())
          }
        }
      }
    }
    checkAuth()
  }, [])

  // Handle time slots change (multi-select)
  const handleTimeSlotsChange = (slots: TimeSlot[]) => {
    setSelectedTimeSlots(slots)
    // Use the first slot's default time or keep current if no slots selected
    if (slots.length > 0) {
      const firstSlot = ["morning", "afternoon", "evening"].find(s => slots.includes(s as TimeSlot))
      if (firstSlot === "morning") setSelectedTime("10:00")
      else if (firstSlot === "afternoon") setSelectedTime("15:00")
      else if (firstSlot === "evening") setSelectedTime("20:00")
    }
  }

  // Validation
  const canSubmit =
    location.trim().length > 0 &&
    selectedDate &&
    selectedTimeSlots.length > 0

  // Refresh results (silent update for polling)
  const refreshResults = async () => {
    if (!searchCriteria) return

    try {
      const { data, error } = await viboraApi.games.searchGames({
        when: searchCriteria.when,
        where: searchCriteria.where,
        skillLevel: searchCriteria.level || null,
        latitude: coordinates?.lat || null,
        longitude: coordinates?.lng || null,
        radiusKm: coordinates ? radius : undefined,
      })

      if (!error && data) {
        setSearchResults(data)

        // Update state based on new results
        if (data.perfectMatches.length > 0) {
          setSearchState("perfect")
        } else if (data.partialMatches.length > 0) {
          setSearchState("partial")
        } else {
          setSearchState("empty")
        }
      }
    } catch (err) {
      console.error("Refresh error:", err)
      // Silent failure - don't show toast for background polling
    }
  }

  // Handle search
  const handleSearch = async () => {
    if (!canSubmit) {
      toast({
        variant: "destructive",
        title: "Formulaire incomplet",
        description: "Veuillez remplir tous les champs requis",
      })
      return
    }

    setSearchState("searching")

    try {
      // Combine date + time in ISO format
      const dateTimeISO = `${selectedDate}T${selectedTime}:00.000Z`

      const criteria: SearchCriteria = {
        when: dateTimeISO,
        where: location.trim(),
        level: parseInt(skillLevel) || undefined,
      }

      setSearchCriteria(criteria)

      const { data, error } = await viboraApi.games.searchGames({
        when: criteria.when,
        where: criteria.where,
        skillLevel: criteria.level || null,
        latitude: coordinates?.lat || null,
        longitude: coordinates?.lng || null,
        radiusKm: coordinates ? radius : undefined,
      })

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message || "Impossible de rechercher des parties",
        })
        setSearchState("idle")
        return
      }

      if (data) {
        setSearchResults(data)

        // Determine state based on results
        if (data.perfectMatches.length > 0) {
          setSearchState("perfect")
        } else if (data.partialMatches.length > 0) {
          setSearchState("partial")
        } else {
          setSearchState("empty")
        }
      }
    } catch (err) {
      console.error("Search error:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur inattendue s'est produite",
      })
      setSearchState("idle")
    }
  }

  // Polling: Refresh results every 20 seconds when viewing results
  usePolling(
    refreshResults,
    20000, // 20 seconds
    searchState !== "idle" && searchState !== "searching" // Only poll when showing results
  )

  // Handle join game
  const handleJoinGame = async (gameId: string) => {
    if (!isAuthenticated) {
      // Open guest dialog instead of redirecting
      setSelectedGameId(gameId)
      setGuestDialogOpen(true)
      return
    }

    // Authenticated user flow
    try {
      const { error } = await viboraApi.games.joinGame(gameId, {
        userName: currentUser?.displayName || "",
        userSkillLevel: skillLevel,
      })

      if (error) {
        toast({
          title: "Erreur",
          description: error.message,
          variant: "destructive",
        })
      } else {
        toast({
          title: "Vous avez rejoint la partie !",
          description: "Rendez-vous dans 'Mes parties' pour voir les détails",
        })
        // Redirect to game details
        router.push(`/games/${gameId}`)
      }
    } catch (err) {
      toast({
        title: "Erreur",
        description: "Une erreur est survenue",
        variant: "destructive",
      })
    }
  }

  // Handle join as guest
  const handleJoinAsGuest = async () => {
    if (!selectedGameId || !guestName.trim()) {
      toast({
        title: "Nom requis",
        description: "Veuillez entrer votre nom",
        variant: "destructive",
      })
      return
    }

    setIsJoining(true)

    try {
      const { error } = await viboraApi.games.joinGameAsGuest(selectedGameId, {
        name: guestName.trim(),
        phoneNumber: guestPhone.trim() || null,
      })

      if (error) {
        toast({
          title: "Erreur",
          description: error.message,
          variant: "destructive",
        })
      } else {
        toast({
          title: "Vous avez rejoint la partie !",
          description: "Consultez vos emails pour plus de détails",
        })

        // Close dialog and reset
        setGuestDialogOpen(false)
        setGuestName("")
        setGuestPhone("")
        setSelectedGameId(null)

        // Redirect to game details
        router.push(`/games/${selectedGameId}`)
      }
    } catch (err) {
      toast({
        title: "Erreur",
        description: "Une erreur est survenue",
        variant: "destructive",
      })
    } finally {
      setIsJoining(false)
    }
  }

  // Handle create game (prefill form)
  const handleCreateGame = () => {
    if (searchCriteria) {
      const queryParams = new URLSearchParams({
        when: searchCriteria.when,
        where: searchCriteria.where,
        level: searchCriteria.level?.toString() || "",
      })
      router.push(`/create-game?${queryParams.toString()}`)
    } else {
      router.push("/create-game")
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
            <h1 className="text-base font-semibold tracking-tight">Jouer</h1>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        {/* Search Criteria Summary - Ultra minimalist */}
        {(searchState === "perfect" || searchState === "partial" || searchState === "empty") && searchCriteria && (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            className="border-b border-border/20"
          >
            <VContainer>
              <div className="py-3 space-y-2.5">
                {/* Chips row */}
                <div className="flex items-center gap-2 flex-wrap text-[11px] text-muted-foreground">
                  {/* Date */}
                  <div className="flex items-center gap-1 px-2.5 py-1.5 rounded-full bg-muted/30">
                    <CalendarIcon className="h-3 w-3" />
                    <span className="font-medium">{searchCriteria.when && formatDateRelative(searchCriteria.when)}</span>
                  </div>

                  {/* Time */}
                  <div className="flex items-center gap-1 px-2.5 py-1.5 rounded-full bg-muted/30">
                    <Clock className="h-3 w-3" />
                    <span className="font-medium">{searchCriteria.when && formatTime(searchCriteria.when)}</span>
                  </div>

                  {/* Location */}
                  <div className="flex items-center gap-1 px-2.5 py-1.5 rounded-full bg-muted/30">
                    <MapPin className="h-3 w-3" />
                    <span className="truncate max-w-[100px] font-medium">{searchCriteria.where}</span>
                  </div>

                  {/* Skill Level */}
                  {searchCriteria.level && (
                    <div className="flex items-center gap-1 px-2.5 py-1.5 rounded-full bg-muted/30">
                      <Users className="h-3 w-3" />
                      <span className="font-medium">Niv. {searchCriteria.level}</span>
                    </div>
                  )}

                  {/* Radius */}
                  {coordinates && (
                    <div className="px-2.5 py-1.5 rounded-full bg-muted/30">
                      <span className="font-medium">{radius} km</span>
                    </div>
                  )}
                </div>

                {/* Modify button - Full width and centered */}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setSearchState("idle")}
                  className="h-8 w-full text-[11px] text-muted-foreground hover:text-foreground rounded-full font-medium"
                >
                  Modifier mes critères
                </Button>
              </div>
            </VContainer>
          </motion.div>
        )}

        <AnimatePresence mode="wait">
          {searchState === "idle" || searchState === "searching" ? (
            <motion.div
              key="form"
              variants={FADE_IN_ANIMATION_VARIANTS}
              initial="hidden"
              animate="show"
              exit={{ opacity: 0, y: -20 }}
              className="max-w-md mx-auto px-4"
            >
              <VForm>
                <GameFormFields
                  selectedDate={selectedDate}
                  onDateChange={setSelectedDate}
                  minDate={minDate}
                  selectedTimeSlots={selectedTimeSlots}
                  onTimeSlotsChange={handleTimeSlotsChange}
                  multiSelect={true}
                  location={location}
                  onLocationChange={(value, coords) => {
                    setLocation(value)
                    if (coords) setCoordinates(coords)
                  }}
                  coordinates={coordinates}
                  radius={radius}
                  onRadiusChange={setRadius}
                  showRadius={!!coordinates}
                  skillLevel={skillLevel}
                  onSkillLevelChange={setSkillLevel}
                  isAuthenticated={isAuthenticated}
                  currentUserSkillLevel={currentUser?.skillLevel}
                />

                {/* Submit Button */}
                <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="pt-2">
                  <Button
                    onClick={handleSearch}
                    disabled={!canSubmit || searchState === "searching"}
                    size="lg"
                    className="w-full h-12 font-medium text-sm tracking-tight shadow-sm hover:shadow-md transition-all rounded-xl disabled:opacity-50"
                  >
                    {searchState === "searching" ? (
                      <>
                        <motion.div
                          animate={{ rotate: 360 }}
                          transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
                          className="w-4 h-4 border-2 border-primary-foreground border-t-transparent rounded-full mr-2"
                        />
                        Recherche en cours...
                      </>
                    ) : (
                      <>
                        <Search className="w-4 h-4 mr-2" strokeWidth={2} />
                        Rechercher des parties
                      </>
                    )}
                  </Button>
                </motion.div>
              </VForm>
            </motion.div>
          ) : null}

          {/* PERFECT MATCHES STATE */}
          {searchState === "perfect" && searchResults && (
            <motion.div
              key="perfect"
              variants={STAGGER_CONTAINER_VARIANTS}
              initial="hidden"
              animate="show"
              exit={{ opacity: 0 }}
              className="max-w-2xl mx-auto"
            >
              <VStack spacing="lg">
                {/* Success Header - Subtle & Elegant */}
                <motion.div 
                  variants={FADE_IN_ANIMATION_VARIANTS}
                  className="text-center space-y-2 px-4"
                >
                  <div className="inline-flex items-center gap-2 text-sm font-medium text-emerald-600 dark:text-emerald-400">
                    <div className="w-1.5 h-1.5 rounded-full bg-emerald-600 dark:bg-emerald-400 animate-pulse" />
                    {searchResults.perfectMatches.length} {searchResults.perfectMatches.length > 1 ? "parties trouvées" : "partie trouvée"}
                  </div>
                  <p className="text-sm text-muted-foreground">
                    Ces parties correspondent parfaitement à vos critères
                  </p>
                </motion.div>

                {/* Perfect Matches List */}
                <VStack spacing="md">
                  {searchResults.perfectMatches.map((game, index) => (
                    <motion.div
                      key={game.id}
                      variants={FADE_IN_ANIMATION_VARIANTS}
                      transition={{ delay: index * 0.1 }}
                    >
                      <GameMatchCard
                        game={game}
                        isPerfectMatch
                        onJoin={() => handleJoinGame(game.id)}
                        searchCriteria={searchCriteria}
                      />
                    </motion.div>
                  ))}
                </VStack>

                {/* Secondary Action - Low visual weight */}
                <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="pt-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleCreateGame}
                    className="w-full text-muted-foreground hover:text-foreground hover:bg-muted/30 transition-colors"
                  >
                    <span className="text-sm">Ou créer ma propre partie</span>
                  </Button>
                </motion.div>
              </VStack>
            </motion.div>
          )}

          {/* PARTIAL MATCHES STATE */}
          {searchState === "partial" && searchResults && (
            <motion.div
              key="partial"
              variants={STAGGER_CONTAINER_VARIANTS}
              initial="hidden"
              animate="show"
              exit={{ opacity: 0 }}
              className="max-w-2xl mx-auto"
            >
              <VStack spacing="lg">
                {/* Info Message - Elegant & Informative */}
                <motion.div 
                  variants={FADE_IN_ANIMATION_VARIANTS}
                  className="bg-gradient-to-r from-slate-50 to-slate-100/50 dark:from-slate-900/40 dark:to-slate-800/20 border border-slate-200 dark:border-slate-700 rounded-2xl p-5 shadow-sm"
                >
                  <div className="flex items-start gap-3">
                    <div className="w-10 h-10 rounded-full bg-slate-200/60 dark:bg-slate-700/60 flex items-center justify-center shrink-0 mt-0.5">
                      <Clock className="w-5 h-5 text-slate-600 dark:text-slate-300" />
                    </div>
                    <div className="flex-1 space-y-1">
                      <h3 className="font-semibold text-base">
                        Aucune partie exactement à {formatTime(searchCriteria?.when || "")}
                      </h3>
                      <p className="text-sm text-muted-foreground">
                        Vous pouvez créer votre partie ou rejoindre une partie similaire
                      </p>
                    </div>
                  </div>
                </motion.div>

                {/* Primary CTA - Visible mais pas agressif */}
                <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
                  <Button
                    onClick={handleCreateGame}
                    size="lg"
                    className="w-full h-12 font-semibold shadow-sm hover:shadow-md transition-all"
                  >
                    Créer ma partie
                  </Button>
                </motion.div>

                {/* Section Divider with softer visual weight */}
                <motion.div 
                  variants={FADE_IN_ANIMATION_VARIANTS}
                  className="relative py-3"
                >
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-border/40" />
                  </div>
                  <div className="relative flex justify-center">
                    <span className="bg-background px-3 text-[10px] uppercase tracking-wide text-muted-foreground/50 font-normal">
                      Alternatives disponibles
                    </span>
                  </div>
                </motion.div>

                {/* Partial Matches List - Secondary Actions */}
                <VStack spacing="md">
                  {searchResults.partialMatches.map((game, index) => (
                    <motion.div
                      key={game.id}
                      variants={FADE_IN_ANIMATION_VARIANTS}
                      transition={{ delay: index * 0.08 }}
                    >
                      <GameMatchCard
                        game={game}
                        isPerfectMatch={false}
                        onJoin={() => handleJoinGame(game.id)}
                        searchCriteria={searchCriteria}
                        isSecondaryAction
                      />
                    </motion.div>
                  ))}
                </VStack>
              </VStack>
            </motion.div>
          )}

          {/* NO MATCHES STATE - ULTRA MODERN */}
          {searchState === "empty" && searchResults && (
            <motion.div
              key="empty"
              variants={STAGGER_CONTAINER_VARIANTS}
              initial="hidden"
              animate="show"
              exit={{ opacity: 0 }}
              className="max-w-md mx-auto px-4"
            >
              <VStack spacing="lg">
                {/* Empty State - Ultra minimal et moderne */}
                <motion.div
                  variants={FADE_IN_ANIMATION_VARIANTS}
                  className="flex flex-col items-center text-center pt-16 pb-10"
                >
                  {/* Icon - Abstract & Modern */}
                  <motion.div
                    initial={{ scale: 0.9, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ delay: 0.1, type: "spring", stiffness: 180, damping: 15 }}
                    className="relative mb-6"
                  >
                    {/* Animated circles representing "no games found" */}
                    <div className="relative w-24 h-24 flex items-center justify-center">
                      {/* Outer ring */}
                      <motion.div
                        initial={{ scale: 1, opacity: 0.3 }}
                        animate={{ scale: [1, 1.1, 1], opacity: [0.3, 0.15, 0.3] }}
                        transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
                        className="absolute inset-0 rounded-full border-2 border-muted-foreground/20"
                      />
                      
                      {/* Middle ring */}
                      <motion.div
                        initial={{ scale: 0.7, opacity: 0.4 }}
                        animate={{ scale: [0.7, 0.8, 0.7], opacity: [0.4, 0.2, 0.4] }}
                        transition={{ duration: 3, repeat: Infinity, ease: "easeInOut", delay: 0.5 }}
                        className="absolute inset-4 rounded-full border-2 border-muted-foreground/25"
                      />
                      
                      {/* Inner circle with plus */}
                      <motion.div
                        initial={{ scale: 0.5 }}
                        animate={{ scale: 1 }}
                        transition={{ delay: 0.15, type: "spring", stiffness: 200, damping: 20 }}
                        className="relative w-12 h-12 rounded-full bg-gradient-to-br from-muted/60 to-muted/30 border border-border/50 flex items-center justify-center shadow-sm"
                      >
                        <Plus className="w-6 h-6 text-muted-foreground/70" strokeWidth={2} />
                      </motion.div>
                    </div>
                  </motion.div>

                  {/* Title - Bold & Clear */}
                  <motion.h3
                    variants={FADE_IN_ANIMATION_VARIANTS}
                    className="text-xl font-semibold tracking-tight mb-3"
                  >
                    Soyez le premier à organiser une partie
                  </motion.h3>

                  {/* Description - Subtle */}
                  <motion.p
                    variants={FADE_IN_ANIMATION_VARIANTS}
                    className="text-[13px] text-muted-foreground/70 leading-relaxed max-w-xs"
                  >
                    Nous n'avons trouvé aucune partie correspondant à vos critères pour le{" "}
                    <span className="text-foreground/90 font-medium">
                      {searchCriteria?.when && formatDateRelative(searchCriteria.when)}
                    </span>
                    {" "}à{" "}
                    <span className="text-foreground/90 font-medium">
                      {searchCriteria?.where}
                    </span>
                  </motion.p>
                </motion.div>

                {/* Primary CTA - Ultra clean */}
                <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
                  <Button
                    onClick={handleCreateGame}
                    size="lg"
                    className="w-full h-12 font-medium text-sm tracking-tight shadow-sm hover:shadow-md transition-all rounded-xl"
                  >
                    <Plus className="w-4 h-4 mr-2" strokeWidth={2} />
                    Créer ma partie
                  </Button>
                </motion.div>

                {/* Secondary CTA - Ghost style */}
                <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
                  <Button
                    onClick={() => setSearchState("idle")}
                    variant="ghost"
                    size="default"
                    className="w-full h-11 font-normal text-[13px] text-muted-foreground hover:text-foreground rounded-xl"
                  >
                    Modifier mes critères
                  </Button>
                </motion.div>

                {/* Alternative Matches - Only if available */}
                {searchResults.partialMatches && searchResults.partialMatches.length > 0 && (
                  <>
                    <motion.div
                      variants={FADE_IN_ANIMATION_VARIANTS}
                      className="relative py-6"
                    >
                      <div className="absolute inset-0 flex items-center">
                        <div className="w-full border-t border-border/20" />
                      </div>
                      <div className="relative flex justify-center">
                        <span className="bg-background px-4 text-[10px] uppercase tracking-widest text-muted-foreground/40 font-medium">
                          Alternatives
                        </span>
                      </div>
                    </motion.div>

                    <VStack spacing="md">
                      {searchResults.partialMatches.map((game, index) => (
                        <motion.div
                          key={game.id}
                          variants={FADE_IN_ANIMATION_VARIANTS}
                          transition={{ delay: index * 0.08 }}
                        >
                          <GameMatchCard
                            game={game}
                            isPerfectMatch={false}
                            onJoin={() => handleJoinGame(game.id)}
                            searchCriteria={searchCriteria}
                            isSecondaryAction
                          />
                        </motion.div>
                      ))}
                    </VStack>
                  </>
                )}
              </VStack>
            </motion.div>
          )}
        </AnimatePresence>
      </VMain>

      {/* Guest Join Dialog */}
      <Dialog open={guestDialogOpen} onOpenChange={setGuestDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Rejoindre cette partie</DialogTitle>
            <DialogDescription>
              Entrez vos informations pour participer. Vous pourrez créer un compte après la partie.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label htmlFor="guest-name" className="text-sm font-medium">
                Votre nom <span className="text-destructive">*</span>
              </label>
              <input
                id="guest-name"
                type="text"
                value={guestName}
                onChange={(e) => setGuestName(e.target.value)}
                placeholder="Ex: Jean Dupont"
                className="w-full px-3 py-2 rounded-lg border border-border bg-background"
                disabled={isJoining}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="guest-phone" className="text-sm font-medium">
                Téléphone (optionnel)
              </label>
              <input
                id="guest-phone"
                type="tel"
                value={guestPhone}
                onChange={(e) => setGuestPhone(e.target.value)}
                placeholder="Ex: 06 12 34 56 78"
                className="w-full px-3 py-2 rounded-lg border border-border bg-background"
                disabled={isJoining}
              />
              <p className="text-xs text-muted-foreground">
                Pour que l'organisateur puisse vous contacter
              </p>
            </div>
          </div>

          <DialogFooter className="flex-col gap-3 sm:flex-col">
            <Button
              onClick={handleJoinAsGuest}
              disabled={!guestName.trim() || isJoining}
              className="w-full"
            >
              {isJoining ? "Rejoindre..." : "Rejoindre la partie"}
            </Button>

            <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
              <span>Vous avez déjà un compte ?</span>
              <Button
                variant="link"
                className="h-auto p-0 text-sm font-semibold text-primary"
                onClick={() => {
                  setGuestDialogOpen(false)
                  router.push("/auth/login")
                }}
                disabled={isJoining}
              >
                Se connecter
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </VPage>
  )
}

