"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { MapPin, Users, Calendar, Search, Bell, AlertCircle, Plus, ChevronRight, Clock } from "lucide-react"
import { motion } from "framer-motion"
import { MobileNav } from "@/components/mobile-nav"
import { ThemeToggle } from "@/components/ui/theme-toggle"
import { HomePageSkeleton } from "@/components/home-skeleton"
import { UserAvatar } from "@/components/user-avatar"
import { viboraApi } from "@/lib/api/vibora-client"
import { format, parseISO } from "date-fns"
import { fr } from "date-fns/locale"
import { useToast } from "@/components/ui/use-toast"
import { useUserProfile, useMyGames, useAvailableGames } from "@/lib/hooks/use-game-data"
import { getSession } from "@/lib/auth/supabase-auth"
import { isGuestMode } from "@/lib/auth/guest-auth"

import { useState, useEffect, useMemo } from "react"

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
    return format(date, "d MMM", { locale: fr })
  }
}

// Helper: Format time HH:mm
const formatTime = (dateStr: string) => {
  const date = parseISO(dateStr)
  return format(date, "HH:mm")
}

// Animation variants for Framer Motion
const FADE_IN_ANIMATION_VARIANTS = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0, transition: { type: "spring" as const } },
}

const STAGGER_CONTAINER_VARIANTS = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2,
    },
  },
}

const SLIDE_UP_VARIANTS = {
  hidden: { opacity: 0, y: 20, scale: 0.95 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      type: "spring" as const,
      stiffness: 300,
      damping: 30,
    },
  },
}

// Skeleton Components
const SkeletonCard = ({ className = "", children }: { className?: string; children?: React.ReactNode }) => (
  <div className={`animate-pulse bg-muted/50 rounded-lg ${className}`}>
    {children}
  </div>
)

const SkeletonAvatar = ({ size = "w-10 h-10" }: { size?: string }) => (
  <div className={`${size} bg-muted/60 rounded-full animate-pulse`} />
)

const SkeletonText = ({ width = "w-full", height = "h-4" }: { width?: string; height?: string }) => (
  <div className={`${width} ${height} bg-muted/60 rounded animate-pulse`} />
)

