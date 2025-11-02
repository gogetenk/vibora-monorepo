"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Loader2 } from "lucide-react"
import { useToast } from "@/components/ui/use-toast"
import { viboraApi } from "@/lib/api/vibora-client"
import { setGuestAuth } from "@/lib/auth/guest-auth"

interface GuestOnboardingModalProps {
  isOpen: boolean
  onClose: () => void
  gameId: string
  gameTitle: string
}

export function GuestOnboardingModal({
  isOpen,
  onClose,
  gameId,
  gameTitle,
}: GuestOnboardingModalProps) {
  const router = useRouter()
  const { toast } = useToast()
  const [isLoading, setIsLoading] = useState(false)
  const [formData, setFormData] = useState({
    name: "",
    phone: "",
    email: "",
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!formData.name.trim()) {
      toast({
        variant: "destructive",
        title: "Nom requis",
        description: "Veuillez entrer votre nom",
      })
      return
    }

    if (!formData.phone && !formData.email) {
      toast({
        variant: "destructive",
        title: "Contact requis",
        description: "Veuillez entrer un numéro de téléphone ou un email",
      })
      return
    }

    setIsLoading(true)

    try {
      // Create guest user via API
      const { data, error } = await viboraApi.users.createGuestUser({
        name: formData.name.trim(),
        skillLevel: 5, // Default: 5 (Intermediate) on 1-10 scale
        phoneNumber: formData.phone.trim() || undefined,
        email: formData.email.trim() || undefined,
      })

      if (error || !data) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error?.message || "Impossible de créer le compte invité",
        })
        return
      }

      // Store guest auth
      setGuestAuth({
        externalId: data.externalId,
        name: data.name,
        skillLevel: data.skillLevel || 5, // Default: 5 (Intermediate)
        token: data.token,
      })

      // Automatically join the game as guest (pass token explicitly)
      const joinResult = await viboraApi.games.joinGameAsGuest(
        gameId,
        {
          name: data.name,
          phoneNumber: data.phoneNumber || null,
          email: data.email || null,
        },
        data.token // Pass the guest token explicitly
      )

      if (joinResult.error) {
        console.error("Failed to join game:", joinResult.error)
        toast({
          variant: "destructive",
          title: "Erreur",
          description: "Impossible de rejoindre la partie",
        })
        return
      }

      toast({
        title: "✅ Bienvenue !",
        description: "Vous avez rejoint la partie avec succès",
      })

      // Redirect to game details
      router.push(`/games/${gameId}`)
    } catch (err) {
      console.error("Failed to create guest user:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur s'est produite",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleLoginRedirect = () => {
    // Store intended destination
    if (typeof window !== "undefined") {
      sessionStorage.setItem("redirect_after_login", `/games/${gameId}`)
    }
    router.push("/login")
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Rejoindre la partie</DialogTitle>
          <DialogDescription>
            Pour rejoindre <strong>{gameTitle}</strong>, créez un compte invité ou connectez-vous
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Nom *</Label>
            <Input
              id="name"
              type="text"
              placeholder="Jean Dupont"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              disabled={isLoading}
              autoFocus
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">Téléphone</Label>
            <Input
              id="phone"
              type="tel"
              placeholder="+33 6 12 34 56 78"
              value={formData.phone}
              onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
              disabled={isLoading}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              placeholder="jean@example.com"
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              disabled={isLoading}
            />
            <p className="text-xs text-muted-foreground">
              * Au moins un contact (téléphone ou email) est requis
            </p>
          </div>

          <div className="flex flex-col gap-2 pt-2">
            <Button
              type="submit"
              disabled={isLoading}
              className="w-full bg-primary hover:bg-primary/90"
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Création...
                </>
              ) : (
                "Continuer en tant qu'invité"
              )}
            </Button>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <span className="w-full border-t" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-background px-2 text-muted-foreground">ou</span>
              </div>
            </div>

            <Button
              type="button"
              variant="outline"
              onClick={handleLoginRedirect}
              disabled={isLoading}
              className="w-full"
            >
              Se connecter avec un compte
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
