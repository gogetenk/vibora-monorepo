"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Calendar, Clock, MapPin, Users } from "lucide-react"
import Link from "next/link"

export interface GameInvitation {
  id: string
  gameId: string
  creatorId: string
  creatorName: string
  creatorAvatar?: string
  date: string
  time: string
  endTime: string
  clubName: string
  clubAddress: string
  level: number
  playersCount: number
  maxPlayers: number
  price: number
  expiresAt: string
}

interface MyInvitationsProps {
  invitations: GameInvitation[]
  onAccept: (invitationId: string) => void
  onDecline: (invitationId: string) => void
}

export function MyInvitations({ invitations, onAccept, onDecline }: MyInvitationsProps) {
  const [expandedInvitation, setExpandedInvitation] = useState<string | null>(null)

  if (invitations.length === 0) {
    return null
  }

  const formatDate = (dateString: string) => {
    const options: Intl.DateTimeFormatOptions = { weekday: "long", day: "numeric", month: "long" }
    return new Date(dateString).toLocaleDateString("fr-FR", options)
  }

  const getTimeRemaining = (expiresAt: string) => {
    const now = new Date()
    const expiry = new Date(expiresAt)
    const diffMs = expiry.getTime() - now.getTime()

    if (diffMs <= 0) return "Expiré"

    const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
    const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60))

    if (diffHours > 0) {
      return `${diffHours}h${diffMinutes}m`
    }
    return `${diffMinutes}m`
  }

  return (
    <Card className="mb-4">
      <CardHeader className="border-b bg-primary-50 py-3">
        <CardTitle className="text-base font-medium">Invitations reçues ({invitations.length})</CardTitle>
      </CardHeader>

      <CardContent className="p-0">
        <div className="divide-y">
          {invitations.map((invitation) => (
            <div key={invitation.id} className="p-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Avatar className="h-10 w-10 border border-gray-200">
                    <AvatarImage src={invitation.creatorAvatar || "/placeholder.svg"} alt={invitation.creatorName} />
                    <AvatarFallback>{invitation.creatorName.charAt(0)}</AvatarFallback>
                  </Avatar>
                  <div>
                    <p className="font-medium">{invitation.creatorName} vous invite</p>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-blue-50 text-blue-700">
                        Niveau {invitation.level}
                      </Badge>
                      <span className="text-xs text-amber-600">
                        Expire dans {getTimeRemaining(invitation.expiresAt)}
                      </span>
                    </div>
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setExpandedInvitation(expandedInvitation === invitation.id ? null : invitation.id)}
                  className="h-8 px-2 text-xs"
                >
                  {expandedInvitation === invitation.id ? "Moins" : "Plus"}
                </Button>
              </div>

              {expandedInvitation === invitation.id && (
                <div className="mt-3 space-y-3">
                  <div className="rounded-lg bg-gray-50 p-3 text-sm">
                    <div className="space-y-2">
                      <div className="flex items-center gap-2">
                        <Calendar className="h-4 w-4 text-gray-500" />
                        <span>{formatDate(invitation.date)}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Clock className="h-4 w-4 text-gray-500" />
                        <span>
                          {invitation.time} - {invitation.endTime}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-gray-500" />
                        <span>{invitation.clubName}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Users className="h-4 w-4 text-gray-500" />
                        <span>
                          {invitation.playersCount}/{invitation.maxPlayers} joueurs
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button className="flex-1" onClick={() => onAccept(invitation.id)}>
                      Accepter ({invitation.price}€)
                    </Button>
                    <Button variant="outline" className="flex-1" onClick={() => onDecline(invitation.id)}>
                      Décliner
                    </Button>
                  </div>

                  <div className="text-center">
                    <Link href={`/games/${invitation.gameId}`} className="text-xs text-primary underline">
                      Voir les détails de la partie
                    </Link>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
