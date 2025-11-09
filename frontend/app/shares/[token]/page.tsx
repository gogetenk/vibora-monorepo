"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Calendar, Clock, MapPin, Users, Loader2, AlertCircle } from "lucide-react"
import { viboraApi } from "@/lib/api/vibora-client"
import type { GetShareByTokenResponse } from "@/lib/api/vibora-types"
import Header from "@/components/header"
import { useToast } from "@/components/ui/use-toast"
import { getSession } from "@/lib/auth/supabase-auth"
import { isGuestMode } from "@/lib/auth/guest-auth"
import { GuestOnboardingModal } from "@/components/guest-onboarding-modal"

export default function SharePage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const token = params.token as string

  const [shareData, setShareData] = useState<GetShareByTokenResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showGuestModal, setShowGuestModal] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)

  useEffect(() => {
    const init = async () => {
      // Check authentication status
      const { session } = await getSession()
      const hasAuth = !!session || isGuestMode()
      setIsAuthenticated(hasAuth)

      // Fetch share data
      setIsLoading(true)
      setError(null)

      try {
        const { data, error } = await viboraApi.shares.getShareByToken(token)

        if (error || !data) {
          setError(error?.message || "Lien invalide ou expiré")
          return
        }

        setShareData(data)
      } catch (err) {
        console.error("Failed to fetch share:", err)
        setError("Une erreur s'est produite")
      } finally {
        setIsLoading(false)
      }
    }

    init()
  }, [token])

  const handleJoinGame = async () => {
    if (!shareData) return

    // Check if user is authenticated
    const { session } = await getSession()
    const hasAuth = !!session || isGuestMode()

    if (!hasAuth) {
      // User needs to authenticate, show guest modal
      setShowGuestModal(true)
      return
    }

    // User is authenticated, join the game first
    try {
      const { error } = await viboraApi.games.joinGame(shareData.gameId)

      if (error) {
        console.error("Failed to join game:", error)
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message || "Impossible de rejoindre la partie",
        })
        return
      }

      toast({
        title: "✅ Succès",
        description: "Vous avez rejoint la partie !",
      })

      // Redirect to game details
      router.push(`/games/${shareData.gameId}`)
    } catch (err) {
      console.error("Unexpected error:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur s'est produite",
      })
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  if (error || !shareData) {
    return (
      <div className="min-h-screen bg-background">
        <Header />
        <main className="container max-w-md mx-auto py-20 px-4">
          <Card className="border-destructive/50">
            <CardContent className="flex flex-col items-center justify-center py-12 text-center">
              <AlertCircle className="h-12 w-12 text-destructive mb-4" />
              <h2 className="text-xl font-bold mb-2">Lien invalide</h2>
              <p className="text-muted-foreground mb-6">
                {error || "Ce lien de partage n'existe pas ou a expiré"}
              </p>
              <Link href="/">
                <Button>Retour à l'accueil</Button>
              </Link>
            </CardContent>
          </Card>
        </main>
      </div>
    )
  }

  const { game, isExpired } = shareData
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleDateString("fr-FR", {
      weekday: "long",
      day: "numeric",
      month: "long",
    })
  }

  const formatTime = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleTimeString("fr-FR", {
      hour: "2-digit",
      minute: "2-digit",
    })
  }

  return (
    <div className="min-h-screen bg-background pb-20">
      <Header />

      <main className="container max-w-2xl mx-auto py-6 px-4">
        {/* Hero Card */}
        <Card className="overflow-hidden mb-6">
          <div className="relative h-48 bg-gradient-to-br from-primary/20 to-primary/5">
            <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent"></div>
            <div className="absolute bottom-4 left-4 right-4">
              <h1 className="text-2xl font-bold text-white mb-2">
                {game.location}
              </h1>
              <div className="flex items-center gap-4 text-sm text-white/90">
                <div className="flex items-center gap-1">
                  <Calendar className="h-4 w-4" />
                  <span>{formatDate(game.dateTime)}</span>
                </div>
                <div className="flex items-center gap-1">
                  <Clock className="h-4 w-4" />
                  <span>{formatTime(game.dateTime)}</span>
                </div>
              </div>
            </div>
          </div>
        </Card>

        {/* Game Info */}
        <Card className="mb-6">
          <CardContent className="p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-muted-foreground">
                <MapPin className="h-4 w-4" />
                <span className="text-sm">{game.location}</span>
              </div>
              {game.skillLevel && (
                <div className="text-sm font-medium">
                  Niveau {game.skillLevel}
                </div>
              )}
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-muted-foreground">
                <Users className="h-4 w-4" />
                <span className="text-sm">
                  {game.currentPlayers}/{game.maxPlayers} joueurs
                </span>
              </div>
              <div className="text-sm text-success font-medium">
                {game.maxPlayers - game.currentPlayers} place(s) disponible(s)
              </div>
            </div>

            {isExpired && (
              <div className="rounded-lg bg-destructive/10 border border-destructive/20 p-3">
                <p className="text-sm text-destructive font-medium">
                  ⚠️ Ce lien de partage a expiré
                </p>
              </div>
            )}

            {game.status === "Canceled" && (
              <div className="rounded-lg bg-destructive/10 border border-destructive/20 p-3">
                <p className="text-sm text-destructive font-medium">
                  ❌ Cette partie a été annulée
                </p>
              </div>
            )}

            {game.status === "Full" && (
              <div className="rounded-lg bg-warning/10 border border-warning/20 p-3">
                <p className="text-sm text-warning font-medium">
                  😔 Cette partie est complète
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* CTA */}
        <div className="space-y-3">
          {game.status === "Open" && !isExpired ? (
            <>
              <Button
                onClick={handleJoinGame}
                className="w-full bg-primary hover:bg-primary/90 text-primary-foreground font-medium"
                size="lg"
              >
                Voir et rejoindre la partie
              </Button>
              <p className="text-xs text-center text-muted-foreground">
                🎾 {shareData.viewCount} personne(s) ont vu cette partie
              </p>
            </>
          ) : (
            <Link href="/">
              <Button variant="outline" className="w-full" size="lg">
                Voir d'autres parties
              </Button>
            </Link>
          )}
        </div>
      </main>

      {/* Guest Onboarding Modal */}
      {shareData && (
        <GuestOnboardingModal
          isOpen={showGuestModal}
          onClose={() => setShowGuestModal(false)}
          gameId={shareData.gameId}
          gameTitle={shareData.game.location}
        />
      )}
    </div>
  )
}
