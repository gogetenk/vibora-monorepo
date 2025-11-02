"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { MapPin, ArrowLeft, Share2, Star, CheckCircle, Clock, Calendar, Phone, Users, Heart } from "lucide-react"
import Header from "@/components/header"
import { motion, AnimatePresence } from "framer-motion"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useFavorites } from "@/hooks/use-favorites"
import { useToast } from "@/hooks/use-toast"

export default function CourtDetail({ params }: { params: { id: string } }) {
  const [selectedDate, setSelectedDate] = useState("Aujourd'hui")
  const [selectedTime, setSelectedTime] = useState<string | null>(null)
  const { isFavorite, toggleFavorite } = useFavorites()
  const { toast } = useToast()
  const [isFavoriteState, setIsFavoriteState] = useState(false)

  // Mock court data
  const court = {
    id: params.id,
    name: "Club Padel Paris",
    address: "123 Avenue des Sports, 75001 Paris",
    distance: "2.5 km",
    price: 25,
    rating: 4.5,
    indoor: true,
    phone: "01 23 45 67 89",
    facilities: ["Vestiaires", "Douches", "Parking", "Café", "Location de raquettes"],
    description:
      "Le Club Padel Paris propose 6 terrains de padel indoor de qualité professionnelle. Nos terrains sont disponibles à la réservation 7j/7 de 8h à 22h.",
    availableTimes: {
      "Aujourd'hui": ["10:00", "14:00", "18:00", "20:00"],
      Demain: ["09:00", "11:00", "15:00", "17:00", "19:00"],
      "Après-demain": ["10:00", "12:00", "16:00", "18:00", "21:00"],
    },
  }

  // Mock upcoming games data
  const upcomingGames = [
    {
      id: "1",
      date: "Aujourd'hui",
      time: "18:00 - 19:00",
      players: {
        current: 2,
        max: 4,
      },
      level: "Intermédiaire (4-6)",
      price: 25,
    },
    {
      id: "2",
      date: "Demain",
      time: "20:00 - 21:00",
      players: {
        current: 3,
        max: 4,
      },
      level: "Débutant (1-3)",
      price: 25,
    },
    {
      id: "3",
      date: "Après-demain",
      time: "10:00 - 11:00",
      players: {
        current: 1,
        max: 4,
      },
      level: "Avancé (7-10)",
      price: 25,
    },
  ]

  useEffect(() => {
    // Vérifier si le club est dans les favoris au chargement
    setIsFavoriteState(isFavorite(params.id))
  }, [isFavorite, params.id])

  const container = {
    hidden: { opacity: 0 },
    show: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
      },
    },
  }

  const item = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 },
  }

  const handleTimeSelection = (time: string) => {
    setSelectedTime(time === selectedTime ? null : time)
  }

  const handleToggleFavorite = () => {
    const result = toggleFavorite({ id: params.id, name: court.name })
    setIsFavoriteState(!isFavoriteState)

    if (result) {
      toast({
        title: isFavoriteState ? "Retiré des favoris" : "Ajouté aux favoris",
        description: isFavoriteState
          ? `${court.name} a été retiré de vos favoris`
          : `${court.name} a été ajouté à vos favoris`,
        duration: 3000,
      })
    }
  }

  const handleCallClub = () => {
    window.location.href = `tel:${court.phone}`
  }

  return (
    <div className="min-h-screen bg-white pb-24">
      <Header />

      <main className="container mx-auto px-4 py-4">
        <div className="mb-4 flex items-center gap-2">
          <Link href="/search">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <ArrowLeft className="h-4 w-4" />
              <span className="sr-only">Retour</span>
            </Button>
          </Link>
          <h1 className="text-lg font-medium">Détail du terrain</h1>
          <div className="ml-auto flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="h-8 w-8 p-0"
              onClick={handleToggleFavorite}
              aria-label={isFavoriteState ? "Retirer des favoris" : "Ajouter aux favoris"}
            >
              <AnimatePresence mode="wait">
                {isFavoriteState ? (
                  <motion.div
                    key="favorite"
                    initial={{ scale: 0.8 }}
                    animate={{ scale: 1 }}
                    exit={{ scale: 0.8 }}
                    className="text-red-500"
                  >
                    <Heart className="h-5 w-5 fill-red-500 text-red-500" />
                  </motion.div>
                ) : (
                  <motion.div key="not-favorite" initial={{ scale: 0.8 }} animate={{ scale: 1 }} exit={{ scale: 0.8 }}>
                    <Heart className="h-5 w-5 text-gray-400" />
                  </motion.div>
                )}
              </AnimatePresence>
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-8 w-8 p-0"
              onClick={handleCallClub}
              aria-label="Appeler le club"
            >
              <Phone className="h-4 w-4 text-primary" />
            </Button>
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <Share2 className="h-4 w-4" />
              <span className="sr-only">Partager</span>
            </Button>
          </div>
        </div>

        <motion.div variants={container} initial="hidden" animate="show">
          {/* Court Image */}
          <div className="relative mb-4 h-48 overflow-hidden rounded-lg bg-gray-100">
            <img
              src={`/placeholder.svg?height=192&width=400&text=${court.name}`}
              alt={court.name}
              className="h-full w-full object-cover"
            />
            <Badge
              className={`absolute top-3 right-3 ${court.indoor ? "bg-blue-500" : "bg-green-500"} text-white text-sm`}
            >
              {court.indoor ? "Indoor" : "Outdoor"}
            </Badge>
          </div>

          {/* Court Info */}
          <Card className="mb-4 overflow-hidden">
            <CardHeader className="border-b bg-white py-3">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-xl">{court.name}</CardTitle>
                  <div className="flex items-center gap-1 text-sm text-gray-600">
                    <MapPin className="h-4 w-4 text-gray-400" />
                    <span>{court.distance}</span>
                  </div>
                </div>
                <div className="flex items-center gap-1 rounded-lg bg-primary/10 px-2 py-1">
                  <Star className="h-4 w-4 fill-primary text-primary" />
                  <span className="text-sm font-bold text-primary">{court.rating}</span>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-4">
              <motion.div className="space-y-4" variants={container}>
                <motion.div variants={item}>
                  <p className="text-sm text-gray-600">{court.description}</p>
                </motion.div>

                <motion.div variants={item}>
                  <h3 className="mb-2 text-base font-medium">Adresse</h3>
                  <p className="text-sm text-gray-600">{court.address}</p>
                </motion.div>

                <motion.div variants={item}>
                  <h3 className="mb-2 text-base font-medium">Équipements</h3>
                  <div className="flex flex-wrap gap-2">
                    {court.facilities.map((facility) => (
                      <Badge key={facility} variant="outline" className="rounded-lg bg-gray-50 text-sm">
                        <CheckCircle className="mr-1 h-3.5 w-3.5 text-green-500" />
                        {facility}
                      </Badge>
                    ))}
                  </div>
                </motion.div>

                <motion.div variants={item}>
                  <h3 className="mb-2 text-base font-medium">Tarif</h3>
                  <div className="flex items-baseline gap-1">
                    <span className="text-lg font-bold text-primary">{court.price}€</span>
                    <span className="text-sm text-gray-500">/heure</span>
                  </div>
                </motion.div>
              </motion.div>
            </CardContent>
          </Card>

          {/* Upcoming Games */}
          <Card className="mb-4 overflow-hidden">
            <CardHeader className="border-b bg-white py-3">
              <CardTitle className="text-lg">Prochains matches</CardTitle>
            </CardHeader>
            <CardContent className="p-4">
              {upcomingGames.length > 0 ? (
                <div className="space-y-3">
                  {upcomingGames.map((game) => (
                    <div
                      key={game.id}
                      className="rounded-lg border border-gray-200 bg-white p-3 transition-all hover:border-primary/30 hover:shadow-xs"
                    >
                      <div className="mb-2 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Calendar className="h-4 w-4 text-gray-500" />
                          <span className="text-sm font-medium">{game.date}</span>
                        </div>
                        <Badge variant="outline" className="bg-gray-50 text-xs">
                          {game.level}
                        </Badge>
                      </div>
                      <div className="mb-2 flex items-center gap-2">
                        <Clock className="h-4 w-4 text-gray-500" />
                        <span className="text-sm">{game.time}</span>
                      </div>
                      <div className="mb-3 flex items-center gap-2">
                        <Users className="h-4 w-4 text-gray-500" />
                        <span className="text-sm">
                          {game.players.current}/{game.players.max} joueurs
                        </span>
                      </div>
                      <div className="flex items-center justify-between">
                        <div className="text-sm font-medium text-primary">
                          {game.price}€ <span className="text-xs font-normal text-gray-500">par personne</span>
                        </div>
                        <Link href={`/join-game/${game.id}`}>
                          <Button size="sm" className="h-8 rounded-lg text-xs">
                            Rejoindre
                          </Button>
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center py-6 text-center">
                  <div className="mb-2 rounded-full bg-gray-100 p-3">
                    <Calendar className="h-6 w-6 text-gray-400" />
                  </div>
                  <p className="text-sm font-medium text-gray-700">Aucun match à venir</p>
                  <p className="text-xs text-gray-500">Soyez le premier à créer une partie dans ce club</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Availability */}
          <Card className="overflow-hidden">
            <CardHeader className="border-b bg-white py-3">
              <CardTitle className="text-lg">Disponibilités</CardTitle>
            </CardHeader>
            <CardContent className="p-4">
              <Tabs defaultValue="Aujourd'hui" value={selectedDate} onValueChange={setSelectedDate}>
                <TabsList className="mb-4 grid w-full grid-cols-3 rounded-lg p-1">
                  {Object.keys(court.availableTimes).map((date) => (
                    <TabsTrigger
                      key={date}
                      value={date}
                      className="rounded-md text-sm data-[state=active]:bg-primary data-[state=active]:text-white"
                    >
                      {date}
                    </TabsTrigger>
                  ))}
                </TabsList>

                {Object.entries(court.availableTimes).map(([date, times]) => (
                  <TabsContent key={date} value={date} className="mt-0">
                    <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
                      {times.map((time) => (
                        <Button
                          key={time}
                          variant="outline"
                          className={`h-10 rounded-lg text-sm ${
                            selectedTime === time
                              ? "border-primary bg-primary text-white"
                              : "border-gray-200 bg-white text-gray-700"
                          }`}
                          onClick={() => handleTimeSelection(time)}
                        >
                          {time}
                        </Button>
                      ))}
                    </div>
                  </TabsContent>
                ))}
              </Tabs>
            </CardContent>
            <CardFooter className="flex flex-col gap-3 border-t bg-gray-50 p-4">
              <Button
                className="w-full rounded-lg bg-primary py-2.5 text-sm shadow-xs transition-transform hover:-translate-y-px active:translate-y-px"
                disabled={!selectedTime}
              >
                Réserver pour {selectedTime || "..."} ({court.price}€)
              </Button>
            </CardFooter>
          </Card>
        </motion.div>
      </main>
    </div>
  )
}
