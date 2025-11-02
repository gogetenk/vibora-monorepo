"use client"

import type React from "react"

import { useEffect, useState } from "react"
import { createClientComponentClient } from "@supabase/auth-helpers-nextjs"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, Upload } from "lucide-react"

type ClubProfile = {
  id: string
  name: string
  address: string
  city: string
  postal_code: string
  phone: string
  email: string
  website: string
  description: string
  club_type: string
  is_fft_affiliated: boolean
  player_message: string
  hide_contact_info: boolean
  latitude?: number
  longitude?: number
}

export default function ClubProfile() {
  const [profile, setProfile] = useState<ClubProfile>({
    id: "",
    name: "",
    address: "",
    city: "",
    postal_code: "",
    phone: "",
    email: "",
    website: "",
    description: "",
    club_type: "private",
    is_fft_affiliated: false,
    player_message: "",
    hide_contact_info: false,
  })
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const supabase = createClientComponentClient()

  useEffect(() => {
    const fetchClubProfile = async () => {
      try {
        const {
          data: { user },
        } = await supabase.auth.getUser()

        if (!user) return

        // Vérifier si le club existe déjà
        const { data: clubData, error: clubError } = await supabase
          .from("clubs")
          .select("*")
          .eq("id", user.id)
          .maybeSingle() // Utiliser maybeSingle() au lieu de single()

        if (clubError) {
          console.error("Erreur lors de la récupération du profil:", clubError)
          setError("Erreur lors de la récupération de votre profil")
          return
        }

        if (clubData) {
          setProfile({
            ...clubData,
            club_type: clubData.club_type || "private",
            is_fft_affiliated: clubData.is_fft_affiliated || false,
            player_message: clubData.player_message || "",
            hide_contact_info: clubData.hide_contact_info || false,
          })
        } else {
          // Initialiser avec l'ID de l'utilisateur
          setProfile((prev) => ({ ...prev, id: user.id }))
        }
      } catch (error) {
        console.error("Erreur:", error)
        setError("Une erreur est survenue")
      } finally {
        setIsLoading(false)
      }
    }

    fetchClubProfile()
  }, [supabase])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setProfile((prev) => ({ ...prev, [name]: value }))
  }

  const handleSwitchChange = (name: string, checked: boolean) => {
    setProfile((prev) => ({ ...prev, [name]: checked }))
  }

  const handleSelectChange = (name: string, value: string) => {
    setProfile((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)
    setError(null)
    setSuccess(null)

    try {
      const { error } = await supabase.from("clubs").upsert(profile, { onConflict: "id" })

      if (error) {
        throw error
      }

      setSuccess("Profil mis à jour avec succès")
    } catch (error: any) {
      console.error("Erreur lors de la sauvegarde:", error)
      setError(error.message || "Erreur lors de la sauvegarde du profil")
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
      <div>
        <h1 className="text-2xl font-bold">Profil du club</h1>
        <p className="text-gray-500">Gérez les informations de votre club</p>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="grid gap-6 md:grid-cols-2">
          <Card className="col-span-1">
            <CardHeader>
              <CardTitle>Informations générales</CardTitle>
              <CardDescription>Ces informations seront affichées publiquement sur votre page club</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Nom du club</Label>
                <Input
                  id="name"
                  name="name"
                  value={profile.name}
                  onChange={handleChange}
                  placeholder="Nom de votre club"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="club_type">Type de club</Label>
                <Select value={profile.club_type} onValueChange={(value) => handleSelectChange("club_type", value)}>
                  <SelectTrigger id="club_type">
                    <SelectValue placeholder="Sélectionner un type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="private">Privé</SelectItem>
                    <SelectItem value="association">Association</SelectItem>
                    <SelectItem value="municipal">Municipal</SelectItem>
                    <SelectItem value="fft">FFT</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center space-x-2">
                <Switch
                  id="is_fft_affiliated"
                  checked={profile.is_fft_affiliated}
                  onCheckedChange={(checked) => handleSwitchChange("is_fft_affiliated", checked)}
                />
                <Label htmlFor="is_fft_affiliated">Club affilié FFT</Label>
              </div>
              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  name="description"
                  value={profile.description}
                  onChange={handleChange}
                  placeholder="Décrivez votre club, ses installations, etc."
                  rows={4}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="player_message">Message aux joueurs</Label>
                <Textarea
                  id="player_message"
                  name="player_message"
                  value={profile.player_message}
                  onChange={handleChange}
                  placeholder="Message visible sur les confirmations de réservation"
                  rows={3}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="website">Site web</Label>
                <Input
                  id="website"
                  name="website"
                  value={profile.website}
                  onChange={handleChange}
                  placeholder="https://www.votreclub.com"
                />
              </div>
            </CardContent>
          </Card>

          <Card className="col-span-1">
            <CardHeader>
              <CardTitle>Coordonnées</CardTitle>
              <CardDescription>
                Ces informations permettent aux joueurs de vous contacter et de localiser votre club
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="address">Adresse</Label>
                <Input
                  id="address"
                  name="address"
                  value={profile.address}
                  onChange={handleChange}
                  placeholder="Adresse du club"
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="city">Ville</Label>
                  <Input
                    id="city"
                    name="city"
                    value={profile.city}
                    onChange={handleChange}
                    placeholder="Ville"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="postal_code">Code postal</Label>
                  <Input
                    id="postal_code"
                    name="postal_code"
                    value={profile.postal_code}
                    onChange={handleChange}
                    placeholder="Code postal"
                    required
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">Téléphone</Label>
                <Input
                  id="phone"
                  name="phone"
                  value={profile.phone}
                  onChange={handleChange}
                  placeholder="Numéro de téléphone"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email de contact</Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  value={profile.email}
                  onChange={handleChange}
                  placeholder="contact@votreclub.com"
                />
              </div>
              <div className="flex items-center space-x-2">
                <Switch
                  id="hide_contact_info"
                  checked={profile.hide_contact_info}
                  onCheckedChange={(checked) => handleSwitchChange("hide_contact_info", checked)}
                />
                <Label htmlFor="hide_contact_info">Masquer mes informations de contact (téléphone, email)</Label>
              </div>
            </CardContent>
          </Card>
        </div>

        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Photos du club</CardTitle>
            <CardDescription>Ajoutez des photos de votre club pour attirer plus de joueurs</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-gray-300 p-12">
              <div className="mb-4 rounded-full bg-gray-100 p-3">
                <Upload className="h-6 w-6 text-gray-500" />
              </div>
              <p className="mb-2 text-sm font-medium">Glissez-déposez des images ici</p>
              <p className="mb-4 text-xs text-gray-500">PNG, JPG jusqu'à 10MB</p>
              <Button type="button" variant="outline">
                Sélectionner des fichiers
              </Button>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between border-t bg-gray-50 px-6 py-4">
            {error && (
              <Alert variant="destructive" className="mr-4 flex-1">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            {success && (
              <Alert className="mr-4 flex-1 border-green-500 bg-green-50 text-green-700">
                <AlertDescription>{success}</AlertDescription>
              </Alert>
            )}
            <Button type="submit" disabled={isSaving}>
              {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              Enregistrer
            </Button>
          </CardFooter>
        </Card>
      </form>
    </div>
  )
}
