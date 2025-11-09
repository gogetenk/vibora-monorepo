"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, Calendar, Euro, Clock, Activity, Phone, Mail } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { PlayerActivityChart } from "@/components/club/player-activity-chart"
import { PlayerGamesList } from "@/components/club/player-games-list"
import { mockPlayers, mockGames } from "@/lib/mock-data"

export default function PlayerDetailsPage({ params }: { params: { id: string } }) {
  const router = useRouter()
  const [player, setPlayer] = useState<any>(null)
  const [playerGames, setPlayerGames] = useState<any[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    // Simuler le chargement des données du joueur
    const foundPlayer = mockPlayers.find((p) => p.id === params.id)

    if (foundPlayer) {
      setPlayer(foundPlayer)

      // Filtrer les parties du joueur
      const games = mockGames.filter((game) => game.players.some((p) => p.id === params.id))
      setPlayerGames(games)
    }

    setIsLoading(false)
  }, [params.id])

  if (isLoading) {
    return <div className="flex h-full items-center justify-center">Chargement...</div>
  }

  if (!player) {
    return (
      <div className="flex h-full flex-col items-center justify-center">
        <h2 className="text-xl font-semibold">Joueur non trouvé</h2>
        <Button variant="link" onClick={() => router.back()}>
          Retour à la liste des joueurs
        </Button>
      </div>
    )
  }

  // Calculer les statistiques du joueur
  const totalSpent = playerGames.reduce((sum, game) => sum + game.price, 0)
  const gamesPerMonth = (player.gamesCount / 6).toFixed(1) // Supposons 6 mois d'activité
  const averageGameDuration = 90 // En minutes, valeur fictive
  const preferredPartners = ["Sophie Leclerc", "Thomas Dubois", "Marie Martin"]

  return (
    <div className="space-y-6">
      <Button variant="ghost" className="mb-4 flex items-center gap-2" onClick={() => router.back()}>
        <ArrowLeft className="h-4 w-4" />
        Retour à la liste
      </Button>

      <div className="flex flex-col gap-4 md:flex-row">
        <Card className="flex-1">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-2xl font-semibold text-primary">
                  {player.name.charAt(0)}
                </div>
                <div>
                  <CardTitle className="text-2xl">{player.name}</CardTitle>
                  <CardDescription className="flex items-center gap-2">
                    {player.isMember && (
                      <Badge variant="outline" className="bg-green-50 text-green-700">
                        Adhérent
                      </Badge>
                    )}
                    <span>Membre depuis {player.memberSince || "Janvier 2023"}</span>
                  </CardDescription>
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-muted-foreground" />
                <span>{player.email}</span>
              </div>
              <div className="flex items-center gap-2">
                <Phone className="h-4 w-4 text-muted-foreground" />
                <span>{player.phone || "Non spécifié"}</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Dépenses totales</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center">
              <Euro className="mr-2 h-4 w-4 text-muted-foreground" />
              <span className="text-2xl font-bold">{totalSpent} €</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Parties jouées</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center">
              <Calendar className="mr-2 h-4 w-4 text-muted-foreground" />
              <span className="text-2xl font-bold">{player.gamesCount}</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Fréquence mensuelle</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center">
              <Activity className="mr-2 h-4 w-4 text-muted-foreground" />
              <span className="text-2xl font-bold">{gamesPerMonth} parties/mois</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Durée moyenne</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center">
              <Clock className="mr-2 h-4 w-4 text-muted-foreground" />
              <span className="text-2xl font-bold">{averageGameDuration} min</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="activity">
        <TabsList>
          <TabsTrigger value="activity">Activité</TabsTrigger>
          <TabsTrigger value="games">Parties</TabsTrigger>
          <TabsTrigger value="partners">Partenaires préférés</TabsTrigger>
        </TabsList>
        <TabsContent value="activity" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Activité du joueur</CardTitle>
              <CardDescription>Évolution des parties jouées au cours des 6 derniers mois</CardDescription>
            </CardHeader>
            <CardContent>
              <PlayerActivityChart playerId={player.id} />
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="games" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Historique des parties</CardTitle>
              <CardDescription>Liste des dernières parties jouées</CardDescription>
            </CardHeader>
            <CardContent>
              <PlayerGamesList games={playerGames} />
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="partners" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Partenaires préférés</CardTitle>
              <CardDescription>Joueurs avec qui {player.name} joue le plus souvent</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {preferredPartners.map((partner, index) => (
                  <div key={index} className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                        {partner.charAt(0)}
                      </div>
                      <span>{partner}</span>
                    </div>
                    <Badge variant="outline">{Math.floor(Math.random() * 10) + 1} parties ensemble</Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
