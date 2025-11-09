"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { createClientComponentClient } from "@supabase/auth-helpers-nextjs"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, Plus, X, AlertCircle } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { toast } from "@/components/ui/use-toast"
import { Alert, AlertDescription } from "@/components/ui/alert"

// Mettre à jour le type de réservation dans le composant NewReservationForm
// Ajouter ces champs dans le type Player (vers le début du fichier)

type Player = {
  id: string
  name: string
  email: string
  is_member: boolean
}

type Court = {
  id: string
  name: string
  price_member: number
  price_non_member: number
  single_price: boolean
}

type NewReservationFormProps = {
  isOpen: boolean
  onClose: () => void
}

export function NewReservationForm({ isOpen, onClose }: NewReservationFormProps) {
  const router = useRouter()
  const supabase = createClientComponentClient()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [courts, setCourts] = useState<Court[]>([])
  const [availableTimes, setAvailableTimes] = useState<string[]>([])
  const [players, setPlayers] = useState<Player[]>([])
  const [selectedPlayers, setSelectedPlayers] = useState<Player[]>([])
  const [newPlayerName, setNewPlayerName] = useState("")
  const [newPlayerEmail, setNewPlayerEmail] = useState("")
  const [totalPrice, setTotalPrice] = useState(0)

  // Form state
  const [date, setDate] = useState<string>(new Date().toISOString().split("T")[0])
  const [startTime, setStartTime] = useState<string>("10:00")
  const [selectedCourt, setSelectedCourt] = useState<string>("")
  const [duration, setDuration] = useState<string>("60")
  const [status, setStatus] = useState<string>("confirmed")
  const [note, setNote] = useState<string>("")
  const [isFree, setIsFree] = useState<boolean>(false)

  // Fetch courts and players on mount
  useEffect(() => {
    const fetchData = async () => {
      try {
        const {
          data: { user },
        } = await supabase.auth.getUser()
        if (!user) return

        // Fetch courts
        const { data: courtsData, error: courtsError } = await supabase
          .from("courts")
          .select("*")
          .eq("club_id", user.id)

        if (courtsError) throw courtsError

        // Mock courts if none exist
        const availableCourts = courtsData?.length
          ? courtsData
          : [
              {
                id: "1",
                name: "Terrain 1",
                price_member: 20,
                price_non_member: 25,
                single_price: false,
              },
              {
                id: "2",
                name: "Terrain 2",
                price_member: 20,
                price_non_member: 25,
                single_price: false,
              },
              {
                id: "3",
                name: "Terrain 3",
                price_member: 15,
                price_non_member: 20,
                single_price: false,
              },
            ]

        setCourts(availableCourts)
        if (availableCourts.length > 0) {
          setSelectedCourt(availableCourts[0].id)
        }

        // Mock players
        const mockPlayers = [
          { id: "1", name: "Jean Dupont", email: "jean@example.com", is_member: true },
          { id: "2", name: "Marie Martin", email: "marie@example.com", is_member: true },
          { id: "3", name: "Thomas Dubois", email: "thomas@example.com", is_member: false },
          { id: "4", name: "Sophie Lefebvre", email: "sophie@example.com", is_member: true },
        ]
        setPlayers(mockPlayers)

        // Generate available times
        generateAvailableTimes()
      } catch (error) {
        console.error("Erreur lors du chargement des données:", error)
        setError("Erreur lors du chargement des données")
      }
    }

    if (isOpen) {
      fetchData()
    }
  }, [isOpen, supabase])

  // Generate available times (8:00 to 22:00)
  const generateAvailableTimes = () => {
    const times = []
    for (let hour = 8; hour <= 22; hour++) {
      times.push(`${hour.toString().padStart(2, "0")}:00`)
      if (hour < 22) {
        times.push(`${hour.toString().padStart(2, "0")}:30`)
      }
    }
    setAvailableTimes(times)
  }

  // Calculate total price when relevant fields change
  useEffect(() => {
    if (!selectedCourt || isFree) {
      setTotalPrice(0)
      return
    }

    const court = courts.find((c) => c.id === selectedCourt)
    if (!court) return

    const durationHours = Number.parseInt(duration) / 60
    let price = 0

    if (selectedPlayers.length === 0) {
      // Default to member price if no players selected
      price = court.price_member * durationHours
    } else {
      // Calculate based on player membership status
      if (court.single_price) {
        price = court.price_member * durationHours
      } else {
        const memberCount = selectedPlayers.filter((p) => p.is_member).length
        const nonMemberCount = selectedPlayers.filter((p) => !p.is_member).length

        // If there are players but not 4, we calculate as if there were 4 players
        // by adding the appropriate number of member-priced slots
        const totalPlayers = selectedPlayers.length
        const additionalMemberSlots = totalPlayers < 4 ? 4 - totalPlayers : 0

        const memberPrice = (memberCount + additionalMemberSlots) * (court.price_member / 4) * durationHours
        const nonMemberPrice = nonMemberCount * (court.price_non_member / 4) * durationHours

        price = memberPrice + nonMemberPrice
      }
    }

    setTotalPrice(price)
  }, [selectedCourt, duration, selectedPlayers, courts, isFree])

  const handleAddPlayer = () => {
    if (!newPlayerName.trim()) return

    const newPlayer: Player = {
      id: `temp-${Date.now()}`,
      name: newPlayerName,
      email: newPlayerEmail,
      is_member: false,
    }

    setSelectedPlayers([...selectedPlayers, newPlayer])
    setNewPlayerName("")
    setNewPlayerEmail("")
  }

  const handleRemovePlayer = (playerId: string) => {
    setSelectedPlayers(selectedPlayers.filter((p) => p.id !== playerId))
  }

  const handleSelectExistingPlayer = (playerId: string) => {
    const player = players.find((p) => p.id === playerId)
    if (!player) return

    // Check if player is already selected
    if (selectedPlayers.some((p) => p.id === playerId)) return

    setSelectedPlayers([...selectedPlayers, player])
  }

  const handleToggleMembership = (playerId: string) => {
    setSelectedPlayers(
      selectedPlayers.map((player) => (player.id === playerId ? { ...player, is_member: !player.is_member } : player)),
    )
  }

  // Modifier la fonction handleSubmit pour inclure le traitement du paiement
  const handleSubmit = async () => {
    setError(null)

    // Validation
    if (!date || !startTime || !selectedCourt) {
      setError("Veuillez remplir tous les champs obligatoires")
      return
    }

    if (selectedPlayers.length === 0) {
      setError("Veuillez ajouter au moins un joueur")
      return
    }

    setIsLoading(true)

    try {
      // Traiter le paiement
      const reservationData = await handlePaymentProcessing()

      // Dans une application réelle, nous sauvegarderions la réservation dans la base de données
      console.log("Réservation créée:", reservationData)

      // Success
      toast({
        title: "Réservation créée",
        description: "La réservation a été créée avec succès",
        variant: "default",
      })

      onClose()
      router.refresh()
    } catch (error) {
      console.error("Erreur lors de la création de la réservation:", error)
      setError("Erreur lors de la création de la réservation")
    } finally {
      setIsLoading(false)
    }
  }

  // Ajouter cette fonction dans le composant pour gérer le paiement
  // Ajouter ceci après la fonction handleSubmit

  const handlePaymentProcessing = async () => {
    // Simuler le traitement du paiement
    setIsLoading(true)

    try {
      // Dans une application réelle, nous ferions une requête à un service de paiement
      await new Promise((resolve) => setTimeout(resolve, 1000))

      // Créer l'objet de réservation
      const reservationData = {
        id: `res-${Date.now()}`,
        date,
        start_time: startTime,
        end_time: calculateEndTime(startTime, Number(duration)),
        court_name: courts.find((c) => c.id === selectedCourt)?.name || "",
        players: selectedPlayers.map((p) => p.name),
        status,
        payment_status: isFree ? "free" : "paid",
        payment_method: isFree ? null : "card", // Par défaut, on suppose un paiement par carte
        payment_date: isFree ? null : new Date().toISOString().split("T")[0],
        payment_amount: isFree ? 0 : totalPrice,
        payment_reference: isFree ? null : `PAY-${Math.floor(Math.random() * 1000000)}`,
        refunded: false,
      }

      return reservationData
    } catch (error) {
      console.error("Erreur lors du traitement du paiement:", error)
      throw new Error("Erreur lors du traitement du paiement")
    } finally {
      setIsLoading(false)
    }
  }

  // Ajouter cette fonction pour calculer l'heure de fin
  const calculateEndTime = (startTime: string, durationMinutes: number) => {
    const [hours, minutes] = startTime.split(":").map(Number)

    const totalMinutes = hours * 60 + minutes + durationMinutes
    const newHours = Math.floor(totalMinutes / 60)
    const newMinutes = totalMinutes % 60

    return `${newHours.toString().padStart(2, "0")}:${newMinutes.toString().padStart(2, "0")}`
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Nouvelle réservation</DialogTitle>
          <DialogDescription>Créez une nouvelle réservation pour un ou plusieurs joueurs</DialogDescription>
        </DialogHeader>

        <div className="grid gap-6 py-4">
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Date et heure */}
            <div className="space-y-2">
              <Label htmlFor="date">
                Date <span className="text-red-500">*</span>
              </Label>
              <Input id="date" type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
            </div>

            <div className="space-y-2">
              <Label htmlFor="start-time">
                Heure de début <span className="text-red-500">*</span>
              </Label>
              <Select value={startTime} onValueChange={setStartTime}>
                <SelectTrigger id="start-time">
                  <SelectValue placeholder="Sélectionner une heure" />
                </SelectTrigger>
                <SelectContent>
                  {availableTimes.map((time) => (
                    <SelectItem key={time} value={time}>
                      {time}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Terrain et durée */}
            <div className="space-y-2">
              <Label htmlFor="court">
                Terrain <span className="text-red-500">*</span>
              </Label>
              <Select value={selectedCourt} onValueChange={setSelectedCourt}>
                <SelectTrigger id="court">
                  <SelectValue placeholder="Sélectionner un terrain" />
                </SelectTrigger>
                <SelectContent>
                  {courts.map((court) => (
                    <SelectItem key={court.id} value={court.id}>
                      {court.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="duration">Durée</Label>
              <Select value={duration} onValueChange={setDuration}>
                <SelectTrigger id="duration">
                  <SelectValue placeholder="Sélectionner une durée" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="60">1 heure</SelectItem>
                  <SelectItem value="90">1 heure 30</SelectItem>
                  <SelectItem value="120">2 heures</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Statut et prix */}
            <div className="space-y-2">
              <Label htmlFor="status">Statut</Label>
              <Select value={status} onValueChange={setStatus}>
                <SelectTrigger id="status">
                  <SelectValue placeholder="Sélectionner un statut" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="confirmed">Confirmée</SelectItem>
                  <SelectItem value="pending">En attente</SelectItem>
                  <SelectItem value="cancelled">Annulée</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="price">Prix total</Label>
                <div className="flex items-center space-x-2">
                  <Switch id="is-free" checked={isFree} onCheckedChange={setIsFree} />
                  <Label htmlFor="is-free" className="text-sm">
                    Gratuit
                  </Label>
                </div>
              </div>
              <div className="h-10 flex items-center px-3 border rounded-md bg-muted/50">
                <span className={isFree ? "line-through text-muted-foreground" : "font-medium"}>
                  {totalPrice.toFixed(2)} €
                </span>
                {isFree && <Badge className="ml-2 bg-green-100 text-green-800">Gratuit</Badge>}
              </div>
            </div>
          </div>

          {/* Joueurs */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <Label>Joueurs ({selectedPlayers.length}/4)</Label>
              <Select value="" onValueChange={handleSelectExistingPlayer}>
                <SelectTrigger className="w-[200px]">
                  <SelectValue placeholder="Ajouter un joueur" />
                </SelectTrigger>
                <SelectContent>
                  {players
                    .filter((p) => !selectedPlayers.some((sp) => sp.id === p.id))
                    .map((player) => (
                      <SelectItem key={player.id} value={player.id}>
                        {player.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </div>

            {/* Liste des joueurs sélectionnés */}
            <div className="space-y-2 max-h-[200px] overflow-y-auto">
              {selectedPlayers.map((player) => (
                <div key={player.id} className="flex items-center justify-between p-3 border rounded-md">
                  <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-primary">
                      {player.name.charAt(0)}
                    </div>
                    <div>
                      <p className="font-medium">{player.name}</p>
                      {player.email && <p className="text-xs text-muted-foreground">{player.email}</p>}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id={`member-${player.id}`}
                        checked={player.is_member}
                        onCheckedChange={() => handleToggleMembership(player.id)}
                      />
                      <Label htmlFor={`member-${player.id}`} className="text-sm">
                        Adhérent
                      </Label>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 p-0 text-muted-foreground hover:text-destructive"
                      onClick={() => handleRemovePlayer(player.id)}
                    >
                      <X className="h-4 w-4" />
                      <span className="sr-only">Supprimer</span>
                    </Button>
                  </div>
                </div>
              ))}

              {selectedPlayers.length === 0 && (
                <div className="flex items-center justify-center p-4 border border-dashed rounded-md">
                  <p className="text-sm text-muted-foreground">Aucun joueur sélectionné</p>
                </div>
              )}
            </div>

            {/* Ajouter un nouveau joueur */}
            <div className="flex gap-2">
              <div className="flex-1">
                <Input
                  placeholder="Nom du joueur"
                  value={newPlayerName}
                  onChange={(e) => setNewPlayerName(e.target.value)}
                />
              </div>
              <div className="flex-1">
                <Input
                  placeholder="Email (optionnel)"
                  type="email"
                  value={newPlayerEmail}
                  onChange={(e) => setNewPlayerEmail(e.target.value)}
                />
              </div>
              <Button type="button" variant="outline" onClick={handleAddPlayer} disabled={!newPlayerName.trim()}>
                <Plus className="h-4 w-4 mr-1" />
                Ajouter
              </Button>
            </div>
          </div>

          {/* Note interne */}
          <div className="space-y-2">
            <Label htmlFor="note">Note interne (optionnel)</Label>
            <Textarea
              id="note"
              placeholder="Ex: Réservation par téléphone"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={2}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Annuler
          </Button>
          <Button onClick={handleSubmit} disabled={isLoading}>
            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Créer la réservation
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
