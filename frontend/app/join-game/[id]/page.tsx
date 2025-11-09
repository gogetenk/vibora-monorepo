"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardFooter } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Calendar, Clock, MapPin, Users, ArrowLeft, Share2, AlertTriangle, CreditCard, Info, Star } from "lucide-react"
import Header from "@/components/header"
import { motion } from "framer-motion"
import { Separator } from "@/components/ui/separator"
import { toast } from "@/components/ui/use-toast"
import { Alert, AlertDescription } from "@/components/ui/alert"

export default function JoinGamePage() {
  const params = useParams()
  const router = useRouter()
  const gameId = params.id as string

  const [game, setGame] = useState<any>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isJoining, setIsJoining] = useState(false)

  // Simuler le chargement des données du jeu
  useEffect(() => {
    const fetchGame = async () => {
      setIsLoading(true)

      // Simuler un appel API
      setTimeout(() => {
        setGame({
          id: gameId,
          status: "open",
          date: "2025-04-25",
          time: "19:00",
          endTime: "20:00",
          level: 5,
          price: 25,
          description: "Partie amicale, tous niveaux bienvenus dans la plage indiquée. Bonne ambiance garantie !",
          club: {
            id: "1",
            name: "Club Padel Paris",
            address: "123 Avenue des Sports, 75001 Paris",
            distance: 2.5,
            indoor: true,
            rating: 4.7,
            facilities: ["Vestiaires", "Douches", "Bar", "Parking gratuit"],
          },
          creator: {
            id: "user-1",
            name: "Thomas Dubois",
            avatar: "/placeholder.svg?height=40&width=40",
            level: 5,
            gamesPlayed: 42,
          },
          players: [
            {
              id: "user-1",
              name: "Thomas Dubois",
              avatar: "/placeholder.svg?height=40&width=40",
              level: 5,
              status: "confirmed",
              isCreator: true,
            },
            {
              id: "user-2",
              name: "Sophie Martin",
              avatar: "/placeholder.svg?height=40&width=40",
              level: 4,
              status: "confirmed",
              isCreator: false,
            },
          ],
          maxPlayers: 4,
          spotsLeft: 2,
          isFull: false,
        })
        setIsLoading(false)
      }, 1000)
    }

    fetchGame()
  }, [gameId])

  const formatDate = (dateString: string) => {
    const options: Intl.DateTimeFormatOptions = { weekday: "long", day: "numeric", month: "long", year: "numeric" }
    return new Date(dateString).toLocaleDateString("fr-FR", options)
  }

  const handleJoinGame = () => {
    setIsJoining(true)

    // Simuler un appel API pour rejoindre la partie
    setTimeout(() => {
      setIsJoining(false)
      toast({
        title: "Vous avez rejoint la partie !",
        description: "Votre place a été réservée et votre paiement sera traité.",
      })

      // Rediriger vers le flow de paiement
      router.push("/payment-flow")
    }, 1500)
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="container mx-auto px-4 py-6">
          <div className="flex items-center justify-center py-12">
            <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-primary"></div>
          </div>
        </main>
      </div>
    )
  }

  if (!game) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main className="container mx-auto px-4 py-6">
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <AlertTriangle className="mb-4 h-12 w-12 text-amber-500" />
            <h2 className="mb-2 text-xl font-bold">Partie introuvable</h2>
            <p className="mb-6 text-gray-600">Cette partie n'existe pas ou a été supprimée</p>
            <Link href="/">
              <Button>Retour à l'accueil</Button>
            </Link>
          </div>
        </main>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 pb-20">
      <Header />

      <main className="container mx-auto px-4 py-6">
        <div className="mb-6 flex items-center gap-2">
          <Link href="/create-game">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <ArrowLeft className="h-4 w-4" />
              <span className="sr-only">Retour</span>
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Rejoindre une partie</h1>
        </div>

        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="mx-auto max-w-2xl">
          <Card className="overflow-hidden shadow-md">
            {/* En-tête avec statut */}
            <CardHeader className="border-b bg-white py-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  {game.isFull ? (
                    <Badge variant="outline" className="bg-red-50 text-red-700">
                      Complet
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="bg-green-50 text-green-700">
                      {game.spotsLeft} place{game.spotsLeft > 1 ? "s" : ""} disponible{game.spotsLeft > 1 ? "s" : ""}
                    </Badge>
                  )}
                  <Badge variant="outline" className="bg-blue-50 text-blue-700">
                    Niveau {game.level}
                  </Badge>
                </div>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    navigator.clipboard.writeText(`https://padel-app.com/join-game/${gameId}`)
                    toast({
                      title: "Lien copié !",
                      description: "Le lien a été copié dans le presse-papier",
                    })
                  }}
                >
                  <Share2 className="h-4 w-4" />
                  <span className="sr-only">Partager</span>
                </Button>
              </div>
            </CardHeader>

            <CardContent className="p-6">
              {/* Informations principales */}
              <div className="mb-6 space-y-4">
                <div>
                  <h2 className="text-xl font-bold">{game.club.name}</h2>
                  <div className="flex items-center gap-1 text-sm text-gray-600">
                    <div className="flex items-center">
                      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
                      <span className="ml-1">{game.club.rating}</span>
                    </div>
                    <span>•</span>
                    <Badge variant="outline" className="bg-white">
                      {game.club.indoor ? "Indoor" : "Outdoor"}
                    </Badge>
                  </div>
                </div>

                <div className="rounded-lg bg-gray-50 p-4 space-y-3">
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-gray-500" />
                    <span>{formatDate(game.date)}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Clock className="h-4 w-4 text-gray-500" />
                    <span>
                      {game.time} - {game.endTime}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <MapPin className="h-4 w-4 text-gray-500" />
                    <span>{game.club.address}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="h-4 w-4 text-gray-500" />
                    <span>
                      {game.players.length}/{game.maxPlayers} joueurs
                    </span>
                  </div>
                </div>

                {/* Description */}
                {game.description && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500 mb-2">Description</h3>
                    <p className="text-gray-700">{game.description}</p>
                  </div>
                )}

                {/* Équipements du club */}
                <div>
                  <h3 className="text-sm font-medium text-gray-500 mb-2">Équipements du club</h3>
                  <div className="flex flex-wrap gap-2">
                    {game.club.facilities.map((facility: string) => (
                      <Badge key={facility} variant="outline" className="bg-gray-50">
                        {facility}
                      </Badge>
                    ))}
                  </div>
                </div>
              </div>

              <Separator className="my-6" />

              {/* Liste des joueurs */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Joueurs inscrits</h3>
                <div className="space-y-3">
                  {game.players.map((player: any) => (
                    <div
                      key={player.id}
                      className="flex items-center justify-between rounded-lg border bg-white p-3 shadow-xs"
                    >
                      <div className="flex items-center gap-3">
                        <Avatar className="h-10 w-10 border border-gray-200">
                          <AvatarImage src={player.avatar || "/placeholder.svg"} alt={player.name} />
                          <AvatarFallback>{player.name.charAt(0)}</AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-medium">{player.name}</p>
                          <p className="text-sm text-gray-500">Niveau {player.level}</p>
                        </div>
                      </div>
                      {player.isCreator && (
                        <Badge variant="outline" className="bg-primary/10 text-primary">
                          Créateur
                        </Badge>
                      )}
                    </div>
                  ))}

                  {/* Places disponibles */}
                  {Array.from({ length: game.spotsLeft }).map((_, index) => (
                    <div
                      key={`empty-${index}`}
                      className="flex items-center justify-between rounded-lg border border-dashed border-gray-300 bg-gray-50 p-3"
                    >
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full border border-dashed border-gray-300 bg-gray-100">
                          <Users className="h-5 w-5 text-gray-400" />
                        </div>
                        <p className="text-gray-500">Place disponible</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Informations de paiement */}
              <div className="mt-6 rounded-lg bg-primary/5 p-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <CreditCard className="h-5 w-5 text-primary" />
                    <span className="text-base font-medium">Prix par personne</span>
                  </div>
                  <span className="text-lg font-bold text-primary">{game.price}€</span>
                </div>
                <p className="mt-2 text-sm text-gray-600">
                  Le paiement sera débité le jour du match. En cas d'annulation avant H-24, aucun frais ne sera
                  appliqué.
                </p>
                <div className="mt-3 flex items-center gap-1 text-xs text-blue-600">
                  <Info className="h-3 w-3" />
                  <Link href="/terms" className="underline">
                    Voir les conditions générales
                  </Link>
                </div>
              </div>

              {/* Message si la partie est complète */}
              {game.isFull && (
                <Alert variant="destructive" className="mt-6 bg-red-50">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>
                    Cette partie est complète. Vous pouvez chercher d'autres parties disponibles ou créer votre propre
                    partie.
                  </AlertDescription>
                </Alert>
              )}
            </CardContent>

            <CardFooter className="border-t bg-gray-50 p-6">
              <div className="w-full space-y-3">
                <Button
                  className="w-full rounded-lg bg-primary py-6 text-sm font-medium shadow-md transition-transform hover:-translate-y-px active:translate-y-px"
                  onClick={handleJoinGame}
                  disabled={game.isFull || isJoining}
                >
                  {isJoining ? (
                    <>
                      <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent"></div>
                      Traitement en cours...
                    </>
                  ) : (
                    `Rejoindre la partie (${game.price}€)`
                  )}
                </Button>
                <Button
                  variant="outline"
                  className="w-full rounded-lg border-gray-300 py-2 text-sm"
                  onClick={() => router.back()}
                >
                  Retour aux résultats
                </Button>
              </div>
            </CardFooter>
          </Card>
        </motion.div>
      </main>
    </div>
  )
}
