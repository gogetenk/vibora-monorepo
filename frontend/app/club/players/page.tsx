"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { Search, Filter, ChevronRight, Users, UserCheck, UserMinus, Wallet } from "lucide-react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { mockPlayers } from "@/lib/mock-data"

export default function PlayersPage() {
  const router = useRouter()
  const [searchQuery, setSearchQuery] = useState("")
  const [filteredPlayers, setFilteredPlayers] = useState(mockPlayers)
  const [activeTab, setActiveTab] = useState("all")
  const [filterDialogOpen, setFilterDialogOpen] = useState(false)

  // États pour les filtres
  const [membershipFilter, setMembershipFilter] = useState("all") // "all", "members", "non-members"
  const [gamesCountFilter, setGamesCountFilter] = useState("all") // "all", "0-5", "6-15", "16+"
  const [spentAmountFilter, setSpentAmountFilter] = useState("all") // "all", "0-100", "101-300", "301+"

  useEffect(() => {
    // Filtrer les joueurs en fonction de la recherche, de l'onglet actif et des filtres
    const filtered = mockPlayers.filter((player) => {
      const matchesSearch =
        player.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        player.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (player.phone && player.phone.includes(searchQuery))

      // Filtre par onglet
      if (activeTab === "all") {
        // Pas de filtre supplémentaire
      } else if (activeTab === "members" && !player.isMember) {
        return false
      } else if (activeTab === "active" && player.gamesCount <= 5) {
        return false
      }

      // Filtre par statut d'adhésion
      if (membershipFilter === "members" && !player.isMember) {
        return false
      } else if (membershipFilter === "non-members" && player.isMember) {
        return false
      }

      // Filtre par nombre de parties
      if (gamesCountFilter === "0-5" && (player.gamesCount < 0 || player.gamesCount > 5)) {
        return false
      } else if (gamesCountFilter === "6-15" && (player.gamesCount < 6 || player.gamesCount > 15)) {
        return false
      } else if (gamesCountFilter === "16+" && player.gamesCount < 16) {
        return false
      }

      // Filtre par montant dépensé
      if (spentAmountFilter === "0-100" && (player.spentAmount < 0 || player.spentAmount > 100)) {
        return false
      } else if (spentAmountFilter === "101-300" && (player.spentAmount < 101 || player.spentAmount > 300)) {
        return false
      } else if (spentAmountFilter === "301+" && player.spentAmount < 301) {
        return false
      }

      return matchesSearch
    })

    setFilteredPlayers(filtered)
  }, [searchQuery, activeTab, membershipFilter, gamesCountFilter, spentAmountFilter])

  const handleViewPlayer = (playerId: string) => {
    router.push(`/club/players/${playerId}`)
  }

  const applyFilters = () => {
    setFilterDialogOpen(false)
  }

  const resetFilters = () => {
    setMembershipFilter("all")
    setGamesCountFilter("all")
    setSpentAmountFilter("all")
  }

  // Calcul des indicateurs clés
  const totalPlayers = mockPlayers.length
  const activePlayers = mockPlayers.filter((p) => p.gamesCount > 5).length
  const retentionRate = Math.round((activePlayers / totalPlayers) * 100)
  const churnRate = 100 - retentionRate
  const avgSpent = Math.round(mockPlayers.reduce((sum, p) => sum + p.spentAmount, 0) / totalPlayers)
  const uniquePlayersLastMonth = Math.round(totalPlayers * 0.8) // Simulation: 80% des joueurs ont joué le mois dernier

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <h1 className="text-2xl font-bold tracking-tight">Joueurs</h1>
        <div className="flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Rechercher un joueur..."
              className="w-full pl-8 sm:w-[300px]"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          <Dialog open={filterDialogOpen} onOpenChange={setFilterDialogOpen}>
            <DialogTrigger asChild>
              <Button variant="outline" size="icon">
                <Filter className="h-4 w-4" />
                <span className="sr-only">Filtrer</span>
              </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
              <DialogHeader>
                <DialogTitle>Filtrer les joueurs</DialogTitle>
              </DialogHeader>
              <div className="grid gap-4 py-4">
                <div className="space-y-2">
                  <h3 className="font-medium">Statut d'adhésion</h3>
                  <RadioGroup value={membershipFilter} onValueChange={setMembershipFilter}>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="all" id="all-membership" />
                      <Label htmlFor="all-membership">Tous</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="members" id="members-only" />
                      <Label htmlFor="members-only">Adhérents uniquement</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="non-members" id="non-members-only" />
                      <Label htmlFor="non-members-only">Non-adhérents uniquement</Label>
                    </div>
                  </RadioGroup>
                </div>

                <div className="space-y-2">
                  <h3 className="font-medium">Nombre de parties</h3>
                  <RadioGroup value={gamesCountFilter} onValueChange={setGamesCountFilter}>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="all" id="all-games" />
                      <Label htmlFor="all-games">Tous</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="0-5" id="games-0-5" />
                      <Label htmlFor="games-0-5">0 à 5 parties</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="6-15" id="games-6-15" />
                      <Label htmlFor="games-6-15">6 à 15 parties</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="16+" id="games-16-plus" />
                      <Label htmlFor="games-16-plus">16 parties et plus</Label>
                    </div>
                  </RadioGroup>
                </div>

                <div className="space-y-2">
                  <h3 className="font-medium">Montant dépensé</h3>
                  <RadioGroup value={spentAmountFilter} onValueChange={setSpentAmountFilter}>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="all" id="all-spent" />
                      <Label htmlFor="all-spent">Tous</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="0-100" id="spent-0-100" />
                      <Label htmlFor="spent-0-100">0€ à 100€</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="101-300" id="spent-101-300" />
                      <Label htmlFor="spent-101-300">101€ à 300€</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="301+" id="spent-301-plus" />
                      <Label htmlFor="spent-301-plus">301€ et plus</Label>
                    </div>
                  </RadioGroup>
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={resetFilters}>
                  Réinitialiser
                </Button>
                <DialogClose asChild>
                  <Button onClick={applyFilters}>Appliquer</Button>
                </DialogClose>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Indicateurs clés */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex flex-row items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 text-blue-700">
              <Users className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Joueurs uniques (mois)</p>
              <h3 className="text-2xl font-bold">{uniquePlayersLastMonth}</h3>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-row items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-green-100 text-green-700">
              <UserCheck className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Taux de rétention</p>
              <h3 className="text-2xl font-bold">{retentionRate}%</h3>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-row items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-100 text-red-700">
              <UserMinus className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Taux d'abandon</p>
              <h3 className="text-2xl font-bold">{churnRate}%</h3>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-row items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-amber-100 text-amber-700">
              <Wallet className="h-6 w-6" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Dépense moyenne</p>
              <h3 className="text-2xl font-bold">{avgSpent}€</h3>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="all" className="w-full" onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="all">Tous</TabsTrigger>
          <TabsTrigger value="members">Adhérents</TabsTrigger>
          <TabsTrigger value="active">Joueurs actifs</TabsTrigger>
        </TabsList>
        <TabsContent value="all" className="mt-6">
          <Card>
            <CardHeader className="px-6 py-4">
              <CardTitle className="text-base">{filteredPlayers.length} joueurs</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y">
                {filteredPlayers.map((player) => (
                  <div
                    key={player.id}
                    className="flex cursor-pointer items-center justify-between p-4 hover:bg-muted/50"
                    onClick={() => handleViewPlayer(player.id)}
                  >
                    <div className="flex items-center gap-4">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                        {player.name.charAt(0)}
                      </div>
                      <div>
                        <div className="font-medium">{player.name}</div>
                        <div className="text-sm text-muted-foreground">{player.email}</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {player.isMember && (
                        <Badge variant="outline" className="bg-green-50 text-green-700">
                          Adhérent
                        </Badge>
                      )}
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="members" className="mt-6">
          <Card>
            <CardHeader className="px-6 py-4">
              <CardTitle className="text-base">{filteredPlayers.length} joueurs</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y">
                {filteredPlayers.map((player) => (
                  <div
                    key={player.id}
                    className="flex cursor-pointer items-center justify-between p-4 hover:bg-muted/50"
                    onClick={() => handleViewPlayer(player.id)}
                  >
                    <div className="flex items-center gap-4">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                        {player.name.charAt(0)}
                      </div>
                      <div>
                        <div className="font-medium">{player.name}</div>
                        <div className="text-sm text-muted-foreground">{player.email}</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-green-50 text-green-700">
                        Adhérent
                      </Badge>
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="active" className="mt-6">
          <Card>
            <CardHeader className="px-6 py-4">
              <CardTitle className="text-base">{filteredPlayers.length} joueurs</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y">
                {filteredPlayers.map((player) => (
                  <div
                    key={player.id}
                    className="flex cursor-pointer items-center justify-between p-4 hover:bg-muted/50"
                    onClick={() => handleViewPlayer(player.id)}
                  >
                    <div className="flex items-center gap-4">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                        {player.name.charAt(0)}
                      </div>
                      <div>
                        <div className="font-medium">{player.name}</div>
                        <div className="text-sm text-muted-foreground">{player.email}</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-blue-50 text-blue-700">
                        {player.gamesCount} parties
                      </Badge>
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </div>
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
