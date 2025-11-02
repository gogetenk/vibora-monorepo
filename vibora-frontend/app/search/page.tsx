"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { MapPin, ArrowLeft, Search, Filter, ChevronDown, Heart, Clock } from "lucide-react"
import { motion } from "framer-motion"
import { useSearchParams } from "next/navigation"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useNotifications } from "@/hooks/use-notifications"
import { useFavorites } from "@/hooks/use-favorites"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import { VInput } from "@/components/ui/vibora-form"
import { FADE_IN_ANIMATION_VARIANTS, STAGGER_CONTAINER_VARIANTS } from "@/lib/animation-variants"

export default function SearchResults() {
  const searchParams = useSearchParams()
  const query = searchParams.get("query") || ""
  const date = searchParams.get("date") || "Aujourd'hui"

  const [searchQuery, setSearchQuery] = useState(query)
  const [selectedDate, setSelectedDate] = useState(date)
  const [selectedTime, setSelectedTime] = useState("Tous")
  const [selectedPrice, setSelectedPrice] = useState("Tous")
  const [filtersVisible, setFiltersVisible] = useState(false)
  const { addNotification } = useNotifications()
  const { favorites, isFavorite } = useFavorites()

  // Mock data for available courts
  const availableCourts = [
    {
      id: "1",
      name: "Club Padel Paris",
      address: "123 Avenue des Sports, 75001 Paris",
      distance: "2.5 km",
      availableTimes: ["10:00", "14:00", "18:00"],
      price: 25,
      rating: 4.5,
      indoor: true,
    },
    {
      id: "2",
      name: "Urban Padel",
      address: "45 Rue du Sport, 75002 Paris",
      distance: "4 km",
      availableTimes: ["11:00", "15:00", "19:00"],
      price: 22,
      rating: 4.2,
      indoor: false,
    },
    {
      id: "3",
      name: "Padel Factory",
      address: "78 Boulevard des Champions, 75003 Paris",
      distance: "3 km",
      availableTimes: ["09:00", "13:00", "17:00"],
      price: 28,
      rating: 4.7,
      indoor: true,
    },
    {
      id: "4",
      name: "Paris Padel Club",
      address: "15 Rue de la Raquette, 75004 Paris",
      distance: "5.2 km",
      availableTimes: ["12:00", "16:00", "20:00"],
      price: 20,
      rating: 4.0,
      indoor: false,
    },
  ]

  // Trier les clubs avec les favoris en premier
  const sortedCourts = [...availableCourts].sort((a, b) => {
    const aIsFavorite = isFavorite(a.id)
    const bIsFavorite = isFavorite(b.id)

    if (aIsFavorite && !bIsFavorite) return -1
    if (!aIsFavorite && bIsFavorite) return 1

    // Si les deux sont favoris ou aucun n'est favori, trier par distance
    return Number.parseFloat(a.distance) - Number.parseFloat(b.distance)
  })

  useEffect(() => {
    // For demo purposes, show a notification after 3 seconds
    const timer = setTimeout(() => {
      addNotification({
        type: "tournament_reminder",
        title: "Tournoi à venir",
        message: "N'oubliez pas votre tournoi P100 ce weekend",
        time: "Samedi à 10:00",
        location: "Urban Padel",
        actionUrl: "/tournaments/2",
        actionLabel: "Voir les détails",
      })
    }, 3000)

    return () => clearTimeout(timer)
  }, [addNotification])

  return (
    <VPage animate>
      <VHeader sticky>
        <VContainer>
          <div className="flex items-center gap-3 h-16">
            <Link href="/">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="w-5 h-5" />
              </Button>
            </Link>
            <h1 className="text-lg font-semibold">Résultats de recherche</h1>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <motion.div
          variants={STAGGER_CONTAINER_VARIANTS}
          initial="hidden"
          animate="show"
        >
          <VStack spacing="lg">
            {/* Search Bar */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <VInput
                type="text"
                placeholder="Nom du club ou adresse"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                icon={<Search className="w-4 h-4" />}
              />
            </motion.div>

            {/* Filters */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <div className="flex items-center justify-between gap-3">
                <div className="flex-1">
                  <Select value={selectedDate} onValueChange={setSelectedDate}>
                    <SelectTrigger className="h-10 rounded-full border-border bg-card">
                      <div className="flex items-center gap-2">
                        <Clock className="w-4 h-4 text-muted-foreground" />
                        <span className="text-sm">{selectedDate}</span>
                      </div>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Aujourd'hui">Aujourd'hui</SelectItem>
                      <SelectItem value="Demain">Demain</SelectItem>
                      <SelectItem value="Cette semaine">Cette semaine</SelectItem>
                      <SelectItem value="Ce weekend">Ce weekend</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  className="rounded-full"
                  onClick={() => setFiltersVisible(!filtersVisible)}
                >
                  <Filter className="mr-2 h-4 w-4" />
                  Filtres
                  <ChevronDown
                    className={`ml-1 h-4 w-4 transition-transform ${filtersVisible ? "rotate-180" : ""}`}
                  />
                </Button>
              </div>
            </motion.div>

            {/* Advanced Filters */}
            {filtersVisible && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: "auto" }}
                exit={{ opacity: 0, height: 0 }}
                variants={FADE_IN_ANIMATION_VARIANTS}
              >
                <VContentCard className="p-4">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="mb-2 block text-sm font-medium">Horaire</label>
                      <Select value={selectedTime} onValueChange={setSelectedTime}>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder="Tous les horaires" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Tous">Tous les horaires</SelectItem>
                          <SelectItem value="Matin">Matin (8h-12h)</SelectItem>
                          <SelectItem value="Midi">Midi (12h-14h)</SelectItem>
                          <SelectItem value="Après-midi">Après-midi (14h-18h)</SelectItem>
                          <SelectItem value="Soir">Soir (18h-22h)</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium">Prix</label>
                      <Select value={selectedPrice} onValueChange={setSelectedPrice}>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder="Tous les prix" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Tous">Tous les prix</SelectItem>
                          <SelectItem value="Économique">Économique (&lt;20€)</SelectItem>
                          <SelectItem value="Moyen">Moyen (20-25€)</SelectItem>
                          <SelectItem value="Premium">Premium (&gt;25€)</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                  <div className="mt-3 flex justify-end">
                    <Button size="sm">Appliquer</Button>
                  </div>
                </VContentCard>
              </motion.div>
            )}

            {/* Results Count */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <p className="text-sm text-muted-foreground">{sortedCourts.length} terrains trouvés</p>
            </motion.div>

            {/* Search Results */}
            <motion.div variants={STAGGER_CONTAINER_VARIANTS}>
              <VStack spacing="md">
                {sortedCourts.map((court) => {
                  const isCourtFavorite = isFavorite(court.id)

                  return (
                    <motion.div key={court.id} variants={FADE_IN_ANIMATION_VARIANTS}>
                      <Link href={`/courts/${court.id}`}>
                        <VContentCard
                          className={`overflow-hidden transition-all hover:shadow-md ${
                            isCourtFavorite ? "border-2 border-destructive/20 bg-destructive/5" : ""
                          }`}
                        >
                          <div className="relative">
                            <div className="h-32 bg-muted">
                              <img
                                src={`/placeholder.svg?height=128&width=400&text=${court.name}`}
                                alt={court.name}
                                className="h-full w-full object-cover"
                              />
                            </div>
                            <Badge
                              className={`absolute top-2 right-2 ${
                                court.indoor ? "bg-primary" : "bg-success"
                              } text-primary-foreground text-xs`}
                            >
                              {court.indoor ? "Indoor" : "Outdoor"}
                            </Badge>
                            {isCourtFavorite && (
                              <div className="absolute top-2 left-2">
                                <Heart className="h-5 w-5 fill-destructive text-destructive" />
                              </div>
                            )}
                          </div>
                          <CardContent className="p-4">
                            <div className="mb-2 flex items-center justify-between">
                              <div className="flex items-center gap-2">
                                <h3 className="text-base font-semibold">{court.name}</h3>
                                {isCourtFavorite && (
                                  <motion.div initial={{ scale: 0.8 }} animate={{ scale: 1 }}>
                                    <Heart className="h-4 w-4 fill-destructive text-destructive" />
                                  </motion.div>
                                )}
                              </div>
                              <div className="flex items-center gap-1">
                                <span className="text-sm font-bold text-primary">{court.price}€</span>
                                <span className="text-xs text-muted-foreground">/h</span>
                              </div>
                            </div>
                            <div className="mb-2 flex items-center gap-1 text-sm text-muted-foreground">
                              <MapPin className="h-4 w-4" />
                              <span>{court.distance}</span>
                            </div>
                            <div className="mb-3 text-xs text-muted-foreground">{court.address}</div>
                            <div className="flex flex-wrap gap-2">
                              {court.availableTimes.map((time) => (
                                <Button
                                  key={time}
                                  variant="outline"
                                  size="sm"
                                  className="h-8 rounded-lg border-primary/20 bg-primary/5 px-3 text-sm text-primary"
                                >
                                  <Clock className="mr-1 h-3.5 w-3.5" />
                                  {time}
                                </Button>
                              ))}
                            </div>
                          </CardContent>
                        </VContentCard>
                      </Link>
                    </motion.div>
                  )
                })}
              </VStack>
            </motion.div>
          </VStack>
        </motion.div>
      </VMain>
    </VPage>
  )
}
