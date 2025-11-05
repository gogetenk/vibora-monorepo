"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { MapPin, Users, Calendar, Search, Bell } from "lucide-react"
import { motion } from "framer-motion"
import { MobileNav } from "@/components/mobile-nav"
import { HomePageSkeleton } from "@/components/home-skeleton"
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

const FADE_IN_ANIMATION_VARIANTS = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0, transition: { type: "spring" as const } },
}

export default function Home() {
  const { toast } = useToast()
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isMounted, setIsMounted] = useState(false)

  // Use offline-first hooks
  const { data: currentUser } = useUserProfile()
  const { data: myGamesData, isLoading: isLoadingMyGames } = useMyGames()
  
  // Memoize my games IDs to prevent infinite re-renders
  const myGamesIds = useMemo(() => myGamesData?.map(g => g.id) || [], [myGamesData])
  
  const { data: availableGamesData, isLoading: isLoadingAvailable, refresh: refreshAvailable } = useAvailableGames(myGamesIds)
  
  const isLoading = isLoadingMyGames || isLoadingAvailable
  
  // Prepare data
  const myGames = myGamesData?.slice(0, 3) || []
  const availableGames = availableGamesData?.slice(0, 5) || []

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
        userName: currentUser.displayName || currentUser.firstName,
        userSkillLevel: currentUser.skillLevel ? currentUser.skillLevel.toString() : "",
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
    <div className="min-h-screen bg-background text-foreground pb-32">
      {/* Header - Mockup Style */}
      <header className="sticky top-0 z-40 bg-background/80 backdrop-blur-lg">
        <div className="container flex items-center justify-between h-20">
          <Link href="/settings/profile">
            <Avatar className="w-10 h-10 border-2 border-muted">
              <AvatarFallback>{currentUser?.displayName?.[0] || 'U'}</AvatarFallback>
            </Avatar>
          </Link>
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" className="rounded-full">
              <Search className="w-5 h-5 text-muted-foreground" />
            </Button>
            <Button variant="ghost" size="icon" className="relative rounded-full">
              <Bell className="w-5 h-5 text-muted-foreground" />
              <span className="absolute flex w-2.5 h-2.5 top-2 right-2">
                <span className="absolute inline-flex w-full h-full rounded-full opacity-75 animate-ping bg-primary"></span>
                <span className="relative inline-flex w-2.5 h-2.5 rounded-full bg-primary"></span>
              </span>
            </Button>
          </div>
        </div>
      </header>

      <main className="container">
        <motion.div
          initial="hidden"
          animate="show"
          viewport={{ once: true }}
          variants={{
            hidden: {},
            show: {
              transition: {
                staggerChildren: 0.15,
              },
            },
          }}
        >
          {/* Hero - Mockup Style */}
          <motion.h1 variants={FADE_IN_ANIMATION_VARIANTS} className="text-3xl font-bold tracking-tight mt-6">
            Bonjour,
            <br />
            <span className="text-muted-foreground">prêt à jouer ?</span>
          </motion.h1>

          {/* Upcoming Games Section - Horizontal Scroll with Photos */}
          {isAuthenticated && myGames.length > 0 && (
            <motion.section variants={FADE_IN_ANIMATION_VARIANTS} className="mt-8">
              <div className="flex items-center justify-between mb-4">
                <h2 className="font-semibold">Prochaines parties</h2>
                <Link href="/my-games" className="text-sm font-medium text-primary">
                  Voir tout
                </Link>
              </div>
              <div className="relative">
                <div className="flex pb-4 -mx-4 space-x-4 overflow-x-auto px-4 scrollbar-hide">
                  {myGames.map((game, index) => {
                    const spotsLeft = game.maxPlayers - game.currentPlayers
                    const host = game.participants?.find((p: any) => p.isHost)
                    
                    return (
                      <Link key={game.id} href={`/games/${game.id}`} className="flex-shrink-0 w-[280px]">
                        <Card className="overflow-hidden transition-all border-none shadow-lg bg-secondary/40 hover:shadow-primary/10 active:scale-[0.98]">
                          <div className="relative h-[150px] w-full bg-gradient-to-br from-primary/10 to-primary/5">
                            {/* Placeholder for court image - will be added later */}
                            <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent"></div>
                            <div className="absolute p-3 bottom-1">
                              <h3 className="font-bold text-white">{game.location}</h3>
                              <p className="text-sm text-white/80">Padel {game.maxPlayers} joueurs</p>
                            </div>
                          </div>
                          <CardContent className="p-3">
                            <div className="flex items-center justify-between">
                              <div className="flex items-center gap-2 text-sm">
                                <Calendar className="w-4 h-4 text-muted-foreground" />
                                <span>
                                  {formatDateRelative(game.dateTime)}, {formatTime(game.dateTime)}
                                </span>
                              </div>
                              <div className="flex -space-x-2">
                                {game.participants?.slice(0, 3).map((player: any, i: number) => (
                                  <Avatar key={i} className="w-6 h-6 border-2 border-background">
                                    <AvatarFallback>{player.displayName?.[0] || 'P'}</AvatarFallback>
                                  </Avatar>
                                ))}
                                {spotsLeft > 0 && (
                                  <div className="flex items-center justify-center w-6 h-6 text-xs font-medium border-2 rounded-full bg-primary/20 text-primary border-background">
                                    +{spotsLeft}
                                  </div>
                                )}
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      </Link>
                    )
                  })}
                  {/* Create Game Card */}
                  <div className="flex-shrink-0 w-[280px]">
                    <Card className="flex flex-col items-center justify-center h-full overflow-hidden transition-all border-2 border-dashed shadow-none border-muted-foreground/20 bg-secondary/20 hover:border-primary active:scale-[0.98]">
                      <Link href="/create-game" className="text-center p-6">
                        <div className="flex items-center justify-center w-12 h-12 mb-2 rounded-full bg-primary/20 mx-auto">
                          <Users className="w-6 h-6 text-primary" />
                        </div>
                        <p className="font-semibold text-foreground">Créer une partie</p>
                        <p className="text-sm text-muted-foreground">et inviter des amis</p>
                      </Link>
                    </Card>
                  </div>
                </div>
              </div>
            </motion.section>
          )}

          {/* Available Games Section - Compact List */}
          <motion.section variants={FADE_IN_ANIMATION_VARIANTS} className="mt-8">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-semibold">Parties près de vous</h2>
              <Link href="/search" className="text-sm font-medium text-primary">
                Voir tout
              </Link>
            </div>
            {(!availableGames || availableGames.length === 0) ? (
              <div className="py-8 text-center">
                <Users className="w-8 h-8 text-muted-foreground/40 mx-auto mb-2" />
                <p className="text-sm text-muted-foreground/60">
                  Aucune partie disponible pour le moment
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 gap-3">
                {availableGames.map((game) => {
                  const spotsLeft = game.maxPlayers - game.currentPlayers
                  const time = formatTime(game.dateTime)
                  
                  return (
                    <Link key={game.id} href={`/games/${game.id}`}>
                      <Card className="overflow-hidden transition-all border-none shadow-md bg-secondary/40 hover:bg-secondary/60 active:scale-[0.99]">
                        <CardContent className="flex items-center gap-4 p-3">
                          {/* Time Badge */}
                          <div className="flex flex-col items-center justify-center w-12 h-12 rounded-lg bg-background">
                            <div className="text-lg font-bold leading-none">{time.split(":")[0]}</div>
                            <div className="text-xs text-muted-foreground">{time.split(":")[1]}</div>
                          </div>
                          {/* Info */}
                          <div className="flex-1">
                            <h3 className="font-semibold">{game.location}</h3>
                            <div className="flex items-center justify-between mt-1 text-sm text-muted-foreground">
                              <div className="flex items-center gap-1.5">
                                <MapPin className="w-4 h-4" />
                                <span>À proximité</span>
                              </div>
                              <div className="flex items-center gap-1.5">
                                <Users className="w-4 h-4" />
                                <span>
                                  {spotsLeft} place{spotsLeft > 1 ? "s" : ""}
                                </span>
                              </div>
                            </div>
                          </div>
                          {/* Skill Level Badge */}
                          {game.skillLevel && (
                            <div className="flex items-center justify-center w-10 h-10 text-sm font-bold rounded-full bg-primary/20 text-primary">
                              {game.skillLevel}
                            </div>
                          )}
                        </CardContent>
                      </Card>
                    </Link>
                  )
                })}
              </div>
            )}
          </motion.section>
        </motion.div>
      </main>
      <MobileNav />
      <style jsx global>{`
        .scrollbar-hide::-webkit-scrollbar {
          display: none;
        }
        .scrollbar-hide {
          -ms-overflow-style: none;
          scrollbar-width: none;
        }
      `}</style>
    </div>
  )
}
