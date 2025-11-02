"use client"

import { useState } from "react"
import { Share2, Copy, Check, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { useToast } from "@/components/ui/use-toast"
import { viboraApi } from "@/lib/api/vibora-client"

interface ShareGameButtonProps {
  gameId: string
  gameTitle: string // Ex: "Partie de Padel - Demain 19h"
  location: string
  dateTime: string
  variant?: "default" | "outline" | "ghost" | "secondary"
  size?: "default" | "sm" | "lg" | "icon"
  className?: string
}

export function ShareGameButton({
  gameId,
  gameTitle,
  location,
  dateTime,
  variant = "outline",
  size = "default",
  className = "",
}: ShareGameButtonProps) {
  const { toast } = useToast()
  const [isGenerating, setIsGenerating] = useState(false)
  const [shareUrl, setShareUrl] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [isOpen, setIsOpen] = useState(false)

  const generateShareLink = async () => {
    if (shareUrl) return // Already generated

    setIsGenerating(true)
    try {
      const { data, error } = await viboraApi.shares.createGameShare(gameId)

      if (error || !data) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: "Impossible de créer le lien de partage",
        })
        return
      }

      setShareUrl(data.shareUrl)
    } catch (err) {
      console.error("Failed to generate share link:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur s'est produite",
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const handleCopyLink = async () => {
    if (!shareUrl) return

    try {
      await navigator.clipboard.writeText(shareUrl)
      setCopied(true)
      toast({
        title: "✅ Lien copié !",
        description: "Le lien a été copié dans le presse-papier",
      })

      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Impossible de copier le lien",
      })
    }
  }

  const handleNativeShare = async () => {
    if (!shareUrl) return

    // Format date for message
    const date = new Date(dateTime)
    const dateStr = date.toLocaleDateString("fr-FR", {
      weekday: "long",
      day: "numeric",
      month: "long",
    })
    const timeStr = date.toLocaleTimeString("fr-FR", {
      hour: "2-digit",
      minute: "2-digit",
    })

    const message = `🎾 ${gameTitle}

📅 ${dateStr} à ${timeStr}
📍 ${location}

Rejoins la partie ! 👇
${shareUrl}`

    // Try native share API first (mobile)
    if (navigator.share) {
      try {
        await navigator.share({
          title: gameTitle,
          text: message,
          url: shareUrl,
        })
        setIsOpen(false)
      } catch (err: any) {
        // User cancelled or error occurred
        if (err.name !== "AbortError") {
          console.error("Share failed:", err)
          // Fallback to copy
          handleCopyLink()
        }
      }
    } else {
      // Desktop fallback: just copy
      handleCopyLink()
    }
  }

  const handleOpenDialog = async () => {
    setIsOpen(true)
    if (!shareUrl) {
      await generateShareLink()
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button
          variant={variant}
          size={size}
          className={className}
          onClick={handleOpenDialog}
        >
          <Share2 className="h-4 w-4 mr-2" />
          Partager
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Partager la partie</DialogTitle>
          <DialogDescription>
            Invitez vos amis à rejoindre cette partie en partageant ce lien
          </DialogDescription>
        </DialogHeader>

        {isGenerating ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : shareUrl ? (
          <div className="space-y-4">
            {/* Share URL Display */}
            <div className="flex items-center space-x-2">
              <input
                readOnly
                value={shareUrl}
                className="flex-1 rounded-md border border-input bg-background px-3 py-2 text-sm"
              />
              <Button
                size="icon"
                variant="outline"
                onClick={handleCopyLink}
                className="shrink-0"
              >
                {copied ? (
                  <Check className="h-4 w-4 text-green-600" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </div>

            {/* Share Buttons */}
            <div className="flex flex-col gap-2">
              <Button
                onClick={handleNativeShare}
                className="w-full bg-primary hover:bg-primary/90"
              >
                <Share2 className="h-4 w-4 mr-2" />
                Partager via...
              </Button>

              <p className="text-xs text-center text-muted-foreground">
                WhatsApp, SMS, Email, etc.
              </p>
            </div>
          </div>
        ) : (
          <div className="text-center py-4 text-muted-foreground">
            Erreur lors de la génération du lien
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
