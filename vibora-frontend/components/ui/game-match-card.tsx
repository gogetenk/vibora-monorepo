"use client"

import { motion } from "framer-motion"
import { Calendar as CalendarIcon, Clock, MapPin, Users } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { format, parseISO } from "date-fns"
import { fr } from "date-fns/locale"
import type { GameMatchDto } from "@/lib/api/vibora-types"

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

export interface SearchCriteria {
  when: string // ISO 8601
  where: string
  level?: number
}

export interface GameMatchCardProps {
  game: GameMatchDto
  isPerfectMatch: boolean
  onJoin: () => void
  searchCriteria: SearchCriteria | null
  isSecondaryAction?: boolean
}

export function GameMatchCard({
  game,
  isPerfectMatch,
  onJoin,
  searchCriteria,
  isSecondaryAction = false
}: GameMatchCardProps) {
  const spotsLeft = game.maxPlayers - game.currentPlayers

  return (
    <motion.div
      whileHover={{ y: -3 }}
      transition={{ type: "spring", stiffness: 500, damping: 30 }}
    >
      <Card className={`group relative overflow-hidden border-2 transition-all duration-300 ${
        isPerfectMatch
          ? 'border-emerald-200 dark:border-emerald-900/50 bg-gradient-to-br from-emerald-50/80 via-emerald-50/40 to-transparent dark:from-emerald-950/30 dark:via-emerald-950/10 dark:to-transparent hover:border-emerald-300 dark:hover:border-emerald-800 shadow-sm hover:shadow-lg'
          : isSecondaryAction
          ? 'border-border/40 hover:border-border/60 opacity-90 backdrop-blur-sm'
          : 'border-border/60 hover:border-border hover:shadow-sm backdrop-blur-sm'
      }`}>
        {/* Subtle gradient overlay on hover */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none" />

        <CardContent className="relative p-6 space-y-4">
          {/* Header: Avatar + Host Info */}
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-center gap-3.5 flex-1 min-w-0">
              <Avatar className="w-12 h-12 border-2 border-background shadow-sm">
                <AvatarFallback className="bg-gradient-to-br from-primary/20 via-primary/10 to-primary/5 text-primary font-bold text-base">
                  {game.hostDisplayName?.[0]?.toUpperCase() || "?"}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-base truncate text-foreground">
                  {game.hostDisplayName || "Organisateur"} organise
                </h3>
                {isPerfectMatch && (
                  <div className="flex items-center gap-1.5 mt-1">
                    <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                    <span className="text-xs font-semibold text-emerald-600 dark:text-emerald-400">
                      Match parfait
                    </span>
                  </div>
                )}
              </div>
            </div>

            {/* Skill Level Badge */}
            {game.skillLevel && (
              <div className="flex items-center justify-center w-11 h-11 text-sm font-bold rounded-xl bg-primary/10 text-primary border border-primary/20 shadow-sm">
                {game.skillLevel}
              </div>
            )}
          </div>

          {/* Date & Time */}
          <div className="flex items-center gap-2.5 text-sm">
            <div className="w-8 h-8 rounded-lg bg-muted/50 flex items-center justify-center shrink-0">
              <CalendarIcon className="w-4 h-4 text-muted-foreground" />
            </div>
            <span className="font-medium text-foreground/90">
              {formatDateRelative(game.dateTime)}, {formatTime(game.dateTime)}
            </span>
          </div>

          {/* Location */}
          <div className="flex items-center gap-2.5 text-sm">
            <div className="w-8 h-8 rounded-lg bg-muted/50 flex items-center justify-center shrink-0">
              <MapPin className="w-4 h-4 text-muted-foreground" />
            </div>
            <div className="flex flex-col gap-0.5 truncate flex-1">
              <span className="font-medium text-foreground/80 truncate">{game.location}</span>
              {game.distanceKm !== null && game.distanceKm !== undefined && (
                <span className="text-xs text-muted-foreground">
                  📍 À {game.distanceKm} km
                </span>
              )}
            </div>
          </div>

          {/* Divider */}
          <div className="border-t border-border/40 pt-3 mt-1" />

          {/* Players Count + Spots */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5 text-sm">
              <div className="w-8 h-8 rounded-lg bg-muted/50 flex items-center justify-center shrink-0">
                <Users className="w-4 h-4 text-muted-foreground" />
              </div>
              <span className="font-medium text-foreground/90">
                {game.currentPlayers}/{game.maxPlayers} joueurs
              </span>
            </div>
            {spotsLeft > 0 && (
              <Badge className="bg-gradient-to-r from-amber-100 to-amber-50 text-amber-800 dark:from-amber-950 dark:to-amber-900 dark:text-amber-200 border-0 font-medium px-2.5 py-0.5 text-xs shadow-sm">
                {spotsLeft} place{spotsLeft > 1 ? "s" : ""}
              </Badge>
            )}
          </div>

          {/* Action Button - Visual hierarchy based on context */}
          <Button
            onClick={onJoin}
            size="lg"
            variant={isSecondaryAction ? "outline" : "default"}
            className={`w-full h-11 font-semibold transition-all duration-200 ${
              isPerfectMatch
                ? 'bg-gradient-to-r from-emerald-600 via-emerald-600 to-emerald-500 hover:from-emerald-700 hover:via-emerald-700 hover:to-emerald-600 text-white border-0 shadow-md hover:shadow-lg'
                : isSecondaryAction
                ? 'border-2 hover:bg-muted/50'
                : 'bg-primary hover:bg-primary/90 shadow-sm hover:shadow-md'
            }`}
          >
            Rejoindre
          </Button>
        </CardContent>
      </Card>
    </motion.div>
  )
}