export default function Home() {
  const { toast } = useToast()
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isMounted, setIsMounted] = useState(false)

  // Use offline-first hooks
  const { data: currentUser, isOffline: isProfileOffline } = useUserProfile()
  const { data: myGamesData, isLoading: isLoadingMyGames, isOffline: isMyGamesOffline } = useMyGames()
  
  // Memoize my games IDs to prevent infinite re-renders
  const myGamesIds = useMemo(() => myGamesData?.map(g => g.id) || [], [myGamesData])
  
  const { data: availableGamesData, isLoading: isLoadingAvailable, isOffline: isAvailableOffline, refresh: refreshAvailable } = useAvailableGames(myGamesIds)
  
  // Combine offline states
  const isOffline = isProfileOffline || isMyGamesOffline || isAvailableOffline
  const isLoading = isLoadingMyGames || isLoadingAvailable
  
  // Prepare data
  const myGames = myGamesData?.slice(0, 5) || []
  const availableGames = availableGamesData || []

  // Check authentication
  useEffect(() => {
    const checkAuth = async () => {
      const { session } = await getSession()
      const isGuest = isGuestMode()
      setIsAuthenticated(!!session || isGuest)
    }
    checkAuth()
  }, [])

  // Mount state
  useEffect(() => {
    setIsMounted(true)
  }, [])

  const handleJoinGame = async (gameId: string) => {
    if (!isAuthenticated || !currentUser) {
      toast({
        title: "Authentification requise",
        description: "Connectez-vous pour rejoindre une partie",
        variant: "destructive",
      })
      return
    }

    try {
      const { error } = await viboraApi.games.joinGame(gameId, {
        userName: currentUser.displayName || currentUser.firstName ,
        userSkillLevel: currentUser.skillLevel?.toString(),
      })
      
      if (error) {
        toast({
          title: "Erreur",
          description: error.message,
          variant: "destructive",
        })
      } else {
        toast({
          title: "Succès !",
          description: "Vous avez rejoint la partie",
        })
        // Refresh available games
        refreshAvailable()
      }
    } catch (err) {
      toast({
        title: "Erreur",
        description: "Une erreur est survenue",
        variant: "destructive",
      })
    }
  }

  // Show skeleton while loading or not mounted
  if (!isMounted || isLoading) {
    return <HomePageSkeleton />
  }

  return (
    <motion.div 
      className="min-h-screen bg-background text-foreground pb-32"
      initial="hidden"
      animate="show"
      variants={STAGGER_CONTAINER_VARIANTS}
    >
      {/* Header - Modern & Clean */}
      <motion.header 
        className="sticky top-0 z-40 border-b border-border/40 bg-background/95 backdrop-blur-md supports-[backdrop-filter]:bg-background/80"
        variants={SLIDE_UP_VARIANTS}
      >
        <div className="container flex items-center justify-between h-16">
          <div className="flex items-center gap-3">
            <motion.div
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              transition={{ type: "spring", stiffness: 400, damping: 25 }}
            >
              {isAuthenticated ? (
                <Link href="/settings/profile">
                  <UserAvatar user={currentUser} size="md" className="ring-2 ring-background shadow-sm hover:ring-primary/20 transition-all" />
                </Link>
              ) : (
                <Link href="/auth/login">
                  <Button variant="ghost" size="sm" className="font-semibold">
                    Connexion
                  </Button>
                </Link>
              )}
            </motion.div>
            {isOffline && (
              <Badge variant="outline" className="text-xs text-muted-foreground border-muted-foreground/30">
                Mode hors ligne
              </Badge>
            )}
          </div>
          <motion.div 
            className="flex items-center gap-1"
            variants={SLIDE_UP_VARIANTS}
          >
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
              <ThemeToggle />
            </motion.div>
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
              <Button variant="ghost" size="icon" className="rounded-full hover:bg-muted/60 transition-colors">
                <Search className="w-5 h-5" />
              </Button>
            </motion.div>
            {isAuthenticated && (
              <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                <Button variant="ghost" size="icon" className="relative rounded-full hover:bg-muted/60 transition-colors">
                  <Bell className="w-5 h-5" />
                  <span className="absolute top-2 right-2 w-2 h-2 bg-primary rounded-full" />
                </Button>
              </motion.div>
            )}
          </motion.div>
        </div>
      </motion.header>

      <main className="pb-6">
        <div className="container max-w-2xl">
          <motion.div
            variants={STAGGER_CONTAINER_VARIANTS}
            className="space-y-8 pt-6"
          >
            {/* Hero - Modern Typography */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-2">
              <h1 className="text-4xl font-extrabold tracking-tight leading-tight">
                Bonjour{currentUser?.displayName ? `, ${currentUser.displayName.split(' ')[0]}` : ''}
              </h1>
              <p className="text-xl text-muted-foreground font-medium">
                Prêt à jouer ?
              </p>
            </motion.div>

          {/* Upcoming Games Section - Show if authenticated (includes guests) */}
          {isAuthenticated && myGames.length > 0 && (
            <motion.section variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-bold">Mes parties</h2>
                <Link href="/my-games" className="text-sm font-semibold text-primary hover:underline flex items-center gap-1">
                  Voir tout
                  <ChevronRight className="w-4 h-4" />
                </Link>
              </div>
              <div className="space-y-2.5">
                  {myGames.slice(0, 3).map((game, index) => (
                    <motion.div
                      key={game.id}
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: index * 0.05 }}
                    >
                      <Link href={`/games/${game.id}`}>
                        <motion.div
                          whileHover={{ y: -1, scale: 1.005 }}
                          transition={{ type: "spring", stiffness: 400, damping: 30 }}
                          className="group block p-4 rounded-2xl border border-border/40 hover:border-border/60 bg-card/50 hover:bg-card transition-all duration-200 hover:shadow-sm"
                        >
                          {/* Date & Time - Compact */}
                          <div className="flex items-center gap-2 mb-2.5">
                            <time className="text-[11px] font-medium text-muted-foreground tracking-wide">
                              {format(parseISO(game.dateTime), 'EEE d MMM', { locale: fr }).replace('.', '')} • {formatTime(game.dateTime)}
                            </time>
                            {game.status === 'Open' && (
                              <div className="flex items-center gap-1 px-2 py-0.5 rounded-full bg-emerald-500/10">
                                <div className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                                <span className="text-[10px] font-semibold text-emerald-600 dark:text-emerald-400 uppercase tracking-wider">Ouvert</span>
                              </div>
                            )}
                          </div>

                          {/* Location & Players */}
                          <div className="flex items-center justify-between gap-3">
                            <h3 className="font-semibold text-[15px] text-foreground line-clamp-1 group-hover:text-primary transition-colors">
                              {game.location}
                            </h3>
                            <div className="flex items-center gap-1.5 text-[13px] text-muted-foreground shrink-0">
                              <Users className="w-3.5 h-3.5" strokeWidth={2.5} />
                              <span className="font-medium">{game.currentPlayers}/{game.maxPlayers}</span>
                            </div>
                          </div>
                        </motion.div>
                      </Link>
                    </motion.div>
                  ))}
              </div>
            </motion.section>
          )}

          {/* Available Games Section */}
          <motion.section variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold">Matches à rejoindre</h2>
              <Link href="/search" className="text-sm font-semibold text-primary hover:underline flex items-center gap-1">
                Voir tout
                <ChevronRight className="w-4 h-4" />
              </Link>
            </div>
            
            {(!availableGames || availableGames.length === 0) ? (
              <div className="py-8 text-center">
                <div className="space-y-2">
                  <Users className="w-8 h-8 text-muted-foreground/40 mx-auto" />
                  <p className="text-sm text-muted-foreground/60">
                    Aucune partie disponible pour le moment
                  </p>
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                {availableGames.slice(0, 5).map((game, index) => {
                  const spotsLeft = game.maxPlayers - game.currentPlayers
                  const host = game.participants?.find((p: any) => p.isHost)
                  
                  return (
                    <motion.div
                      key={game.id}
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: index * 0.05 }}
                    >
                      <Card className="group border border-border/50 hover:border-border hover:shadow-md transition-all duration-200 overflow-hidden">
                        <Link href={`/games/${game.id}`}>
                          <CardContent className="p-5 cursor-pointer">
                          <div className="flex items-start gap-4">
                            {/* Avatar */}
                            <Avatar className="w-12 h-12 border-2 border-background shadow-sm shrink-0">
                              <AvatarFallback className="bg-gradient-to-br from-primary/20 via-primary/10 to-primary/5 text-primary font-bold">
                                {host?.displayName?.[0] || game.hostDisplayName?.[0] || 'H'}
                              </AvatarFallback>
                            </Avatar>

                            {/* Info */}
                            <div className="flex-1 min-w-0">
                              <div className="flex items-start justify-between gap-2 mb-2">
                                <div>
                                  <h3 className="font-bold text-base group-hover:text-primary transition-colors">
                                    {host?.displayName || game.hostDisplayName}
                                  </h3>
                                  <p className="text-sm text-muted-foreground">organise une partie</p>
                                </div>
                                {game.skillLevel && (
                                  <div className="flex items-center justify-center w-10 h-10 text-sm font-bold rounded-xl bg-primary/10 text-primary border border-primary/20 shrink-0">
                                    {game.skillLevel}
                                  </div>
                                )}
                              </div>

                              {/* Details */}
                              <div className="space-y-2">
                                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                  <Clock className="w-4 h-4 shrink-0" />
                                  <span className="font-medium">
                                    {formatDateRelative(game.dateTime)}, {formatTime(game.dateTime)}
                                  </span>
                                </div>
                                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                  <MapPin className="w-4 h-4 shrink-0" />
                                  <span className="font-medium truncate">{game.location}</span>
                                </div>
                              </div>

                              {/* Footer */}
                              <div className="flex items-center justify-between mt-3 pt-3 border-t border-border/40">
                                <div className="flex items-center gap-2">
                                  <Badge variant="secondary" className="bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300 border-0 font-semibold text-xs px-2.5">
                                    {spotsLeft} place{spotsLeft > 1 ? 's' : ''}
                                  </Badge>
                                  <span className="text-xs text-muted-foreground">
                                    {game.currentPlayers}/{game.maxPlayers} joueurs
                                  </span>
                                </div>
                                <Button 
                                  size="sm"
                                  className="h-8 font-semibold"
                                  onClick={(e) => {
                                    e.preventDefault()
                                    handleJoinGame(game.id)
                                  }}
                                >
                                  Rejoindre
                                </Button>
                              </div>
                            </div>
                          </div>
                        </CardContent>
                        </Link>
                      </Card>
                    </motion.div>
                  )
                })}
              </div>
            )}
          </motion.section>
          </motion.div>
        </div>
      </main>
      <MobileNav />

    </motion.div>
  )
}
