"use client"

import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent } from "@/components/ui/card"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Loader2, Search, CalendarIcon, Plus } from "lucide-react"
import { format } from "date-fns"
import { fr } from "date-fns/locale"
import { NewReservationForm } from "@/components/club/new-reservation-form"
import { ReservationDetailsModal } from "@/components/club/reservation-details-modal"
import { toast } from "@/components/ui/use-toast"
import { mockReservations, mockCourts, type Reservation } from "@/lib/mock-data"

export default function ClubReservations() {
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [filteredReservations, setFilteredReservations] = useState<Reservation[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState("")
  const [selectedStatus, setSelectedStatus] = useState<string>("all")
  const [selectedCourt, setSelectedCourt] = useState<string>("all")
  const [startDate, setStartDate] = useState<string>(new Date().toISOString().split("T")[0])
  const [endDate, setEndDate] = useState<string>("")
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const [selectedReservation, setSelectedReservation] = useState<Reservation | null>(null)
  const [courts, setCourts] = useState<{ id: string; name: string }[]>([])
  const [isNewReservationOpen, setIsNewReservationOpen] = useState(false)
  const [date, setDate] = useState<Date>(new Date())

  useEffect(() => {
    const fetchReservations = async () => {
      try {
        // Simuler un délai de chargement
        await new Promise((resolve) => setTimeout(resolve, 500))

        // Utiliser les données mockées
        setReservations(mockReservations)
        setFilteredReservations(mockReservations)

        // Récupérer les terrains
        setCourts(mockCourts.map((court) => ({ id: court.id, name: court.name })))
      } catch (error) {
        console.error("Erreur:", error)
        setError("Une erreur est survenue lors du chargement des réservations")
      } finally {
        setIsLoading(false)
      }
    }

    fetchReservations()
  }, [])

  useEffect(() => {
    // Filtrer les réservations en fonction des critères
    let filtered = [...reservations]

    // Filtre par recherche (nom de joueur)
    if (searchQuery) {
      filtered = filtered.filter((reservation) =>
        reservation.players.some((player) => player.name.toLowerCase().includes(searchQuery.toLowerCase())),
      )
    }

    // Filtre par statut
    if (selectedStatus !== "all") {
      filtered = filtered.filter((reservation) => reservation.status === selectedStatus)
    }

    // Filtre par terrain
    if (selectedCourt !== "all") {
      filtered = filtered.filter((reservation) => reservation.court_name === selectedCourt)
    }

    // Filtre par date de début
    if (startDate) {
      filtered = filtered.filter((reservation) => reservation.date >= startDate)
    }

    // Filtre par date de fin
    if (endDate) {
      filtered = filtered.filter((reservation) => reservation.date <= endDate)
    }

    setFilteredReservations(filtered)
  }, [reservations, searchQuery, selectedStatus, selectedCourt, startDate, endDate])

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

    if (selectedReservation?.id === id) {
      setSelectedReservation({ ...selectedReservation, status: "cancelled" })
    }

    toast({
      title: "Réservation annulée",
      description: "La réservation a été annulée avec succès",
    })
  }

  const handleRefundReservation = (id: string) => {
    // Simuler un appel API pour le remboursement
    const updatedReservations = reservations.map((reservation) =>
      reservation.id === id ? { ...reservation, refunded: true } : reservation,
    )

    setReservations(updatedReservations)

    if (selectedReservation?.id === id) {
      setSelectedReservation({ ...selectedReservation, refunded: true })
    }

    toast({
      title: "Remboursement effectué",
      description: `Le montant a été remboursé avec succès`,
    })
  }

  const formatDate = (dateString: string | undefined | null) => {
    if (!dateString) return "Non spécifié"
    const date = new Date(dateString)
    return format(date, "dd MMMM yyyy", { locale: fr })
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "confirmed":
        return <Badge className="bg-green-100 text-green-800 hover:bg-green-200">Confirmée</Badge>
      case "pending":
        return <Badge className="bg-yellow-100 text-yellow-800 hover:bg-yellow-200">En attente</Badge>
      case "cancelled":
        return <Badge className="bg-red-100 text-red-800 hover:bg-red-200">Annulée</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  const getPaymentStatusBadge = (status: string, refunded = false) => {
    if (refunded) {
      return <Badge className="bg-purple-100 text-purple-800 hover:bg-purple-200">Remboursé</Badge>
    }

    switch (status) {
      case "paid":
        return <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-200">Payé</Badge>
      case "pending":
        return <Badge className="bg-orange-100 text-orange-800 hover:bg-orange-200">En attente</Badge>
      case "free":
        return <Badge className="bg-gray-100 text-gray-800 hover:bg-gray-200">Gratuit</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Réservations</h1>
          <p className="text-gray-500">Gérez les réservations de vos terrains</p>
        </div>
        <Button onClick={() => setIsNewReservationOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Nouvelle réservation
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardContent className="p-6">
          <div className="mb-6 space-y-4">
            <div className="flex flex-col gap-4 md:flex-row">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                <Input
                  placeholder="Rechercher un joueur..."
                  className="pl-10"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
              <div className="flex flex-1 gap-2">
                <div className="w-1/2">
                  <Label htmlFor="start-date" className="sr-only">
                    Date de début
                  </Label>
                  <div className="relative">
                    <CalendarIcon className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                    <Input
                      id="start-date"
                      type="date"
                      className="pl-10"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                </div>
                <div className="w-1/2">
                  <Label htmlFor="end-date" className="sr-only">
                    Date de fin
                  </Label>
                  <div className="relative">
                    <CalendarIcon className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                    <Input
                      id="end-date"
                      type="date"
                      className="pl-10"
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>
            <div className="flex flex-col gap-4 md:flex-row">
              <div className="w-full md:w-1/2">
                <Label htmlFor="court-filter" className="sr-only">
                  Terrain
                </Label>
                <Select value={selectedCourt} onValueChange={setSelectedCourt}>
                  <SelectTrigger id="court-filter" className="w-full">
                    <SelectValue placeholder="Tous les terrains" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tous les terrains</SelectItem>
                    {courts.map((court) => (
                      <SelectItem key={court.id} value={court.name}>
                        {court.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="w-full md:w-1/2">
                <Label htmlFor="status-filter" className="sr-only">
                  Statut
                </Label>
                <Select value={selectedStatus} onValueChange={setSelectedStatus}>
                  <SelectTrigger id="status-filter" className="w-full">
                    <SelectValue placeholder="Tous les statuts" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tous les statuts</SelectItem>
                    <SelectItem value="confirmed">Confirmée</SelectItem>
                    <SelectItem value="pending">En attente</SelectItem>
                    <SelectItem value="cancelled">Annulée</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead>Heure</TableHead>
                  <TableHead>Terrain</TableHead>
                  <TableHead>Joueurs</TableHead>
                  <TableHead>Statut</TableHead>
                  <TableHead>Paiement</TableHead>
                  <TableHead>Prix</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredReservations.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center py-4">
                      Aucune réservation trouvée
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredReservations.map((reservation) => (
                    <TableRow
                      key={reservation.id}
                      className="cursor-pointer hover:bg-gray-50"
                      onClick={() => handleViewDetails(reservation)}
                    >
                      <TableCell>{formatDate(reservation.date)}</TableCell>
                      <TableCell>{`${reservation.start_time} - ${reservation.end_time}`}</TableCell>
                      <TableCell>{reservation.court_name}</TableCell>
                      <TableCell>
                        <div className="flex items-center">
                          <span>{`${reservation.players.length}/4`}</span>
                          <div className="ml-2 flex -space-x-2">
                            {reservation.players.slice(0, 3).map((player, index) => (
                              <div
                                key={index}
                                className="flex h-6 w-6 items-center justify-center rounded-full bg-gray-200 text-xs font-medium"
                                title={player.name}
                              >
                                {player.name.charAt(0)}
                              </div>
                            ))}
                            {reservation.players.length > 3 && (
                              <div className="flex h-6 w-6 items-center justify-center rounded-full bg-gray-300 text-xs font-medium">
                                +{reservation.players.length - 3}
                              </div>
                            )}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>{getStatusBadge(reservation.status)}</TableCell>
                      <TableCell>{getPaymentStatusBadge(reservation.payment_status, reservation.refunded)}</TableCell>
                      <TableCell>
                        <span className={reservation.refunded ? "line-through text-gray-500" : ""}>
                          {reservation.payment_amount > 0 ? `${reservation.payment_amount}€` : "-"}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {/* Utilisation du nouveau composant de détails de réservation */}
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
