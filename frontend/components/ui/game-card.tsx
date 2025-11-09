"use client"

import React from "react"
import Link from "next/link"
import { motion } from "framer-motion"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Calendar, MapPin, Users, Plus } from "lucide-react"
import { cn } from "@/lib/utils"

interface GameCardProps {
  id: string
  title: string
  subtitle?: string
  time: string
  date?: string
  location?: string
  distance?: string
  imageUrl?: string
  players?: string[]
  playersNeeded?: number
  skillLevel?: number
  spotsLeft?: number
  price?: number
  className?: string
  variant?: "upcoming" | "available" | "create"
  onClick?: () => void
}

export function GameCard({
  id,
  title,
  subtitle,
  time,
  date,
  location,
  distance,
  imageUrl,
  players = [],
  playersNeeded,
  skillLevel,
  spotsLeft,
  price,
  className,
  variant = "upcoming",
  onClick,
}: GameCardProps) {
  const handleClick = () => {
    if (onClick) {
      onClick()
    }
  }

  // Upcoming game card with image
  if (variant === "upcoming") {
    return (
      <motion.div
        whileHover={{ scale: 1.02 }}
        whileTap={{ scale: 0.98 }}
        transition={{ type: "spring", stiffness: 300, damping: 30 }}
      >
        <Link href={`/games/${id}`}>
          <Card className={cn(
            "shrink-0 w-[280px] overflow-hidden snap-start border-border/50 bg-card/80 backdrop-blur-sm hover:bg-card/90 transition-all duration-200 hover:shadow-lg",
            className
          )}>
            {imageUrl && (
              <div className="relative h-40">
                <img
                  src={imageUrl}
                  alt={title}
                  loading="lazy"
                  className="object-cover w-full h-full"
                />
                <div className="absolute inset-0 bg-linear-to-t from-black/60 to-transparent"></div>
                <div className="absolute p-3 bottom-1">
                  <h3 className="font-bold text-white">{title}</h3>
                  {subtitle && <p className="text-sm text-white/80">{subtitle}</p>}
                </div>
              </div>
            )}
            <CardContent className="p-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm">
                  <Calendar className="w-4 h-4 text-muted-foreground" />
                  <span>
                    {date && `${date}, `}{time}
                  </span>
                </div>
                <div className="flex -space-x-2">
                  {players.map((player, i) => (
                    <Avatar key={i} className="w-6 h-6 border-2 border-background">
                      <AvatarImage src={player || "/placeholder.svg"} />
                      <AvatarFallback>P</AvatarFallback>
                    </Avatar>
                  ))}
                  {playersNeeded && (
                    <div className="flex items-center justify-center w-6 h-6 text-xs font-medium border-2 rounded-full bg-success/40 text-success-foreground border-background">
                      +{playersNeeded}
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </Link>
      </motion.div>
    )
  }

  // Available game card - simple layout
  if (variant === "available") {
    return (
      <motion.div
        whileHover={{ scale: 1.01 }}
        whileTap={{ scale: 0.99 }}
        transition={{ type: "spring", stiffness: 300, damping: 30 }}
        onClick={handleClick}
      >
        <Card className={cn(
          "overflow-hidden border-border/50 bg-card/80 backdrop-blur-sm hover:bg-card/90 transition-all duration-200 hover:shadow-md cursor-pointer",
          className
        )}>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="text-center">
                  <div className="text-xl font-bold">{time.split(":")[0]}</div>
                  <div className="text-xs text-muted-foreground">{time.split(":")[1]}</div>
                </div>
                <div>
                  <h3 className="font-semibold">{title}</h3>
                  <div className="flex items-center gap-1 mt-1 text-sm text-muted-foreground">
                    {location && (
                      <>
                        <MapPin className="w-3.5 h-3.5" />
                        <span>{location}</span>
                        {distance && <span> • {distance}</span>}
                      </>
                    )}
                    {(spotsLeft || playersNeeded) && (
                      <>
                        <span className="mx-1">•</span>
                        <Users className="w-3.5 h-3.5" />
                        <span>{spotsLeft || playersNeeded} places</span>
                      </>
                    )}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                {price && <span className="font-semibold">{price}€</span>}
                {skillLevel && (
                  <div className="flex items-center justify-center w-10 h-10 text-sm font-bold rounded-full bg-success text-success-foreground">
                    {skillLevel}
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.div>
    )
  }

  // Create game card - call to action
  if (variant === "create") {
    return (
      <motion.div
        whileHover={{ scale: 1.02 }}
        whileTap={{ scale: 0.98 }}
        transition={{ type: "spring", stiffness: 300, damping: 30 }}
        className="shrink-0 w-[280px]"
      >
        <Card className={cn(
          "flex flex-col items-center justify-center h-full overflow-hidden snap-start transition-all duration-200 border-2 border-dashed shadow-none border-muted-foreground/20 bg-secondary/20 hover:border-primary hover:bg-secondary/30 active:scale-[0.98]",
          className
        )}>
          <Link href="/create-game" className="text-center group" onClick={handleClick}>
            <div className="flex items-center justify-center w-12 h-12 mb-2 rounded-full bg-primary/20 transition-all duration-200 group-hover:bg-primary/30">
              <Users className="w-6 h-6 text-primary" />
            </div>
            <p className="font-semibold text-foreground">{title}</p>
            {subtitle && <p className="text-sm text-muted-foreground">{subtitle}</p>}
          </Link>
        </Card>
      </motion.div>
    )
  }

  return null
}

// Specialized components for common use cases
export function UpcomingGameCard(props: Omit<GameCardProps, "variant">) {
  return <GameCard {...props} variant="upcoming" />
}

export function AvailableGameCard(props: Omit<GameCardProps, "variant">) {
  return <GameCard {...props} variant="available" />
}

export function CreateGameCard(props: Omit<GameCardProps, "variant" | "id">) {
  return <GameCard {...props} id="create" variant="create" />
}