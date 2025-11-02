"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Search, Star, Users, Clock, X } from "lucide-react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"
import { toast } from "@/components/ui/use-toast"

interface Player {
  id: string
  name: string
  avatar?: string
  level: number
  playedWith?: boolean
  favorite?: boolean
  lastPlayed?: string
}

interface InvitePlayersModalProps {
  isOpen: boolean
  onClose: () => void
  gameId: string
  gameLevel: number
}

export function InvitePlayersModal({ isOpen, onClose, gameId, gameLevel }: InvitePlayersModalProps) {
  const [searchQuery, setSearchQuery] = useState("")
  const [selectedPlayers, setSelectedPlayers] = useState<string[]>([])
  const [players, setPlayers] = useState<Player[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [activeTab, setActiveTab] = useState("recent")

  // Simuler le chargement des données des joueurs
  useEffect(() => {
    if (isOpen) {
      setIsLoading(true)
      // Simuler un appel API
      setTimeout(() => {
        const mockPlayers: Player[] = [
          {
            id: "player-1",
            name: "Sophie Martin",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel,
            playedWith: true,
            favorite: true,
            lastPlayed: "2025-04-10",
          },
          {
            id: "player-2",
            name: "Jean Dupont",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel,
            playedWith: true,
            lastPlayed: "2025-04-05",
          },
          {
            id: "player-3",
            name: "Marie Leroy",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel - 1,
            favorite: true,
          },
          {
            id: "player-4",
            name: "Pierre Durand",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel + 1,
          },
          {
            id: "player-5",
            name: "Camille Bernard",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel,
          },
          {
            id: "player-6",
            name: "Lucas Petit",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel - 1,
            playedWith: true,
            lastPlayed: "2025-03-20",
          },
          {
            id: "player-7",
            name: "Emma Roux",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel,
          },
          {
            id: "player-8",
            name: "Thomas Moreau",
            avatar: "/placeholder.svg?height=40&width=40",
            level: gameLevel + 1,
          },
        ]
        setPlayers(mockPlayers)
        setIsLoading(false)
      }, 1000)
    }
  }, [isOpen, gameLevel])

  const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value)
  }

  const togglePlayerSelection = (playerId: string) => {
    if (selectedPlayers.includes(playerId)) {
      setSelectedPlayers(selectedPlayers.filter((id) => id !== playerId))
    } else {
      setSelectedPlayers([...selectedPlayers, playerId])
    }
  }

  const handleInvite = () => {
    if (selectedPlayers.length === 0) {
      toast({
        title: "Aucun joueur sélectionné",
        description: "Veuillez sélectionner au moins un joueur à inviter",
        variant: "destructive",
      })
      return
    }

    // Simuler l'envoi d'invitations
    toast({
      title: "Invitations envoyées",
      description: `${selectedPlayers.length} invitation${selectedPlayers.length > 1 ? "s" : ""} envoyée${selectedPlayers.length > 1 ? "s" : ""}`,
    })
    onClose()
    setSelectedPlayers([])
  }

  const filteredPlayers = players.filter((player) => player.name.toLowerCase().includes(searchQuery.toLowerCase()))

  const recentPlayers = filteredPlayers.filter((player) => player.playedWith || player.lastPlayed)

  const favoriteAndPlayedWith = filteredPlayers.filter((player) => player.favorite || player.playedWith)

  const renderPlayerItem = (player: Player) => (
    <div
      key={player.id}
      className={`flex cursor-pointer items-center justify-between rounded-lg border p-3 transition-colors ${
        selectedPlayers.includes(player.id)
          ? "border-primary bg-primary/5"
          : "border-border bg-card hover:bg-accent/50"
      }`}
      onClick={() => togglePlayerSelection(player.id)}
    >
      <div className="flex items-center gap-3">
        <Avatar className="h-10 w-10 border border-border">
          <AvatarImage src={player.avatar || "/placeholder.svg"} alt={player.name} />
          <AvatarFallback>{player.name.charAt(0)}</AvatarFallback>
        </Avatar>
        <div>
          <div className="flex items-center gap-2">
            <p className="font-medium">{player.name}</p>
            {player.favorite && <Star className="h-3.5 w-3.5 fill-secondary text-secondary" />}
          </div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Badge variant="outline" className="bg-muted/50 px-1.5 py-0 text-xs">
              Niveau {player.level}
            </Badge>
            {player.playedWith && (
              <div className="flex items-center gap-1 text-xs">
                <Clock className="h-3 w-3" />
                <span>
                  Dernier match:{" "}
                  {new Date(player.lastPlayed!).toLocaleDateString("fr-FR", {
                    day: "numeric",
                    month: "short",
                  })}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
      <div
        className={`flex h-5 w-5 items-center justify-center rounded-full border ${
          selectedPlayers.includes(player.id) ? "border-primary bg-primary text-primary-foreground" : "border-border bg-background"
        }`}
      >
        {selectedPlayers.includes(player.id) && <span className="text-xs">✓</span>}
      </div>
    </div>
  )

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-h-[90vh] max-w-md overflow-hidden p-0 sm:max-w-md">
        <DialogHeader className="sticky top-0 z-10 border-b bg-background px-4 py-3">
          <div className="flex items-center justify-between">
            <DialogTitle>Inviter des joueurs</DialogTitle>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={onClose}>
              <X className="h-4 w-4" />
              <span className="sr-only">Fermer</span>
            </Button>
          </div>
          <div className="mt-2 flex items-center gap-2 rounded-md border bg-muted px-3 py-2">
            <Search className="h-4 w-4 text-muted-foreground" />
            <Input
              type="text"
              placeholder="Rechercher un joueur..."
              value={searchQuery}
              onChange={handleSearch}
              className="border-0 bg-transparent p-0 shadow-none focus-visible:ring-0"
            />
          </div>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <div className="sticky top-[73px] z-10 border-b bg-background">
            <TabsList className="grid w-full grid-cols-3">
              <TabsTrigger value="recent" className="text-xs">
                Récents
              </TabsTrigger>
              <TabsTrigger value="favorites" className="text-xs">
                Favoris
              </TabsTrigger>
              <TabsTrigger value="all" className="text-xs">
                Tous
              </TabsTrigger>
            </TabsList>
          </div>

          <div className="px-4 pb-4">
            {isLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-primary"></div>
              </div>
            ) : (
              <>
                <TabsContent value="recent" className="mt-4">
                  {recentPlayers.length > 0 ? (
                    <ScrollArea className="max-h-[50vh]">
                      <div className="space-y-2">{recentPlayers.map((player) => renderPlayerItem(player))}</div>
                    </ScrollArea>
                  ) : (
                    <div className="flex flex-col items-center justify-center py-8 text-center">
                      <Users className="mb-2 h-10 w-10 text-muted-foreground" />
                      <p className="text-muted-foreground">Aucun joueur récent trouvé</p>
                    </div>
                  )}
                </TabsContent>

                <TabsContent value="favorites" className="mt-4">
                  {favoriteAndPlayedWith.length > 0 ? (
                    <ScrollArea className="max-h-[50vh]">
                      <div className="space-y-2">{favoriteAndPlayedWith.map((player) => renderPlayerItem(player))}</div>
                    </ScrollArea>
                  ) : (
                    <div className="flex flex-col items-center justify-center py-8 text-center">
                      <Star className="mb-2 h-10 w-10 text-muted-foreground" />
                      <p className="text-muted-foreground">Aucun joueur favori ou récent trouvé</p>
                    </div>
                  )}
                </TabsContent>

                <TabsContent value="all" className="mt-4">
                  {filteredPlayers.length > 0 ? (
                    <ScrollArea className="max-h-[50vh]">
                      <div className="space-y-2">{filteredPlayers.map((player) => renderPlayerItem(player))}</div>
                    </ScrollArea>
                  ) : (
                    <div className="flex flex-col items-center justify-center py-8 text-center">
                      <Search className="mb-2 h-10 w-10 text-muted-foreground" />
                      <p className="text-muted-foreground">Aucun joueur trouvé</p>
                    </div>
                  )}
                </TabsContent>
              </>
            )}

            <div className="mt-4 flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {selectedPlayers.length} joueur{selectedPlayers.length !== 1 ? "s" : ""} sélectionné
                {selectedPlayers.length !== 1 ? "s" : ""}
              </p>
              <Button onClick={handleInvite} disabled={selectedPlayers.length === 0}>
                Inviter
              </Button>
            </div>
          </div>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
