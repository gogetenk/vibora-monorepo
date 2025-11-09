"use client"

import { useState, useMemo, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { Calendar, Clock, MapPin, Users, AlertCircle, Loader2, ChevronRight, ArrowLeft, Plus, WifiOff } from "lucide-react"
import { motion, AnimatePresence } from "framer-motion"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import { useMyGamesWithTabs } from "@/lib/hooks/use-game-data"
import { FADE_IN_ANIMATION_VARIANTS, STAGGER_CONTAINER_VARIANTS } from "@/lib/animation-variants"

export default function MyGames() {
  const router = useRouter()
  const [activeTab, setActiveTab] = useState("upcoming")
  const [isMounted, setIsMounted] = useState(false)
  
  // Use offline-first hook
  const { data: gamesData, isLoading, isOffline } = useMyGamesWithTabs()
  
  // Avoid hydration errors
  useEffect(() => {
    setIsMounted(true)
  }, [])
  
  // Map API data to component format
  const upcomingGames = useMemo(() => {
    if (!gamesData?.upcoming) return []
    return gamesData.upcoming.map((game) => ({
      id: game.id,
      dateTime: game.dateTime, // Keep original for parsing
      time: new Date(game.dateTime).toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" }),
      date: new Date(game.dateTime).toLocaleDateString("fr-FR", { day: "numeric", month: "long", year: "numeric" }),
      location: game.location,
      status: game.status === "Open" ? "confirmed" : "pending",
      currentPlayers: game.currentPlayers,
      maxPlayers: game.maxPlayers,
    }))
  }, [gamesData?.upcoming])
  
  const pastGames = useMemo(() => {
    if (!gamesData?.past) return []
    return gamesData.past.map((game) => ({
      id: game.id,
      dateTime: game.dateTime, // Keep original for parsing
      time: new Date(game.dateTime).toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" }),
      date: new Date(game.dateTime).toLocaleDateString("fr-FR", { day: "numeric", month: "long", year: "numeric" }),
      location: game.location,
      status: game.status === "Open" ? "confirmed" : "pending",
      currentPlayers: game.currentPlayers,
      maxPlayers: game.maxPlayers,
    }))
  }, [gamesData?.past])

  // Show loader while mounting or loading to avoid hydration errors
  if (!isMounted || isLoading) {
    return (
      <VPage>
        <VHeader>
          <VContainer>
            <div className="flex items-center gap-3 h-16">
              <Button variant="ghost" size="icon" onClick={() => router.back()}>
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold">Mes parties</h1>
            </div>
          </VContainer>
        </VHeader>
        <VMain className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </VMain>
      </VPage>
    )
  }

  const displayedGames = activeTab === "upcoming" ? upcomingGames : pastGames

  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-4">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => router.back()}
                className="rounded-full hover:bg-muted/60 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold">Mes parties</h1>
            </div>
            {isOffline && (
              <Badge variant="outline" className="text-xs text-muted-foreground border-muted-foreground/30 flex items-center gap-1.5">
                <WifiOff className="w-3 h-3" />
                Hors ligne
              </Badge>
            )}
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <motion.div
          variants={STAGGER_CONTAINER_VARIANTS}
          initial="hidden"
          animate="show"
          className="max-w-2xl mx-auto"
        >
          <VStack spacing="lg">
            {/* Onglets - Design moderne */}
            <Tabs defaultValue="upcoming" value={activeTab} onValueChange={setActiveTab} className="w-full">
              <TabsList className="grid w-full grid-cols-2 h-12 bg-muted/30 backdrop-blur-sm p-1 rounded-xl border border-border/50">
                <TabsTrigger
                  value="upcoming"
                  className="rounded-lg data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm font-semibold transition-all"
                >
                  À venir
                </TabsTrigger>
                <TabsTrigger
                  value="past"
                  className="rounded-lg data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm font-semibold transition-all"
                >
                  Passées
                </TabsTrigger>
              </TabsList>

              {/* Content */}
              <AnimatePresence mode="wait">
                {activeTab === "upcoming" && (
                  <motion.div
                    key="upcoming"
                    variants={STAGGER_CONTAINER_VARIANTS}
                    initial="hidden"
                    animate="show"
                    exit="hidden"
                    className="mt-6"
                  >
                    <VStack spacing="md">
                      {displayedGames.length === 0 ? (
                        <div className="py-12 text-center">
                          <div className="space-y-2">
                            <Calendar className="w-8 h-8 text-muted-foreground/40 mx-auto" />
                            <p className="text-sm text-muted-foreground/60">
                              {activeTab === "upcoming" ? "Aucune partie à venir" : "Aucune partie passée"}
                            </p>
                          </div>
                        </div>
                      ) : (
                        displayedGames.map((game, index) => (
                          <motion.div 
                            key={game.id} 
                            variants={FADE_IN_ANIMATION_VARIANTS}
                            transition={{ delay: index * 0.05 }}
                          >
                            <Link href={`/games/${game.id}`}>
                              <motion.div
                                whileHover={{ y: -2 }}
                                transition={{ type: "spring", stiffness: 400, damping: 25 }}
                              >
                                <Card className="group border border-border/50 hover:border-border hover:shadow-lg transition-all duration-200 overflow-hidden">
                                  <CardContent className="p-0">
                                    <div className="flex items-stretch gap-0">
                                      {/* Date Badge - Style Calendrier */}
                                      <div className="flex flex-col items-center justify-center w-[72px] bg-gradient-to-br from-primary/10 to-primary/5 border-r border-border/50 py-4 px-2">
                                        <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-0.5">
                                          {new Date(game.dateTime).toLocaleDateString("fr-FR", { month: "short" }).replace('.', '')}
                                        </div>
                                        <div className="text-[32px] font-extrabold text-primary leading-none mb-1">
                                          {new Date(game.dateTime).getDate()}
                                        </div>
                                        <div className="text-xs font-bold text-foreground/80 tracking-tight">
                                          {game.time}
                                        </div>
                                      </div>

                                      {/* Game Info */}
                                      <div className="flex-1 p-5 min-w-0">
                                        {/* Header with title and status */}
                                        <div className="flex items-start justify-between gap-3 mb-3">
                                          <h3 className="font-bold text-base text-foreground line-clamp-2 group-hover:text-primary transition-colors leading-snug">
                                            {game.location}
                                          </h3>
                                          <Badge className="bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300 border-0 text-xs font-bold shrink-0 px-2.5 py-1">
                                            Confirmé
                                          </Badge>
                                        </div>

                                        {/* Date complète */}
                                        <div className="flex items-center gap-2 text-sm text-muted-foreground mb-3.5">
                                          <Calendar className="w-4 h-4 shrink-0" />
                                          <span className="font-medium">{game.date}</span>
                                        </div>

                                        {/* Footer with players and action */}
                                        <div className="flex items-center justify-between pt-2.5 border-t border-border/40">
                                          <div className="flex items-center gap-2 text-sm">
                                            <div className="w-8 h-8 rounded-lg bg-muted/60 flex items-center justify-center">
                                              <Users className="w-4 h-4 text-muted-foreground" />
                                            </div>
                                            <span className="text-foreground/90 font-semibold">
                                              {game.currentPlayers}/{game.maxPlayers}
                                            </span>
                                          </div>
                                          <ChevronRight className="w-5 h-5 text-muted-foreground group-hover:text-primary group-hover:translate-x-1 transition-all" />
                                        </div>
                                      </div>
                                    </div>
                                  </CardContent>
                                </Card>
                              </motion.div>
                            </Link>
                          </motion.div>
                        ))
                      )}
                    </VStack>
                  </motion.div>
                )}

                {activeTab === "past" && (
                  <motion.div
                    key="past"
                    variants={STAGGER_CONTAINER_VARIANTS}
                    initial="hidden"
                    animate="show"
                    exit="hidden"
                    className="mt-6"
                  >
                    <VStack spacing="md">
                      {displayedGames.length === 0 ? (
                        <div className="py-12 text-center">
                          <div className="space-y-2">
                            <Clock className="w-8 h-8 text-muted-foreground/40 mx-auto" />
                            <p className="text-sm text-muted-foreground/60">
                              Aucune partie passée
                            </p>
                          </div>
                        </div>
                      ) : (
                        displayedGames.map((game, index) => (
                          <motion.div 
                            key={game.id} 
                            variants={FADE_IN_ANIMATION_VARIANTS}
                            transition={{ delay: index * 0.05 }}
                          >
                            <Link href={`/games/${game.id}`}>
                              <motion.div
                                whileHover={{ y: -2 }}
                                transition={{ type: "spring", stiffness: 400, damping: 25 }}
                              >
                                <Card className="group border border-border/50 hover:border-border hover:shadow-lg transition-all duration-200 overflow-hidden opacity-60">
                                  <CardContent className="p-0">
                                    <div className="flex items-stretch gap-0">
                                      {/* Date Badge */}
                                      <div className="flex flex-col items-center justify-center w-[72px] bg-gradient-to-br from-muted/30 to-muted/10 border-r border-border/50 py-4 px-2">
                                        <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest mb-0.5">
                                          {new Date(game.dateTime).toLocaleDateString("fr-FR", { month: "short" }).replace('.', '')}
                                        </div>
                                        <div className="text-[32px] font-extrabold text-muted-foreground leading-none mb-1">
                                          {new Date(game.dateTime).getDate()}
                                        </div>
                                        <div className="text-xs font-bold text-foreground/60 tracking-tight">
                                          {game.time}
                                        </div>
                                      </div>

                                      {/* Game Info */}
                                      <div className="flex-1 p-5 min-w-0">
                                        <div className="flex items-start justify-between gap-3 mb-3">
                                          <h3 className="font-bold text-base text-foreground/70 line-clamp-2 leading-snug">
                                            {game.location}
                                          </h3>
                                          <Badge className="bg-muted text-muted-foreground border-0 text-xs font-bold shrink-0 px-2.5 py-1">
                                            Terminée
                                          </Badge>
                                        </div>

                                        <div className="flex items-center gap-2 text-sm text-muted-foreground mb-3.5">
                                          <Calendar className="w-4 h-4 shrink-0" />
                                          <span className="font-medium">{game.date}</span>
                                        </div>

                                        <div className="flex items-center justify-between pt-2.5 border-t border-border/40">
                                          <div className="flex items-center gap-2 text-sm">
                                            <div className="w-8 h-8 rounded-lg bg-muted/60 flex items-center justify-center">
                                              <Users className="w-4 h-4 text-muted-foreground" />
                                            </div>
                                            <span className="text-foreground/70 font-semibold">
                                              {game.currentPlayers}/{game.maxPlayers}
                                            </span>
                                          </div>
                                          <ChevronRight className="w-5 h-5 text-muted-foreground" />
                                        </div>
                                      </div>
                                    </div>
                                  </CardContent>
                                </Card>
                              </motion.div>
                            </Link>
                          </motion.div>
                        ))
                      )}
                    </VStack>
                  </motion.div>
                )}
              </AnimatePresence>
            </Tabs>
          </VStack>
        </motion.div>
      </VMain>
    </VPage>
  )
}
