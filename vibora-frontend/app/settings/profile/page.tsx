"use client"

import type React from "react"
import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { motion } from "framer-motion"
import { Camera, Edit, Mail, Calendar, Loader2, AlertCircle, ArrowLeft, WifiOff } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import { useToast } from "@/components/ui/use-toast"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import { viboraApi } from "@/lib/api/vibora-client"
import type { UserProfileDto } from "@/lib/api/vibora-types"
import { useUserProfile } from "@/lib/hooks/use-game-data"
import { FADE_IN_ANIMATION_VARIANTS, STAGGER_CONTAINER_VARIANTS } from "@/lib/animation-variants"

// Skill Level Badge Component (réutilisable)
interface SkillLevelBadgeProps {
  level?: number
  size?: "sm" | "md" | "lg"
  showLabel?: boolean
}

function SkillLevelBadge({ level, size = "md", showLabel = true }: SkillLevelBadgeProps) {
  const getLevelLabel = (lvl?: number) => {
    if (!lvl) return "Non défini"
    if (lvl <= 3) return "Débutant"
    if (lvl <= 6) return "Intermédiaire"
    return "Avancé"
  }

  const sizes = {
    sm: "w-8 h-8 text-sm",
    md: "w-11 h-11 text-base",
    lg: "w-14 h-14 text-xl",
  }

  return (
    <div className="flex items-center gap-3">
      <div className={`${sizes[size]} rounded-xl flex items-center justify-center font-bold bg-primary/10 text-primary border border-primary/20`}>
        {level || "-"}
      </div>
      {showLabel && (
        <span className="text-sm font-medium text-foreground/80">
          {getLevelLabel(level)}
        </span>
      )}
    </div>
  )
}

