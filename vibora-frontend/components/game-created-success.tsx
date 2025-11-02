"use client"

import { motion } from "framer-motion"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent } from "@/components/ui/dialog"
import { CheckCircle, Calendar, MapPin, Clock, Users, Share2 } from "lucide-react"
import { useRouter } from "next/navigation"
import { Badge } from "@/components/ui/badge"

interface GameCreatedSuccessProps {
  isOpen: boolean
  onClose: () => void
  gameId: string
  gameDetails: {
    date: string
    time: string
    club: string
    location: string
    level: number
    spotsLeft: number
  }
}

export function GameCreatedSuccess({ isOpen, onClose, gameId, gameDetails }: GameCreatedSuccessProps) {
  const router = useRouter()

  const handleViewGame = () => {
    router.push(`/games/${gameId}`)
    onClose()
  }

  const handleShareGame = () => {
    // Logique de partage à implémenter
    // Pour l'instant, simulons juste une copie de lien
    navigator.clipboard.writeText(`${window.location.origin}/games/${gameId}`)
    // Ici vous pourriez afficher un toast de confirmation
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString("fr-FR", { weekday: "long", day: "numeric", month: "long" })
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="bg-card border-border p-0 overflow-hidden max-w-md mx-auto rounded-xl">
        <div className="p-6">
          <div className="flex flex-col items-center text-center mb-6">
            <div className="bg-primary/10 p-3 rounded-full mb-4">
              <CheckCircle className="h-8 w-8 text-primary" />
            </div>
            <h2 className="text-xl font-bold text-foreground mb-2">Partie créée avec succès !</h2>
            <p className="text-muted-foreground">
              Votre partie a été créée et votre paiement a été autorisé. Vous serez débité uniquement après la partie.
            </p>
          </div>

          <div className="bg-primary/10 rounded-lg p-4 mb-6">
            <div className="grid gap-3">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-primary" />
                <span className="text-sm">{formatDate(gameDetails.date)}</span>
              </div>
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4 text-primary" />
                <span className="text-sm">{gameDetails.time}</span>
              </div>
              <div className="flex items-center gap-2">
                <MapPin className="h-4 w-4 text-primary" />
                <span className="text-sm">{gameDetails.club}, {gameDetails.location}</span>
              </div>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-primary" />
                  <span className="text-sm">
                    {gameDetails.spotsLeft} place{gameDetails.spotsLeft > 1 ? "s" : ""} disponible{gameDetails.spotsLeft > 1 ? "s" : ""}
                  </span>
                </div>
                <Badge className="bg-primary/20 text-primary border-primary/30 text-xs">
                  Niveau {gameDetails.level}
                </Badge>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-3">
            <Button onClick={handleViewGame} className="w-full bg-primary text-primary-foreground hover:bg-primary/90">
              Voir ma partie
            </Button>
            <Button
              variant="outline"
              onClick={handleShareGame}
              className="w-full border-border text-muted-foreground hover:bg-accent hover:text-accent-foreground"
            >
              <Share2 className="mr-2 h-4 w-4" />
              Partager avec des amis
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
