"use client"

import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { MapPin, Users, Calendar, Search, Bell } from "lucide-react"
import { motion } from "framer-motion"
import { MobileNav } from "@/components/mobile-nav"
import { ThemeToggle } from "@/components/ui/theme-toggle"
import { HomePageSkeleton } from "@/components/home-skeleton"

import { useState, useEffect } from "react"

// Mock data with real profile pictures
const myUpcomingGames = [
  {
    id: "1",
    time: "18:00",
    date: "Aujourd'hui",
    location: "Le Padel Club",
    courtName: "Court Central",
    players: [
      "https://images.unsplash.com/photo-1599566150163-29194dcaad36?q=80&w=1287&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
      "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=1287&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
      "https://images.unsplash.com/photo-1580489944761-15a19d654956?q=80&w=1361&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    ],
    imageUrl: "/vibrant-padel-match.png",
  },
  {
    id: "2",
    time: "20:00",
    date: "Demain",
    location: "Urban Padel",
    courtName: "Court 3",
    players: [
      "https://images.unsplash.com/photo-1544005313-94ddf0286df2?q=80&w=1288&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    ],
    playersNeeded: 1,
    imageUrl: "/summer-padel-match.png",
  },
]

const availableGames = [
  {
    id: "1",
    time: "18:00",
    location: "Le Padel Club",
    distance: "2.5 km",
    playersNeeded: 2,
    skillLevel: 7,
  },
  {
    id: "2",
    time: "19:30",
    location: "Urban Padel",
    distance: "4 km",
    playersNeeded: 1,
    skillLevel: 9,
  },
]

const quickFilters = [
  { id: "tonight", label: "Ce soir", count: 5, active: false },
  { id: "level", label: "Niveau 5-6", count: 8, active: false },
  { id: "nearby", label: "À proximité", count: 12, active: true },
  { id: "morning", label: "Matin", count: 3, active: false },
  { id: "weekend", label: "Week-end", count: 15, active: false },
]

// Animation variants for Framer Motion
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

const CARD_HOVER_VARIANTS = {
  rest: { scale: 1, y: 0 },
  hover: {
    scale: 1.02,
    y: -2,
    transition: {
      type: "spring" as const,
      stiffness: 400,
      damping: 25,
    },
  },
}

export default function Home() {
  const [filters, setFilters] = useState(quickFilters)
  const [isLoading, setIsLoading] = useState(true)

  // Simulate loading for 1 second
  useEffect(() => {
    const timer = setTimeout(() => {
      setIsLoading(false)
    }, 1000)

    return () => clearTimeout(timer)
  }, [])

  const toggleFilter = (filterId: string) => {
    setFilters((prev) =>
      prev.map((filter) => (filter.id === filterId ? { ...filter, active: !filter.active } : filter)),
    )
  }

  // Show skeleton while loading
  if (isLoading) {
    return <HomePageSkeleton />
  }

  return (
    <motion.div 
      className="min-h-screen bg-background text-foreground pb-32"
      initial="hidden"
      animate="show"
      variants={STAGGER_CONTAINER_VARIANTS}
    >
      {/* Header with micro-interactions */}
      <motion.header 
        className="sticky top-0 z-40 bg-background/80 backdrop-blur-lg"
        variants={SLIDE_UP_VARIANTS}
      >
        <div className="container flex items-center justify-between h-20">
          <motion.div
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            transition={{ type: "spring", stiffness: 400, damping: 25 }}
          >
            <Link href="/settings/profile">
              <Avatar className="w-10 h-10 border-2 border-muted transition-all duration-200 hover:border-primary/50">
                <AvatarImage
                  src="https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?q=80&w=2080&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                  alt="User"
                />
                <AvatarFallback>U</AvatarFallback>
              </Avatar>
            </Link>
          </motion.div>
          <motion.div 
            className="flex items-center gap-2"
            variants={SLIDE_UP_VARIANTS}
          >
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }}>
              <ThemeToggle />
            </motion.div>
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }}>
              <Button variant="ghost" size="icon" className="rounded-full hover:bg-accent/50 transition-colors">
                <Search className="w-5 h-5 text-muted-foreground" />
              </Button>
            </motion.div>
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.9 }}>
              <Button variant="ghost" size="icon" className="relative rounded-full hover:bg-accent/50 transition-colors">
                <Bell className="w-5 h-5 text-muted-foreground" />
                <span className="absolute flex w-2.5 h-2.5 top-2 right-2">
                  <span className="absolute inline-flex w-full h-full rounded-full opacity-75 animate-ping bg-success"></span>
                  <span className="relative inline-flex w-2.5 h-2.5 rounded-full bg-success"></span>
                </span>
              </Button>
            </motion.div>
          </motion.div>
        </div>
      </motion.header>

      <main>
        <div className="container">
          <motion.div
            variants={STAGGER_CONTAINER_VARIANTS}
            className="space-y-8"
          >
            {/* Welcome Section */}
            <motion.section variants={SLIDE_UP_VARIANTS}>
              <div className="mb-8">
                <motion.h1 
                  className="mb-2 text-2xl font-bold"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.3, type: "spring", stiffness: 300 }}
                >
                  Salut Alex ! 👋
                </motion.h1>
                <motion.p 
                  className="text-muted-foreground"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.4, type: "spring", stiffness: 300 }}
                >
                  Prêt pour une nouvelle partie de padel ?
                </motion.p>
              </div>
            </motion.section>

            {/* Quick Filters */}
            <motion.section variants={SLIDE_UP_VARIANTS}>
              <motion.h2 
                className="mb-4 font-semibold"
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.5 }}
              >
                Filtres rapides
              </motion.h2>
              <div className="flex gap-2 overflow-x-auto pb-2 hide-scrollbar">
                {filters.map((filter, index) => (
                  <motion.div
                    key={filter.id}
                    initial={{ opacity: 0, scale: 0.8, y: 20 }}
                    animate={{ opacity: 1, scale: 1, y: 0 }}
                    transition={{ 
                      delay: 0.6 + index * 0.1,
                      type: "spring",
                      stiffness: 400,
                      damping: 25
                    }}
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                  >
                    <Button
                      variant={filter.active ? "default" : "outline"}
                      size="sm"
                      onClick={() => toggleFilter(filter.id)}
                      className={`shrink-0 transition-all duration-200 ${
                        filter.active
                          ? "bg-success/40 text-success-foreground border-success/50 hover:bg-success/50"
                          : "hover:bg-accent/50"
                      }`}
                    >
                      {filter.label}
                      <span className="ml-1.5 text-xs opacity-70">({filter.count})</span>
                    </Button>
                  </motion.div>
                ))}
              </div>
            </motion.section>

            {/* Upcoming Games Section */}
            <motion.section variants={SLIDE_UP_VARIANTS}>
              <div className="flex items-center justify-between mb-4">
                <motion.h2 
                  className="font-semibold"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.8 }}
                >
                  Mes prochaines parties
                </motion.h2>
                <motion.div
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.9 }}
                  whileHover={{ scale: 1.05 }}
                >
                  <Link href="/my-games" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                    Voir tout
                  </Link>
                </motion.div>
              </div>
              <div className="flex gap-4 overflow-x-auto pb-4 hide-scrollbar snap-x snap-mandatory">
                {myUpcomingGames.map((game, index) => (
                  <motion.div
                    key={game.id}
                    className="shrink-0 w-[280px] snap-start"
                    initial={{ opacity: 0, x: 50, scale: 0.9 }}
                    animate={{ opacity: 1, x: 0, scale: 1 }}
                    transition={{ 
                      delay: 1.0 + index * 0.15,
                      type: "spring",
                      stiffness: 300,
                      damping: 30
                    }}
                    variants={CARD_HOVER_VARIANTS}
                    initial="rest"
                    whileHover="hover"
                  >
                    <Link href={`/games/${game.id}`}>
                      <Card className="overflow-hidden border-border/50 bg-card/80 backdrop-blur-sm hover:bg-card/90 transition-all duration-200 hover:shadow-lg">
                        <div
                          className="relative h-32 bg-cover bg-center"
                          style={{
                            backgroundImage: `url(${game.imageUrl})`,
                          }}
                        >
                          <div className="absolute inset-0 bg-gradient-to-t from-black/95 via-black/60 to-black/20" />
                          <div className="absolute bottom-3 left-3 right-3">
                            <div className="flex items-center justify-between mb-1">
                              <span className="text-xs font-medium text-white/95 drop-shadow-[0_1px_3px_rgba(0,0,0,0.8)]">
                                {game.date}
                              </span>
                              <span className="text-sm font-bold text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">
                                {game.time}
                              </span>
                            </div>
                            <h3 className="font-semibold text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">
                              {game.location}
                            </h3>
                            <p className="text-sm text-white/95 drop-shadow-[0_1px_3px_rgba(0,0,0,0.8)]">
                              {game.courtName}
                            </p>
                          </div>
                        </div>
                        <CardContent className="p-3">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-1 text-sm text-muted-foreground">
                              <Users className="w-3.5 h-3.5" />
                              <span>Joueurs confirmés</span>
                            </div>
                            <div className="flex items-center gap-1">
                              {game.players.map((player, playerIndex) => (
                                <motion.div
                                  key={playerIndex}
                                  initial={{ opacity: 0, scale: 0 }}
                                  animate={{ opacity: 1, scale: 1 }}
                                  transition={{ 
                                    delay: 1.2 + index * 0.15 + playerIndex * 0.05,
                                    type: "spring",
                                    stiffness: 500
                                  }}
                                >
                                  <Avatar className="w-6 h-6 border border-background">
                                    <AvatarImage src={player || "/placeholder.svg"} />
                                    <AvatarFallback>P</AvatarFallback>
                                  </Avatar>
                                </motion.div>
                              ))}
                              {game.playersNeeded && (
                                <motion.div 
                                  className="flex items-center justify-center w-6 h-6 text-xs font-medium border-2 rounded-full bg-success/40 text-success-foreground border-background"
                                  initial={{ opacity: 0, scale: 0 }}
                                  animate={{ opacity: 1, scale: 1 }}
                                  transition={{ 
                                    delay: 1.3 + index * 0.15,
                                    type: "spring",
                                    stiffness: 500
                                  }}
                                >
                                  +{game.playersNeeded}
                                </motion.div>
                              )}
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    </Link>
                  </motion.div>
                ))}
                
                {/* Create Game Card */}
                <motion.div 
                  className="shrink-0 w-[280px]"
                  initial={{ opacity: 0, x: 50, scale: 0.9 }}
                  animate={{ opacity: 1, x: 0, scale: 1 }}
                  transition={{ 
                    delay: 1.0 + myUpcomingGames.length * 0.15,
                    type: "spring",
                    stiffness: 300,
                    damping: 30
                  }}
                  whileHover={{ scale: 1.02, y: -2 }}
                  whileTap={{ scale: 0.98 }}
                >
                  <Card className="flex flex-col items-center justify-center h-full overflow-hidden snap-start transition-all duration-200 border-2 border-dashed shadow-none border-muted-foreground/20 bg-secondary/20 hover:border-primary hover:bg-secondary/30">
                    <Link href="/create-game" className="text-center group p-8">
                      <motion.div 
                        className="flex items-center justify-center w-12 h-12 mb-2 rounded-full bg-primary/20 transition-all duration-200 group-hover:bg-primary/30"
                        whileHover={{ rotate: 5, scale: 1.1 }}
                      >
                        <Users className="w-6 h-6 text-primary" />
                      </motion.div>
                      <p className="font-semibold text-foreground">Créer une partie</p>
                      <p className="text-sm text-muted-foreground">et inviter des amis</p>
                    </Link>
                  </Card>
                </motion.div>
              </div>
            </motion.section>

            {/* Available Games Section */}
            <motion.section variants={SLIDE_UP_VARIANTS}>
              <div className="flex items-center justify-between mb-4">
                <motion.h2 
                  className="font-semibold"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 1.4 }}
                >
                  Parties près de vous
                </motion.h2>
                <motion.div
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 1.5 }}
                  whileHover={{ scale: 1.05 }}
                >
                  <Link href="/search" className="text-sm font-medium text-primary hover:text-primary/80 transition-colors">
                    Voir tout
                  </Link>
                </motion.div>
              </div>
              <div className="grid grid-cols-1 gap-3">
                {availableGames.map((game, index) => (
                  <motion.div
                    key={game.id}
                    initial={{ opacity: 0, y: 20, scale: 0.95 }}
                    animate={{ opacity: 1, y: 0, scale: 1 }}
                    transition={{ 
                      delay: 1.6 + index * 0.1,
                      type: "spring",
                      stiffness: 300,
                      damping: 30
                    }}
                    whileHover={{ scale: 1.01, y: -1 }}
                    whileTap={{ scale: 0.99 }}
                  >
                    <Link href={`/games/${game.id}`}>
                      <Card className="overflow-hidden border-border/50 bg-card/80 backdrop-blur-sm hover:bg-card/90 transition-all duration-200 hover:shadow-md">
                        <CardContent className="p-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-4">
                              <div className="text-center">
                                <div className="text-xl font-bold">{game.time.split(":")[0]}</div>
                                <div className="text-xs text-muted-foreground">{game.time.split(":")[1]}</div>
                              </div>
                              <div>
                                <h3 className="font-semibold">{game.location}</h3>
                                <div className="flex items-center gap-1 mt-1 text-sm text-muted-foreground">
                                  <MapPin className="w-3.5 h-3.5" />
                                  <span>{game.distance}</span>
                                  <span className="mx-1">•</span>
                                  <Users className="w-3.5 h-3.5" />
                                  <span>{game.playersNeeded} places</span>
                                </div>
                              </div>
                            </div>
                            <motion.div 
                              className="flex items-center justify-center w-10 h-10 text-sm font-bold rounded-full bg-success text-success-foreground"
                              whileHover={{ scale: 1.1, rotate: 5 }}
                            >
                              {game.skillLevel}
                            </motion.div>
                          </div>
                        </CardContent>
                      </Card>
                    </Link>
                  </motion.div>
                ))}
              </div>
            </motion.section>
          </motion.div>
        </div>
      </main>
      
      <MobileNav />
    </motion.div>
  )
}
