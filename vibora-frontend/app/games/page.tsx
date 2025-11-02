"use client"

import { useState } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { MapPin, Calendar, Clock, ArrowLeft, Users } from "lucide-react"
import { motion } from "framer-motion"

// Mock data for available games
const availableGames = [
  {
    id: "1",
    time: "18:00",
    date: "Aujourd'hui",
    location: "Le Padel Club",
    distance: "2.5 km",
    playersNeeded: 2,
    skillLevel: 7,
    organizer: "Marc",
    organizerAvatar: "https://images.unsplash.com/photo-1599566150163-29194dcaad36?q=80&w=1287&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    players: [
      "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=1287&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
      "https://images.unsplash.com/photo-1580489944761-15a19d654956?q=80&w=1361&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    ]
  },
  {
    id: "2",
    time: "19:30",
    date: "Aujourd'hui",
    location: "Urban Padel",
    distance: "4 km",
    playersNeeded: 1,
    skillLevel: 9,
    organizer: "Sophie",
    organizerAvatar: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?q=80&w=1288&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    players: [
      "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?q=80&w=2080&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
      "https://images.unsplash.com/photo-1494790108755-2616b612b786?q=80&w=1287&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
      "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
    ]
  },
]

const FADE_IN_VARIANTS = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0, transition: { duration: 0.25 } },
}

export default function GamesPage() {
  return (
    <div className="min-h-screen bg-background text-foreground pb-20">
      <div className="container py-6">
        <div className="mb-6 flex items-center gap-4">
          <Link href="/">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <ArrowLeft className="h-4 w-4" />
              <span className="sr-only">Retour</span>
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Parties disponibles</h1>
        </div>

        <motion.div
          initial="hidden"
          animate="show"
          variants={{
            hidden: {},
            show: { transition: { staggerChildren: 0.1 } },
          }}
          className="space-y-4"
        >
          {availableGames.map((game) => (
            <motion.div key={game.id} variants={FADE_IN_VARIANTS}>
              <Link href={`/join-game/${game.id}`}>
                <Card className="overflow-hidden border-border/50 bg-card/80 backdrop-blur-sm hover:bg-card/90 transition-all duration-200 hover:shadow-md">
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-4">
                        <Avatar className="w-12 h-12 border-2 border-muted transition-all duration-200">
                          <AvatarImage src={game.organizerAvatar} alt={game.organizer} />
                          <AvatarFallback>{game.organizer[0]}</AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="flex items-center gap-2 mb-1">
                            <h3 className="font-semibold">{game.organizer}</h3>
                            <span className="text-sm text-muted-foreground">organise</span>
                          </div>
                          <div className="flex items-center gap-1 text-sm text-muted-foreground">
                            <Calendar className="w-3.5 h-3.5" />
                            <span>{game.date}, {game.time}</span>
                            <span className="mx-1">•</span>
                            <MapPin className="w-3.5 h-3.5" />
                            <span>{game.distance}</span>
                          </div>
                          <div className="flex items-center gap-2 mt-2">
                            <div className="flex -space-x-2">
                              {game.players.map((player, i) => (
                                <Avatar key={i} className="w-6 h-6 border-2 border-background">
                                  <AvatarImage src={player} />
                                  <AvatarFallback>P</AvatarFallback>
                                </Avatar>
                              ))}
                            </div>
                            <span className="text-xs text-muted-foreground">
                              {game.playersNeeded} place{game.playersNeeded > 1 ? 's' : ''} restante{game.playersNeeded > 1 ? 's' : ''}
                            </span>
                          </div>
                        </div>
                      </div>
                      <div className="flex flex-col items-center gap-2">
                        <div className="flex items-center justify-center w-10 h-10 text-sm font-bold rounded-full bg-success/40 text-success-foreground">
                          {game.skillLevel}
                        </div>
                        <Button size="sm" className="px-3 py-1 text-xs">
                          Rejoindre
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            </motion.div>
          ))}
        </motion.div>
      </div>
    </div>
  )
}
