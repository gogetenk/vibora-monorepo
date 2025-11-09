"use client"

import { useState } from "react"
import { motion } from "framer-motion"
import { 
  Share2, 
  Copy, 
  MessageCircle, 
  Mail,
  Link2,
  Check,
  QrCode
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "@/components/ui/use-toast"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"

interface MagicLinkGeneratorProps {
  gameId: string
  gameData: {
    date: string
    time: string
    club: {
      name: string
    }
    creator: {
      name: string
    }
    spotsLeft: number
  }
  onClose?: () => void
}

export function MagicLinkGenerator({ gameId, gameData, onClose }: MagicLinkGeneratorProps) {
  const [magicLink, setMagicLink] = useState("")
  const [isGenerating, setIsGenerating] = useState(false)
  const [isCopied, setIsCopied] = useState(false)

  // Générer le Magic Link
  const generateMagicLink = async () => {
    setIsGenerating(true)

    try {
      // Simuler la génération du token sécurisé
      await new Promise(resolve => setTimeout(resolve, 800))
      
      // Dans un vrai cas, on ferait un appel API pour créer le token
      // const response = await fetch('/api/magic-links/generate', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({ gameId, expiresIn: '7d' })
      // })
      
      const token = `mg_${gameId}_${Date.now()}`
      const link = `${window.location.origin}/join/${token}`
      setMagicLink(link)

    } catch (error) {
      toast({
        title: "Erreur",
        description: "Impossible de générer le lien d'invitation",
        variant: "destructive"
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(magicLink)
      setIsCopied(true)
      toast({
        title: "Lien copié !",
        description: "Le lien d'invitation a été copié dans le presse-papier"
      })
      
      setTimeout(() => setIsCopied(false), 2000)
    } catch (error) {
      toast({
        title: "Erreur",
        description: "Impossible de copier le lien",
        variant: "destructive"
      })
    }
  }

  const shareViaWhatsApp = () => {
    const message = `Salut ! ${gameData.creator.name} t'invite à une partie de padel le ${new Date(gameData.date).toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' })} à ${gameData.time} chez ${gameData.club.name}.\n\nPlus que ${gameData.spotsLeft} place${gameData.spotsLeft > 1 ? 's' : ''} !\n\nRéserve ta place : ${magicLink}`
    
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`
    window.open(whatsappUrl, '_blank')
  }

  const shareViaEmail = () => {
    const subject = `Invitation padel - ${gameData.club.name}`
    const body = `Salut !\n\n${gameData.creator.name} t'invite à une partie de padel :\n\n📅 ${new Date(gameData.date).toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' })}\n⏰ ${gameData.time}\n📍 ${gameData.club.name}\n\nPlus que ${gameData.spotsLeft} place${gameData.spotsLeft > 1 ? 's' : ''} disponible${gameData.spotsLeft > 1 ? 's' : ''} !\n\nRéserve ta place en un clic : ${magicLink}\n\nÀ bientôt sur le terrain ! 🎾`
    
    const emailUrl = `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
    window.location.href = emailUrl
  }

  const nativeShare = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: `Partie padel - ${gameData.club.name}`,
          text: `${gameData.creator.name} t'invite à une partie de padel le ${new Date(gameData.date).toLocaleDateString('fr-FR')} à ${gameData.time}`,
          url: magicLink
        })
      } catch (error) {
        // L'utilisateur a annulé ou erreur
      }
    }
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    const today = new Date()
    const tomorrow = new Date(today)
    tomorrow.setDate(tomorrow.getDate() + 1)

    if (date.toDateString() === today.toDateString()) {
      return "Aujourd'hui"
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return "Demain"
    } else {
      return date.toLocaleDateString("fr-FR", { 
        weekday: "long", 
        day: "numeric", 
        month: "long" 
      })
    }
  }

  return (
    <Card className="w-full max-w-lg mx-auto">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Link2 className="w-5 h-5" />
          Inviter des joueurs
        </CardTitle>
        <div className="text-sm text-muted-foreground">
          <p>{formatDate(gameData.date)} à {gameData.time}</p>
          <p>{gameData.club.name}</p>
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {!magicLink ? (
          <div className="text-center space-y-4">
            <div className="space-y-2">
              <h3 className="font-medium">Créer un lien d'invitation</h3>
              <p className="text-sm text-muted-foreground">
                Partagez un lien magique qui permet de rejoindre votre partie sans inscription
              </p>
            </div>

            <div className="bg-primary/5 rounded-lg p-4 space-y-2">
              <h4 className="text-sm font-medium">✨ Magic Link</h4>
              <ul className="text-xs text-muted-foreground space-y-1">
                <li>• Participation en 2 clics, sans inscription</li>
                <li>• Conversion douce vers un compte après</li>
                <li>• Valide 7 jours</li>
                <li>• Partage facile WhatsApp/Email</li>
              </ul>
            </div>

            <Button 
              onClick={generateMagicLink}
              disabled={isGenerating}
              className="w-full"
            >
              {isGenerating ? (
                <>
                  <div className="w-4 h-4 mr-2 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  Génération...
                </>
              ) : (
                <>
                  <Link2 className="w-4 h-4 mr-2" />
                  Générer le lien
                </>
              )}
            </Button>
          </div>
        ) : (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="space-y-6"
          >
            {/* Statut */}
            <div className="flex items-center justify-center gap-2">
              <Badge variant="outline" className="bg-success/10 text-success">
                <Check className="w-3 h-3 mr-1" />
                Lien créé
              </Badge>
            </div>

            {/* Lien généré */}
            <div className="space-y-3">
              <Label htmlFor="magic-link">Lien d'invitation</Label>
              <div className="flex gap-2">
                <Input
                  id="magic-link"
                  value={magicLink}
                  readOnly
                  className="font-mono text-xs"
                />
                <Button
                  onClick={copyToClipboard}
                  variant="outline"
                  size="sm"
                  className="flex-shrink-0"
                >
                  {isCopied ? (
                    <Check className="w-4 h-4 text-success" />
                  ) : (
                    <Copy className="w-4 h-4" />
                  )}
                </Button>
              </div>
            </div>

            <Separator />

            {/* Options de partage */}
            <div className="space-y-4">
              <h4 className="font-medium">Partager l'invitation</h4>
              
              <div className="grid grid-cols-2 gap-3">
                <Button
                  onClick={shareViaWhatsApp}
                  variant="outline"
                  className="h-12 flex-col gap-1"
                >
                  <MessageCircle className="w-5 h-5 text-green-600" />
                  <span className="text-xs">WhatsApp</span>
                </Button>

                <Button
                  onClick={shareViaEmail}
                  variant="outline" 
                  className="h-12 flex-col gap-1"
                >
                  <Mail className="w-5 h-5 text-blue-600" />
                  <span className="text-xs">Email</span>
                </Button>
              </div>

              {navigator.share && (
                <Button
                  onClick={nativeShare}
                  variant="outline"
                  className="w-full"
                >
                  <Share2 className="w-4 h-4 mr-2" />
                  Partager avec...
                </Button>
              )}
            </div>

            {/* Infos importantes */}
            <div className="bg-muted/50 rounded-lg p-4 space-y-2">
              <h5 className="text-sm font-medium">Important</h5>
              <ul className="text-xs text-muted-foreground space-y-1">
                <li>• Ce lien expire dans 7 jours</li>
                <li>• Les invités peuvent participer sans inscription</li>
                <li>• Vous serez notifié des nouvelles inscriptions</li>
                <li>• Maximum {gameData.spotsLeft} participant{gameData.spotsLeft > 1 ? 's' : ''} avec ce lien</li>
              </ul>
            </div>

            {onClose && (
              <Button
                onClick={onClose}
                variant="ghost"
                className="w-full"
              >
                Fermer
              </Button>
            )}
          </motion.div>
        )}
      </CardContent>
    </Card>
  )
}