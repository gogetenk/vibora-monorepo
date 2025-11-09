"use client"

import { useEffect, useState } from "react"
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Loader2,
  Users,
  Euro,
  CalendarIcon,
  TrendingUp,
  Plus,
  ChevronLeft,
  ChevronRight,
  Search,
  Clock,
  CreditCard,
  RefreshCcw,
  ChevronDown,
} from "lucide-react"
import { NewReservationForm } from "@/components/club/new-reservation-form"
import {
  format,
  startOfWeek,
  endOfWeek,
  eachDayOfInterval,
  addWeeks,
  subWeeks,
  isSameDay,
  isWeekend,
  getDay,
} from "date-fns"
import { fr } from "date-fns/locale"
import { cn } from "@/lib/utils"
import { Input } from "@/components/ui/input"
import { ReservationDetailsModal } from "@/components/club/reservation-details-modal"
import { toast } from "@/components/ui/use-toast"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { mockCourts, mockReservations, type Reservation } from "@/lib/mock-data"

// Heures d'ouverture du club (de 8h à 22h)
const HOURS = Array.from({ length: 15 }, (_, i) => i + 8)

export default function ClubDashboard() {
  const [club, setClub] = useState<{ name: string } | null>({ name: "Padel Club Paris" })
  const [isLoading, setIsLoading] = useState(true)
  const [stats, setStats] = useState({
    totalReservations: 156,
    upcomingReservations: 23,
    occupancyRate: 78,
    revenue: 3450,
  })
  const [currentDate, setCurrentDate] = useState<Date>(new Date())
  const [currentWeek, setCurrentWeek] = useState<Date[]>([])
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [courts, setCourts] = useState(mockCourts)
  const [selectedCourtId, setSelectedCourtId] = useState<string>("")
  const [isNewReservationOpen, setIsNewReservationOpen] = useState(false)
  const [selectedReservation, setSelectedReservation] = useState<Reservation | null>(null)
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState("")
  const [calendarView, setCalendarView] = useState<"week" | "day">("week")
  const [selectedDay, setSelectedDay] = useState<Date>(new Date())

  useEffect(() => {
    // Calculer les jours de la semaine courante
    const start = startOfWeek(currentDate, { weekStartsOn: 1 }) // Commence le lundi
    const end = endOfWeek(currentDate, { weekStartsOn: 1 }) // Finit le dimanche
    const days = eachDayOfInterval({ start, end })
    setCurrentWeek(days)
  }, [currentDate])

  useEffect(() => {
    const fetchData = async () => {
      try {
        // Simuler un délai de chargement
        await new Promise((resolve) => setTimeout(resolve, 500))

        // Utiliser les données mockées
        setReservations(mockReservations)

        // Sélectionner le premier terrain par défaut
        if (mockCourts.length > 0 && !selectedCourtId) {
          setSelectedCourtId(mockCourts[0].id)
        }
      } catch (error) {
        console.error("Erreur:", error)
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [selectedCourtId])

  // Générer des données synthétiques pour le week-end
  const generateWeekendReservations = (courts: typeof mockCourts, currentWeek: Date[]) => {
    const weekendReservations: Reservation[] = []
    const weekendDays = currentWeek.filter((day) => isWeekend(day))

    if (weekendDays.length === 0) return []

    // Créer des réservations pour chaque terrain et chaque jour du week-end
    courts.forEach((court) => {
      weekendDays.forEach((day) => {
        const dayStr = format(day, "yyyy-MM-dd")
        const isSaturday = getDay(day) === 6

        // Créer des réservations pour différentes plages horaires
        const timeSlots = [
          { start: "09:00", end: "10:30" },
          { start: "10:30", end: "12:00" },
          { start: "12:00", end: "13:30" },
          { start: "13:30", end: "15:00" },
          { start: "15:00", end: "16:30" },
          { start: "16:30", end: "18:00" },
          { start: "18:00", end: "19:30" },
          { start: "19:30", end: "21:00" },
        ]

        // Garantir que presque tous les créneaux sont réservés le week-end
        timeSlots.forEach((slot, index) => {
          // Forcer plus de réservations le week-end (90% de chance d'avoir une réservation)
          if (Math.random() < 0.9) {
            const playerCount = Math.floor(Math.random() * 3) + 2 // 2 à 4 joueurs
            const players = Array.from({ length: playerCount }, (_, i) => {
              const names = [
                "Jean Dupont",
                "Marie Martin",
                "Thomas Dubois",
                "Sophie Lefebvre",
                "Pierre Durand",
                "Julie Petit",
                "Lucas Bernard",
                "Emma Moreau",
                "Hugo Leroy",
                "Camille Dubois",
                "Léa Roux",
                "Nathan Fournier",
                "Chloé Girard",
                "Théo Lambert",
                "Manon Bonnet",
                "Alexandre Dupuis",
                "Juliette Moreau",
                "Maxime Laurent",
                "Sarah Petit",
                "Nicolas Martin",
              ]

              return {
                name: names[Math.floor(Math.random() * names.length)],
                email: `joueur${i}@example.com`,
                phone: `06 ${Math.floor(10000000 + Math.random() * 90000000)}`,
                is_member: Math.random() > 0.3, // 70% de chance d'être membre
              }
            })

            // Favoriser les statuts "confirmed" pour le week-end
            const status = Math.random() < 0.8 ? "confirmed" : Math.random() < 0.5 ? "pending" : "cancelled"
            const paymentStatus = status === "cancelled" ? "free" : Math.random() < 0.8 ? "paid" : "pending"

            weekendReservations.push({
              id: `weekend-${court.id}-${dayStr}-${slot.start}`,
              date: dayStr,
              start_time: slot.start,
              end_time: slot.end,
              court_name: court.name,
              players,
              status: status as "confirmed" | "pending" | "cancelled",
              payment_status: paymentStatus as "paid" | "pending" | "free",
              payment_method:
                paymentStatus === "paid" ? (["card", "cash", "transfer"][Math.floor(Math.random() * 3)] as any) : null,
              payment_date: paymentStatus === "paid" ? dayStr : null,
              payment_amount: paymentStatus === "free" ? 0 : Math.floor(20 + Math.random() * 15),
              payment_reference: paymentStatus === "paid" ? `REF-${Math.floor(100000 + Math.random() * 900000)}` : null,
              refunded: status === "cancelled" && paymentStatus === "paid" && Math.random() > 0.7,
            })
          }
        })
      })
    })

    return weekendReservations
  }

  useEffect(() => {
    // Générer des réservations supplémentaires pour le week-end
    const weekendReservations = generateWeekendReservations(courts, currentWeek)

    // Combiner les réservations existantes avec les réservations du week-end
    setReservations((prev) => [...prev, ...weekendReservations])
  }, [currentWeek, courts])

  const nextWeek = () => {
    setCurrentDate(addWeeks(currentDate, 1))
  }

  const prevWeek = () => {
    setCurrentDate(subWeeks(currentDate, 1))
  }

  const goToToday = () => {
    setCurrentDate(new Date())
    setSelectedDay(new Date())
  }

  const handleDayClick = (day: Date) => {
    setSelectedDay(day)
    setCalendarView("day")
  }

  const getReservationsForDay = (day: Date, courtId: string) => {
    const dayStr = format(day, "yyyy-MM-dd")
    return reservations.filter((res) => {
      // Filtrer par court
      if (courtId && res.court_id !== courtId) {
        return false
      }

      // Filtrer par recherche si nécessaire
      if (searchQuery && !res.players.some((player) => player.name.toLowerCase().includes(searchQuery.toLowerCase()))) {
        return false
      }

      return res.date === dayStr
    })
  }

  const handleViewDetails = (reservation: Reservation) => {
    setSelectedReservation(reservation)
    setIsDetailsOpen(true)
  }

  const handleCancelReservation = (id: string) => {
    // Mettre à jour l'état des réservations
    const updatedReservations = reservations.map((reservation) =>
      reservation.id === id ? { ...reservation, status: "cancelled" as const } : reservation,
    )
    setReservations(updatedReservations)

    // Mettre à jour la réservation sélectionnée si nécessaire
    if (selectedReservation?.id === id) {
      setSelectedReservation({ ...selectedReservation, status: "cancelled" })
    }

    toast({
      title: "Réservation annulée",
      description: "La réservation a été annulée avec succès",
    })
  }

  const handleRefundReservation = (id: string) => {
    // Mettre à jour l'état des réservations
    const updatedReservations = reservations.map((reservation) =>
      reservation.id === id ? { ...reservation, refunded: true } : reservation,
    )
    setReservations(updatedReservations)

    // Mettre à jour la réservation sélectionnée si nécessaire
    if (selectedReservation?.id === id) {
      setSelectedReservation({ ...selectedReservation, refunded: true })
    }

    toast({
      title: "Remboursement effectué",
      description: `Le montant a été remboursé avec succès`,
    })
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case "confirmed":
        return "bg-green-50 border-green-200"
      case "pending":
        return "bg-yellow-50 border-yellow-200"
      case "cancelled":
        return "bg-red-50 border-red-200"
      default:
        return "bg-gray-50 border-gray-200"
    }
  }

  const getStatusBadgeColor = (status: string) => {
    switch (status) {
      case "confirmed":
        return "bg-green-100 text-green-800"
      case "pending":
        return "bg-yellow-100 text-yellow-800"
      case "cancelled":
        return "bg-red-100 text-red-800"
      default:
        return "bg-gray-100 text-gray-800"
    }
  }

  const getPaymentStatusIcon = (status: string, refunded = false) => {
    if (refunded) return <RefreshCcw className="h-3 w-3 text-purple-600" />

    switch (status) {
      case "paid":
        return <CreditCard className="h-3 w-3 text-blue-600" />
      case "pending":
        return <Clock className="h-3 w-3 text-yellow-600" />
      case "free":
        return <Euro className="h-3 w-3 text-gray-600" />
      default:
        return null
    }
  }

  const formatDuration = (startTime: string, endTime: string) => {
    const [startHour, startMinute] = startTime.split(":").map(Number)
    const [endHour, endMinute] = endTime.split(":").map(Number)

    const startMinutes = startHour * 60 + startMinute
    const endMinutes = endHour * 60 + endMinute
    const durationMinutes = endMinutes - startMinutes

    const hours = Math.floor(durationMinutes / 60)
    const minutes = durationMinutes % 60

    if (hours === 0) {
      return `${minutes}min`
    } else if (minutes === 0) {
      return `${hours}h`
    } else {
      return `${hours}h${minutes}`
    }
  }

  // Fonction pour obtenir la position et la hauteur d'une réservation dans la grille
  const getReservationPosition = (startTime: string, endTime: string) => {
    const [startHour, startMinute] = startTime.split(":").map(Number)
    const [endHour, endMinute] = endTime.split(":").map(Number)

    // Calculer la position relative (8h = 0, 22h = 14)
    const startPosition = startHour - 8 + startMinute / 60
    const endPosition = endHour - 8 + endMinute / 60
    const height = endPosition - startPosition

    return {
      top: `${startPosition * 60}px`,
      height: `${height * 60}px`,
    }
  }

  // Fonction pour vérifier si une réservation chevauche une autre
  const checkOverlap = (reservations: Reservation[], currentIndex: number) => {
    const current = reservations[currentIndex]
    const [currentStartHour, currentStartMinute] = current.start_time.split(":").map(Number)
    const [currentEndHour, currentEndMinute] = current.end_time.split(":").map(Number)
    const currentStart = currentStartHour * 60 + currentStartMinute
    const currentEnd = currentEndHour * 60 + currentEndMinute

    let overlapCount = 0
    let position = 0

    for (let i = 0; i < reservations.length; i++) {
      if (i === currentIndex) continue

      const other = reservations[i]
      const [otherStartHour, otherStartMinute] = other.start_time.split(":").map(Number)
      const [otherEndHour, otherEndMinute] = other.end_time.split(":").map(Number)
      const otherStart = otherStartHour * 60 + otherStartMinute
      const otherEnd = otherEndHour * 60 + otherEndMinute

      // Vérifier s'il y a chevauchement
      if (currentStart < otherEnd && currentEnd > otherStart) {
        overlapCount++
        if (i < currentIndex) {
          position++
        }
      }
    }

    return { overlapCount, position }
  }

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  const selectedCourt = courts.find((court) => court.id === selectedCourtId)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Dashboard</h1>
          <p className="text-gray-500">{club?.name ? `Bienvenue, ${club.name}` : "Bienvenue dans votre espace club"}</p>
        </div>
        <Button onClick={() => setIsNewReservationOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Nouvelle réservation
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-500">Réservations à venir</p>
                <h3 className="mt-1 text-2xl font-bold">{stats.upcomingReservations}</h3>
              </div>
              <div className="rounded-full bg-primary/10 p-3 text-primary">
                <CalendarIcon className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-500">Taux d'occupation</p>
                <h3 className="mt-1 text-2xl font-bold">{stats.occupancyRate}%</h3>
              </div>
              <div className="rounded-full bg-green-100 p-3 text-green-600">
                <TrendingUp className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-500">Chiffre d'affaires</p>
                <h3 className="mt-1 text-2xl font-bold">{stats.revenue}€</h3>
              </div>
              <div className="rounded-full bg-blue-100 p-3 text-blue-600">
                <Euro className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-500">Total joueurs</p>
                <h3 className="mt-1 text-2xl font-bold">312</h3>
              </div>
              <div className="rounded-full bg-purple-100 p-3 text-purple-600">
                <Users className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Calendrier */}
      <Card>
        <CardContent className="p-6">
          <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={prevWeek}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" onClick={goToToday}>
                Aujourd'hui
              </Button>
              <Button variant="outline" size="sm" onClick={nextWeek}>
                <ChevronRight className="h-4 w-4" />
              </Button>
              <h2 className="text-lg font-medium">
                {format(currentWeek[0], "d MMMM", { locale: fr })} -{" "}
                {format(currentWeek[6], "d MMMM yyyy", { locale: fr })}
              </h2>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                <Input
                  placeholder="Rechercher un joueur..."
                  className="pl-10 h-9"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm" className="h-9">
                    {selectedCourt ? selectedCourt.name : "Sélectionner un terrain"}
                    <ChevronDown className="ml-2 h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                  {courts.map((court) => (
                    <DropdownMenuItem
                      key={court.id}
                      onClick={() => setSelectedCourtId(court.id)}
                      className={cn(
                        "cursor-pointer",
                        selectedCourtId === court.id && "bg-primary/10 font-medium text-primary",
                      )}
                    >
                      <div className="flex items-center">
                        <div className={`w-3 h-3 rounded-full mr-2 ${court.color?.split(" ")[0] || "bg-gray-300"}`} />
                        {court.name}
                      </div>
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <Tabs value={calendarView} onValueChange={(value) => setCalendarView(value as "week" | "day")}>
                <TabsList className="h-9">
                  <TabsTrigger value="week">Semaine</TabsTrigger>
                  <TabsTrigger value="day">Jour</TabsTrigger>
                </TabsList>
              </Tabs>
            </div>
          </div>

          {calendarView === "week" ? (
            <div className="overflow-x-auto">
              <div className="min-w-[800px]">
                {/* En-tête avec les jours de la semaine */}
                <div className="grid grid-cols-8 border-b">
                  <div className="p-3 font-medium border-r">Heures</div>
                  {currentWeek.map((day, index) => {
                    const isToday = isSameDay(day, new Date())
                    const isWeekendDay = isWeekend(day)
                    return (
                      <div
                        key={index}
                        className={cn(
                          "p-3 text-center cursor-pointer hover:bg-gray-50",
                          isToday && "bg-primary/10 font-medium",
                          isWeekendDay && "bg-amber-50",
                        )}
                        onClick={() => handleDayClick(day)}
                      >
                        <div className="text-sm font-medium">{format(day, "EEE", { locale: fr })}</div>
                        <div className={cn("text-lg", isToday && "font-bold text-primary")}>
                          {format(day, "dd", { locale: fr })}
                        </div>
                        <div className="text-xs text-gray-500">{format(day, "MMM", { locale: fr })}</div>
                      </div>
                    )
                  })}
                </div>

                {/* Corps du calendrier avec les heures en vertical et les jours en horizontal */}
                {HOURS.map((hour) => {
                  return (
                    <div key={hour} className="grid grid-cols-8 border-b last:border-b-0">
                      <div className="p-2 border-r text-center text-sm text-gray-500">{hour}:00</div>
                      {currentWeek.map((day, dayIndex) => {
                        const isToday = isSameDay(day, new Date())
                        const isWeekendDay = isWeekend(day)
                        const dayReservations = getReservationsForDay(day, selectedCourtId).filter((res) => {
                          const [startHour] = res.start_time.split(":").map(Number)
                          return startHour === hour
                        })

                        return (
                          <div
                            key={dayIndex}
                            className={cn(
                              "relative p-1 h-16",
                              isToday && "bg-primary/5",
                              isWeekendDay && "bg-amber-50/30",
                            )}
                          >
                            {dayReservations.map((reservation) => (
                              <div
                                key={reservation.id}
                                className={cn(
                                  "absolute inset-x-1 cursor-pointer rounded-md border p-1 shadow-xs transition-transform hover:-translate-y-px hover:shadow-md overflow-hidden",
                                  getStatusColor(reservation.status),
                                )}
                                style={{
                                  top: "0",
                                  height: "calc(100% - 4px)",
                                }}
                                onClick={() => handleViewDetails(reservation)}
                              >
                                <div className="flex items-center justify-between text-xs">
                                  <span className="font-medium truncate">
                                    {reservation.start_time} - {reservation.end_time}
                                  </span>
                                  <Badge
                                    variant="outline"
                                    className={cn("px-1 py-0.5 text-[10px]", getStatusBadgeColor(reservation.status))}
                                  >
                                    {reservation.players.length}/4
                                  </Badge>
                                </div>
                              </div>
                            ))}
                          </div>
                        )
                      })}
                    </div>
                  )
                })}
              </div>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[600px]">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-lg font-medium">
                    {format(selectedDay, "EEEE d MMMM yyyy", { locale: fr })}
                    {selectedCourt && ` - ${selectedCourt.name}`}
                  </h3>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" onClick={() => setCalendarView("week")}>
                      Retour à la semaine
                    </Button>
                  </div>
                </div>

                {/* Vue détaillée du jour */}
                <div className="relative border rounded-lg min-h-[600px]">
                  {/* Heures */}
                  <div className="absolute left-0 top-0 bottom-0 w-16 border-r bg-gray-50">
                    {HOURS.map((hour) => (
                      <div
                        key={hour}
                        className="h-[60px] border-b last:border-b-0 flex items-center justify-center text-sm text-gray-500"
                      >
                        {hour}:00
                      </div>
                    ))}
                  </div>

                  {/* Grille des réservations */}
                  <div className="ml-16 relative">
                    {/* Lignes horizontales pour les heures */}
                    {HOURS.map((hour, index) => (
                      <div
                        key={hour}
                        className={cn("absolute left-0 right-0 h-px bg-gray-200", index === 0 && "hidden")}
                        style={{ top: `${index * 60}px` }}
                      />
                    ))}

                    {/* Réservations */}
                    {getReservationsForDay(selectedDay, selectedCourtId).map((reservation, index, arr) => {
                      const position = getReservationPosition(reservation.start_time, reservation.end_time)
                      const { overlapCount, position: overlapPosition } = checkOverlap(arr, index)
                      const width = overlapCount > 0 ? `calc((100% - 8px) / ${overlapCount + 1})` : "calc(100% - 8px)"
                      const left = overlapCount > 0 ? `calc(${width} * ${overlapPosition})` : "4px"

                      return (
                        <div
                          key={reservation.id}
                          className={cn(
                            "absolute cursor-pointer rounded-md border p-2 shadow-xs transition-transform hover:-translate-y-px hover:shadow-md",
                            getStatusColor(reservation.status),
                          )}
                          style={{
                            top: position.top,
                            height: position.height,
                            left: left,
                            width: width,
                          }}
                          onClick={() => handleViewDetails(reservation)}
                        >
                          <div className="flex flex-col h-full overflow-hidden">
                            {/* Header with time and status */}
                            <div className="flex items-center gap-1.5 text-sm">
                              <Clock className="h-4 w-4 text-gray-500" />
                              <span className="font-medium">
                                {reservation.start_time} - {reservation.end_time}
                              </span>
                              <span className="text-xs text-gray-500">
                                ({formatDuration(reservation.start_time, reservation.end_time)})
                              </span>
                            </div>

                            {/* Players and payment info */}
                            <div className="flex justify-between items-center mt-1">
                              <div className="flex -space-x-1">
                                {reservation.players.slice(0, 3).map((player, idx) => (
                                  <Avatar key={idx} className="h-6 w-6 border border-white" title={player.name}>
                                    <AvatarFallback className="text-xs bg-gray-100">
                                      {player.name.charAt(0)}
                                    </AvatarFallback>
                                  </Avatar>
                                ))}
                                {reservation.players.length > 3 && (
                                  <div className="flex h-6 w-6 items-center justify-center rounded-full bg-gray-100 text-xs font-medium border border-white">
                                    +{reservation.players.length - 3}
                                  </div>
                                )}
                              </div>

                              <div className="flex items-center gap-1 bg-white/80 px-1.5 py-0.5 rounded-full">
                                {getPaymentStatusIcon(reservation.payment_status, reservation.refunded)}
                                <span
                                  className={cn(
                                    "text-xs font-medium",
                                    reservation.refunded ? "line-through text-gray-500" : "text-gray-700",
                                  )}
                                >
                                  {reservation.payment_amount > 0 ? `${reservation.payment_amount}€` : "Gratuit"}
                                </span>
                              </div>
                            </div>

                            {/* Status badge */}
                            <div className="mt-auto pt-1">
                              <Badge
                                variant="outline"
                                className={cn(
                                  "px-2 py-0.5 text-xs font-medium",
                                  getStatusBadgeColor(reservation.status),
                                )}
                              >
                                {reservation.status === "confirmed"
                                  ? "Confirmé"
                                  : reservation.status === "pending"
                                    ? "En attente"
                                    : "Annulé"}
                              </Badge>
                            </div>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modal de détails de réservation */}
      <ReservationDetailsModal
        isOpen={isDetailsOpen}
        onClose={() => setIsDetailsOpen(false)}
        reservation={selectedReservation}
        onCancelReservation={handleCancelReservation}
        onRefundReservation={handleRefundReservation}
      />

      {/* Formulaire de nouvelle réservation */}
      <NewReservationForm isOpen={isNewReservationOpen} onClose={() => setIsNewReservationOpen(false)} />
    </div>
  )
}
