"use client"

import { format } from "date-fns"
import { fr } from "date-fns/locale"
import { Calendar, Clock, Users } from "lucide-react"
import { Badge } from "@/components/ui/badge"

interface Game {
  id: string
  date: string
  startTime: string
  endTime: string
  court: string
  players: Array<{ id: string; name: string }>
  price: number
  status: string
}

export function PlayerGamesList({ games }: { games: Game[] }) {
  if (games.length === 0) {
    return <div className="flex h-40 items-center justify-center text-muted-foreground">Aucune partie trouvée</div>
  }

  return (
    <div className="space-y-4">
      {games.map((game) => {
        const gameDate = new Date(game.date)

        return (
          <div key={game.id} className="rounded-lg border p-4">
            <div className="mb-2 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{format(gameDate, "d MMMM yyyy", { locale: fr })}</span>
              </div>
              <Badge
                variant={game.status === "completed" ? "default" : "outline-solid"}
                className={game.status === "completed" ? "bg-green-100 text-green-800" : ""}
              >
                {game.status === "completed" ? "Terminée" : "À venir"}
              </Badge>
            </div>

            <div className="mb-3 grid grid-cols-2 gap-2 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4" />
                <span>
                  {game.startTime} - {game.endTime}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <Users className="h-4 w-4" />
                <span>{game.players.length} joueurs</span>
              </div>
            </div>

            <div className="flex justify-between">
              <div>
                <div className="text-sm font-medium">Terrain {game.court}</div>
                <div className="text-sm text-muted-foreground">{game.players.map((p) => p.name).join(", ")}</div>
              </div>
              <div className="text-right">
                <div className="font-medium">{game.price} €</div>
                <div className="text-xs text-muted-foreground">
                  {(game.price / game.players.length).toFixed(2)} € par joueur
                </div>
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}
