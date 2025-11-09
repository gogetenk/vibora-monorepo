"use client"

import type React from "react"

import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Alert, AlertDescription } from "@/components/ui/alert"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, Plus, Pencil, Trash2, Clock } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { mockCourts, type Court } from "@/lib/mock-data"
import { toast } from "@/components/ui/use-toast"

export default function ClubCourts() {
  const [courts, setCourts] = useState<Court[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [currentCourt, setCurrentCourt] = useState<Court>({
    id: "",
    club_id: "mock-club-id",
    name: "",
    is_indoor: false,
    description: "",
    price_member: 0,
    price_non_member: 0,
    single_price: true,
    opening_time: "08:00",
    closing_time: "22:00",
    slot_duration: 60,
    surface_type: "",
    access_code: "",
  })
  const [isEditing, setIsEditing] = useState(false)

  useEffect(() => {
    const fetchCourts = async () => {
      try {
        // Simuler un délai de chargement
        await new Promise((resolve) => setTimeout(resolve, 500))

        // Utiliser les données mockées
        setCourts(mockCourts)
      } catch (error) {
        console.error("Erreur:", error)
        setError("Une erreur est survenue")
      } finally {
        setIsLoading(false)
      }
    }

    fetchCourts()
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setCurrentCourt((prev) => ({
      ...prev,
      [name]: ["price_member", "price_non_member", "slot_duration"].includes(name) ? Number.parseFloat(value) : value,
    }))
  }

  const handleSwitchChange = (name: string, checked: boolean) => {
    setCurrentCourt((prev) => ({ ...prev, [name]: checked }))
  }

  const handleSelectChange = (name: string, value: string) => {
    setCurrentCourt((prev) => ({ ...prev, [name]: name === "slot_duration" ? Number.parseInt(value) : value }))
  }

  const handleAddCourt = () => {
    setIsEditing(false)
    setCurrentCourt({
      id: "",
      club_id: "mock-club-id",
      name: "",
      is_indoor: false,
      description: "",
      price_member: 0,
      price_non_member: 0,
      single_price: true,
      opening_time: "08:00",
      closing_time: "22:00",
      slot_duration: 60,
      surface_type: "",
      access_code: "",
    })
    setIsDialogOpen(true)
  }

  const handleEditCourt = (court: Court) => {
    setIsEditing(true)
    setCurrentCourt(court)
    setIsDialogOpen(true)
  }

  const handleDeleteCourt = async (id: string) => {
    if (!confirm("Êtes-vous sûr de vouloir supprimer ce terrain ?")) return

    try {
      // Simuler un délai de traitement
      await new Promise((resolve) => setTimeout(resolve, 300))

      setCourts(courts.filter((court) => court.id !== id))
      setSuccess("Terrain supprimé avec succès")

      toast({
        title: "Terrain supprimé",
        description: "Le terrain a été supprimé avec succès",
      })
    } catch (error: any) {
      console.error("Erreur lors de la suppression:", error)
      setError(error.message || "Erreur lors de la suppression du terrain")
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)
    setError(null)
    setSuccess(null)

    try {
      // Simuler un délai de traitement
      await new Promise((resolve) => setTimeout(resolve, 800))

      if (isEditing) {
        // Mise à jour d'un terrain existant
        setCourts(courts.map((court) => (court.id === currentCourt.id ? currentCourt : court)))
        toast({
          title: "Terrain mis à jour",
          description: "Le terrain a été mis à jour avec succès",
        })
      } else {
        // Ajout d'un nouveau terrain
        const newCourt = {
          ...currentCourt,
          id: `new-${Date.now()}`,
          club_id: "mock-club-id",
          price_non_member: currentCourt.single_price ? currentCourt.price_member : currentCourt.price_non_member,
        }
        setCourts([...courts, newCourt])
        toast({
          title: "Terrain ajouté",
          description: "Le terrain a été ajouté avec succès",
        })
      }

      setSuccess(isEditing ? "Terrain mis à jour avec succès" : "Terrain ajouté avec succès")
      setIsDialogOpen(false)
    } catch (error: any) {
      console.error("Erreur lors de la sauvegarde:", error)
      setError(error.message || "Erreur lors de la sauvegarde du terrain")
    } finally {
      setIsSaving(false)
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
          <h1 className="text-2xl font-bold">Terrains</h1>
          <p className="text-gray-500">Gérez les terrains de votre club</p>
        </div>
        <Button onClick={handleAddCourt}>
          <Plus className="mr-2 h-4 w-4" />
          Ajouter un terrain
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {success && (
        <Alert className="border-green-500 bg-green-50 text-green-700">
          <AlertDescription>{success}</AlertDescription>
        </Alert>
      )}

      {courts.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center p-12">
            <div className="mb-4 rounded-full bg-gray-100 p-3">
              <Plus className="h-6 w-6 text-gray-500" />
            </div>
            <p className="mb-2 text-sm font-medium">Aucun terrain</p>
            <p className="mb-4 text-center text-xs text-gray-500">
              Vous n'avez pas encore ajouté de terrains à votre club.
              <br />
              Commencez par ajouter votre premier terrain.
            </p>
            <Button onClick={handleAddCourt}>Ajouter un terrain</Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {courts.map((court) => (
            <Card key={court.id}>
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle>{court.name}</CardTitle>
                  <Badge variant={court.is_indoor ? "default" : "outline-solid"}>
                    {court.is_indoor ? "Indoor" : "Outdoor"}
                  </Badge>
                </div>
                <CardDescription>{court.description || "Aucune description"}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-sm text-gray-500">Prix:</p>
                  <div className="text-right">
                    <p className="text-lg font-bold text-primary">
                      {court.price_member}€ <span className="text-sm font-normal text-gray-500">/heure</span>
                    </p>
                    {!court.single_price && (
                      <p className="text-sm text-gray-600">
                        {court.price_non_member}€{" "}
                        <span className="text-xs font-normal text-gray-500">/non-adhérent</span>
                      </p>
                    )}
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1 text-sm text-gray-500">
                    <Clock className="h-4 w-4" />
                    <span>Horaires:</span>
                  </div>
                  <span className="text-sm">
                    {court.opening_time} - {court.closing_time}
                  </span>
                </div>
                {court.surface_type && (
                  <div className="flex items-center justify-between">
                    <p className="text-sm text-gray-500">Surface:</p>
                    <span className="text-sm">{court.surface_type}</span>
                  </div>
                )}
              </CardContent>
              <CardFooter className="flex justify-between border-t bg-gray-50 px-6 py-3">
                <Button variant="outline" size="sm" onClick={() => handleEditCourt(court)}>
                  <Pencil className="mr-2 h-4 w-4" />
                  Modifier
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="text-red-500 hover:bg-red-50 hover:text-red-600"
                  onClick={() => handleDeleteCourt(court.id)}
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Supprimer
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{isEditing ? "Modifier le terrain" : "Ajouter un terrain"}</DialogTitle>
            <DialogDescription>
              {isEditing
                ? "Modifiez les informations de votre terrain"
                : "Renseignez les informations de votre nouveau terrain"}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleSubmit}>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="name">Nom du terrain</Label>
                <Input
                  id="name"
                  name="name"
                  value={currentCourt.name}
                  onChange={handleChange}
                  placeholder="Ex: Terrain 1, Court Central..."
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  name="description"
                  value={currentCourt.description}
                  onChange={handleChange}
                  placeholder="Description du terrain (optionnel)"
                  rows={3}
                />
              </div>

              <div className="space-y-4">
                <Label>Tarification</Label>
                <div className="flex items-center space-x-2 mb-2">
                  <Switch
                    id="single_price"
                    checked={currentCourt.single_price}
                    onCheckedChange={(checked) => handleSwitchChange("single_price", checked)}
                  />
                  <Label htmlFor="single_price">Tarif unique</Label>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="price_member">
                      {currentCourt.single_price ? "Prix par heure (€)" : "Prix adhérent (€)"}
                    </Label>
                    <Input
                      id="price_member"
                      name="price_member"
                      type="number"
                      min="0"
                      step="0.01"
                      value={currentCourt.price_member}
                      onChange={handleChange}
                      required
                    />
                  </div>

                  {!currentCourt.single_price && (
                    <div className="space-y-2">
                      <Label htmlFor="price_non_member">Prix non-adhérent (€)</Label>
                      <Input
                        id="price_non_member"
                        name="price_non_member"
                        type="number"
                        min="0"
                        step="0.01"
                        value={currentCourt.price_non_member}
                        onChange={handleChange}
                        required
                      />
                    </div>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label>Horaires réservables</Label>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="opening_time">Heure d'ouverture</Label>
                    <Input
                      id="opening_time"
                      name="opening_time"
                      type="time"
                      value={currentCourt.opening_time}
                      onChange={handleChange}
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="closing_time">Heure de fermeture</Label>
                    <Input
                      id="closing_time"
                      name="closing_time"
                      type="time"
                      value={currentCourt.closing_time}
                      onChange={handleChange}
                      required
                    />
                  </div>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="slot_duration">Durée des créneaux</Label>
                <Select
                  value={currentCourt.slot_duration.toString()}
                  onValueChange={(value) => handleSelectChange("slot_duration", value)}
                >
                  <SelectTrigger id="slot_duration">
                    <SelectValue placeholder="Sélectionner une durée" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="30">30 minutes</SelectItem>
                    <SelectItem value="60">1 heure</SelectItem>
                    <SelectItem value="90">1 heure 30</SelectItem>
                    <SelectItem value="120">2 heures</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="surface_type">Type de surface (facultatif)</Label>
                <Select
                  value={currentCourt.surface_type}
                  onValueChange={(value) => handleSelectChange("surface_type", value)}
                >
                  <SelectTrigger id="surface_type">
                    <SelectValue placeholder="Sélectionner un type de surface" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Non spécifié</SelectItem>
                    <SelectItem value="Moquette">Moquette</SelectItem>
                    <SelectItem value="Béton poreux">Béton poreux</SelectItem>
                    <SelectItem value="Gazon synthétique">Gazon synthétique</SelectItem>
                    <SelectItem value="Résine">Résine</SelectItem>
                    <SelectItem value="Terre battue">Terre battue</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="access_code">Code d'accès (facultatif)</Label>
                <Input
                  id="access_code"
                  name="access_code"
                  value={currentCourt.access_code}
                  onChange={handleChange}
                  placeholder="Ex: 1234# ou instructions d'accès"
                />
              </div>

              <div className="flex items-center space-x-2">
                <Switch
                  id="is_indoor"
                  checked={currentCourt.is_indoor}
                  onCheckedChange={(checked) => handleSwitchChange("is_indoor", checked)}
                />
                <Label htmlFor="is_indoor">Terrain indoor</Label>
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                Annuler
              </Button>
              <Button type="submit" disabled={isSaving}>
                {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                {isEditing ? "Mettre à jour" : "Ajouter"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