export default function ProfilePage() {
  const router = useRouter()
  const { toast } = useToast()
  
  // Use offline-first hook
  const { data: profileData, isLoading, isOffline, refresh } = useUserProfile()
  
  const [isMounted, setIsMounted] = useState(false)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [editData, setEditData] = useState({
    firstName: "",
    lastName: "",
    skillLevel: 5,
    bio: "",
  })
  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  
  // Avoid hydration errors
  useEffect(() => {
    setIsMounted(true)
  }, [])

  // Update edit data when profile loads
  useEffect(() => {
    if (profileData) {
      setEditData({
        firstName: profileData.firstName || "",
        lastName: profileData.lastName || "",
        skillLevel: profileData.skillLevel || 5,
        bio: profileData.bio || "",
      })
      
      // Load photo preview from profile
      if (profileData.profilePhotoUrl) {
        setPhotoPreview(profileData.profilePhotoUrl)
      }
    }
  }, [profileData])

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      setPhotoFile(file)
      const reader = new FileReader()
      reader.onloadend = () => {
        setPhotoPreview(reader.result as string)
      }
      reader.readAsDataURL(file)
    }
  }

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)

    try {
      // Créer FormData pour upload avec photo (multipart/form-data)
      const formData = new FormData()
      formData.append("FirstName", editData.firstName)
      formData.append("LastName", editData.lastName)
      formData.append("SkillLevel", editData.skillLevel.toString())
      formData.append("Bio", editData.bio || "")
      if (photoFile) {
        formData.append("Photo", photoFile)
      }

      // Récupérer le JWT depuis Supabase
      const { session } = await (await import("@/lib/auth/supabase-auth")).getSession()
      if (!session?.access_token) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: "Session expirée",
        })
        return
      }

      // Appel direct à l'API avec FormData (multipart/form-data)
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_VIBORA_API_URL}/users/profile`,
        {
          method: "PUT",
          headers: {
            Authorization: `Bearer ${session.access_token}`,
            // Ne pas définir Content-Type - le navigateur le fera automatiquement pour FormData
          },
          body: formData,
        }
      )

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || `HTTP ${response.status}`)
      }

      // Refresh profile data from API to update cache
      await refresh()
      setPhotoFile(null)

      toast({
        title: "Succès",
        description: "Profil mis à jour avec succès",
      })
      setIsEditModalOpen(false)
    } catch (err) {
      console.error("Error saving profile:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: err instanceof Error ? err.message : "Une erreur s'est produite",
      })
    } finally {
      setIsSaving(false)
    }
  }

  // Show loader while mounting or loading to avoid hydration errors
  if (!isMounted || isLoading) {
    return (
      <VPage>
        <VHeader>
          <VContainer>
            <div className="flex items-center gap-4 h-16">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => router.back()}
                className="rounded-full hover:bg-muted/60 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold tracking-tight">Mon Profil</h1>
            </div>
          </VContainer>
        </VHeader>
        <VMain className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </VMain>
      </VPage>
    )
  }

  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-4">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => router.back()}
                className="rounded-full hover:bg-muted/60 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
              </Button>
              <h1 className="text-xl font-bold tracking-tight">Mon Profil</h1>
            </div>
            {isOffline && (
              <Badge variant="outline" className="text-xs text-muted-foreground border-muted-foreground/30 flex items-center gap-1.5">
                <WifiOff className="w-3 h-3" />
                Hors ligne
              </Badge>
            )}
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
            {/* Profile Avatar - Élégant */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <div className="flex justify-center">
                <div className="relative">
                  {(() => {
                    const photoUrl = photoPreview || profileData?.profilePhotoUrl
                    if (photoUrl) {
                      const fullUrl = photoPreview
                        ? photoPreview
                        : `${process.env.NEXT_PUBLIC_VIBORA_API_URL}${profileData?.profilePhotoUrl}`
                      return (
                        <Avatar className="w-28 h-28 ring-2 ring-background shadow-lg">
                          <AvatarImage src={fullUrl} alt="Profile" />
                          <AvatarFallback className="text-2xl font-bold">
                            {(profileData?.firstName?.[0] || "").toUpperCase()}
                            {(profileData?.lastName?.[0] || "").toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                      )
                    }
                    return (
                      <Avatar className="w-28 h-28 ring-2 ring-background shadow-lg">
                        <AvatarFallback className="text-2xl font-bold bg-gradient-to-br from-primary/20 via-primary/10 to-primary/5 text-primary">
                          {(profileData?.firstName?.[0] || "").toUpperCase()}
                          {(profileData?.lastName?.[0] || "").toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                    )
                  })()}
                  <button
                    type="button"
                    onClick={() => document.getElementById("photo-upload")?.click()}
                    className="absolute bottom-0 right-0 w-9 h-9 bg-primary/10 rounded-full flex items-center justify-center hover:bg-primary/20 transition-colors border-2 border-background shadow-md"
                  >
                    <Camera className="w-4 h-4 text-primary" />
                  </button>
                  <input
                    id="photo-upload"
                    type="file"
                    accept="image/*"
                    onChange={handlePhotoChange}
                    className="hidden"
                  />
                </div>
              </div>
            </motion.div>

            {/* Main Profile Card - Design moderne */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="max-w-2xl mx-auto w-full">
              <div className="border border-border/50 rounded-2xl bg-card/50 backdrop-blur-sm shadow-sm p-6">
                <div className="flex items-center justify-between mb-6">
                  <h2 className="text-lg font-bold">Informations personnelles</h2>
                  <Button
                    onClick={() => setIsEditModalOpen(true)}
                    variant="ghost"
                    size="sm"
                    className="text-primary hover:text-primary/80 hover:bg-primary/10"
                  >
                    <Edit className="w-4 h-4 mr-1.5" />
                    Modifier
                  </Button>
                </div>

                {isLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="w-6 h-6 animate-spin text-primary" />
                  </div>
                ) : profileData ? (
                  <div className="space-y-6">
                    {/* Nom complet */}
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Prénom</Label>
                        <p className="text-foreground font-semibold mt-1.5">{profileData.firstName || "-"}</p>
                      </div>
                      <div>
                        <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Nom</Label>
                        <p className="text-foreground font-semibold mt-1.5">{profileData.lastName || "-"}</p>
                      </div>
                    </div>

                    {/* Divider */}
                    <div className="border-t border-border/40" />

                    {/* Niveau de jeu */}
                    <div>
                      <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Niveau de jeu</Label>
                      <div className="mt-2">
                        <SkillLevelBadge level={profileData.skillLevel ?? undefined} />
                      </div>
                    </div>

                    {/* Biographie */}
                    {profileData.bio && (
                      <div>
                        <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Biographie</Label>
                        <p className="text-foreground/80 text-sm mt-1.5 leading-relaxed">{profileData.bio}</p>
                      </div>
                    )}

                    {/* Divider */}
                    <div className="border-t border-border/40" />

                    {/* Stats */}
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Membre depuis</Label>
                        <div className="flex items-center mt-1.5">
                          <Calendar className="w-3.5 h-3.5 text-muted-foreground mr-1.5" />
                          <p className="text-foreground font-medium text-sm">
                            {profileData.memberSince ? new Date(profileData.memberSince).toLocaleDateString("fr-FR") : "-"}
                          </p>
                        </div>
                      </div>
                      <div>
                        <Label className="text-muted-foreground/70 text-xs font-medium uppercase tracking-wide">Parties jouées</Label>
                        <p className="text-foreground font-bold text-lg mt-1">{profileData.gamesPlayedCount || 0}</p>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-destructive py-4">
                    <AlertCircle className="w-5 h-5" />
                    <span className="text-sm">Impossible de charger le profil</span>
                  </div>
                )}
              </div>
            </motion.div>
          </VStack>
        </motion.div>
      </VMain>

      {/* Edit Modal */}
      <Dialog open={isEditModalOpen} onOpenChange={setIsEditModalOpen}>
        <DialogContent className="bg-card border-border">
          <DialogHeader>
            <DialogTitle className="text-foreground">Modifier le profil</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSave} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="firstName" className="text-foreground">
                  Prénom
                </Label>
                <Input
                  id="firstName"
                  value={editData.firstName}
                  onChange={(e) => setEditData({ ...editData, firstName: e.target.value })}
                  className="bg-background border-border text-foreground"
                />
              </div>
              <div>
                <Label htmlFor="lastName" className="text-foreground">
                  Nom
                </Label>
                <Input
                  id="lastName"
                  value={editData.lastName}
                  onChange={(e) => setEditData({ ...editData, lastName: e.target.value })}
                  className="bg-background border-border text-foreground"
                />
              </div>
            </div>

            <div>
              <Label htmlFor="skillLevel" className="text-foreground">
                Niveau de jeu
              </Label>
              <Select
                value={editData.skillLevel.toString()}
                onValueChange={(value) => setEditData({ ...editData, skillLevel: parseInt(value) })}
              >
                <SelectTrigger className="bg-background border-border text-foreground">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-card border-border">
                  {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((level) => (
                    <SelectItem key={level} value={level.toString()}>
                      Niveau {level}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="bio" className="text-foreground">
                Biographie
              </Label>
              <Textarea
                id="bio"
                value={editData.bio}
                onChange={(e) => setEditData({ ...editData, bio: e.target.value })}
                placeholder="Parlez-nous de vous..."
                maxLength={150}
                className="bg-background border-border text-foreground"
              />
              <p className="text-xs text-muted-foreground mt-1">{editData.bio.length}/150</p>
            </div>

            <div>
              <Label htmlFor="photo" className="text-foreground">
                Photo de profil
              </Label>
              <Input
                id="photo"
                type="file"
                accept="image/*"
                onChange={handlePhotoChange}
                className="bg-background border-border text-foreground"
              />
              {photoPreview && (
                <Avatar className="w-20 h-20 mt-2 border-2 border-border">
                  <AvatarImage src={photoPreview} alt="Preview" />
                </Avatar>
              )}
            </div>

            <DialogFooter>
              <Button type="button" variant="ghost" onClick={() => setIsEditModalOpen(false)}>
                Annuler
              </Button>
              <Button
                type="submit"
                disabled={isSaving}
                className="bg-success hover:bg-success/80 text-success-foreground font-medium"
              >
                {isSaving ? "Enregistrement..." : "Enregistrer"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </VPage>
  )
}
