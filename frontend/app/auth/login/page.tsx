"use client"

import { useState } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import Link from "next/link"
import { motion } from "framer-motion"
import { Mail, Lock, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { signIn, signInWithOAuth, signInWithMagicLink } from "@/lib/auth/supabase-auth"
import { useToast } from "@/components/ui/use-toast"
import { VPage, VContainer, VContentCard } from "@/components/ui/vibora-layout"
import { FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"
import { GuestOnboardingModal } from "@/components/guest-onboarding-modal"

export default function LoginPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const redirectTo = searchParams.get("redirectTo") || "/"
  const { toast } = useToast()

  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [useMagicLink, setUseMagicLink] = useState(false)
  const [showGuestModal, setShowGuestModal] = useState(false)
  
  // Extract gameId from redirect URL if present (e.g., /games/123)
  const gameId = redirectTo.match(/\/games\/([a-f0-9-]+)/)?.[1] || null

  // ============================================================================
  // Handlers
  // ============================================================================

  const handleEmailPasswordLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)

    try {
      const { error } = await signIn({ email, password })

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur de connexion",
          description: error.message,
        })
        return
      }

      toast({
        title: "Connexion réussie",
        description: "Vous êtes maintenant connecté !",
      })

      router.push(redirectTo)
      router.refresh()
    } catch (err) {
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur inattendue s'est produite.",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleMagicLinkLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)

    try {
      const { error } = await signInWithMagicLink(email)

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message,
        })
        return
      }

      toast({
        title: "Email envoyé !",
        description: "Vérifiez votre boîte mail pour vous connecter.",
      })
    } catch (err) {
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur inattendue s'est produite.",
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleOAuthLogin = async (provider: "google" | "apple") => {
    setIsLoading(true)

    try {
      const { error } = await signInWithOAuth(provider)

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message,
        })
      }
    } catch (err) {
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur inattendue s'est produite.",
      })
    } finally {
      setIsLoading(false)
    }
  }

  // ============================================================================
  // Render
  // ============================================================================

  return (
    <VPage className="flex items-center justify-center p-6">
      <VContainer className="max-w-md">
        <motion.div
          variants={FADE_IN_ANIMATION_VARIANTS}
          initial="hidden"
          animate="show"
        >
          <VContentCard variant="elevated">
            <CardHeader className="space-y-1 text-center">
              <CardTitle className="text-2xl font-bold">Connexion</CardTitle>
              <CardDescription>
                Connectez-vous pour accéder à vos parties
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4">
              {/* OAuth Buttons */}
              <div className="grid grid-cols-2 gap-4">
                <Button
                  variant="outline"
                  onClick={() => handleOAuthLogin("google")}
                  disabled={isLoading}
                  className="w-full"
                >
                  <svg className="mr-2 h-4 w-4" viewBox="0 0 24 24">
                    <path
                      d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                      fill="#4285F4"
                    />
                    <path
                      d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                      fill="#34A853"
                    />
                    <path
                      d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                      fill="#FBBC05"
                    />
                    <path
                      d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                      fill="#EA4335"
                    />
                  </svg>
                  Google
                </Button>

                <Button
                  variant="outline"
                  onClick={() => handleOAuthLogin("apple")}
                  disabled={isLoading}
                  className="w-full"
                >
                  <svg className="mr-2 h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.54 4.09l.01-.01zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z" />
                  </svg>
                  Apple
                </Button>
              </div>

              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <span className="w-full border-t border-border/50" />
                </div>
                <div className="relative flex justify-center text-xs uppercase">
                  <span className="bg-card px-2 text-muted-foreground">Ou</span>
                </div>
              </div>

              {/* Toggle Magic Link / Email+Password */}
              <div className="flex items-center justify-center gap-2 text-sm">
                <Button
                  variant={!useMagicLink ? "default" : "ghost"}
                  size="sm"
                  onClick={() => setUseMagicLink(false)}
                  className="h-8"
                >
                  Email + Mot de passe
                </Button>
                <Button
                  variant={useMagicLink ? "default" : "ghost"}
                  size="sm"
                  onClick={() => setUseMagicLink(true)}
                  className="h-8"
                >
                  Magic Link
                </Button>
              </div>

              {/* Form */}
              <form onSubmit={useMagicLink ? handleMagicLinkLogin : handleEmailPasswordLogin} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="email"
                      type="email"
                      placeholder="votre@email.com"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      disabled={isLoading}
                      className="pl-9"
                    />
                  </div>
                </div>

                {!useMagicLink && (
                  <div className="space-y-2">
                    <Label htmlFor="password">Mot de passe</Label>
                    <div className="relative">
                      <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                      <Input
                        id="password"
                        type="password"
                        placeholder="••••••••"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        disabled={isLoading}
                        className="pl-9"
                      />
                    </div>
                  </div>
                )}

                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {useMagicLink ? "Envoyer le lien magique" : "Se connecter"}
                </Button>
              </form>

              {/* Link to signup */}
              <p className="text-center text-sm text-muted-foreground">
                Pas encore de compte ?{" "}
                <Link href="/auth/signup" className="text-primary hover:underline font-medium">
                  Créer un compte
                </Link>
              </p>

              {/* Continue as guest */}
              <Button
                variant="ghost"
                className="w-full"
                onClick={() => {
                  if (gameId) {
                    // User wants to join a specific game - show guest modal
                    setShowGuestModal(true)
                  } else {
                    // Just browsing - redirect without account
                    router.push(redirectTo)
                  }
                }}
                disabled={isLoading}
              >
                Continuer sans inscription
              </Button>
            </CardContent>
          </VContentCard>
        </motion.div>
      </VContainer>
      
      {/* Guest Onboarding Modal - only show if joining a game */}
      {gameId && (
        <GuestOnboardingModal
          isOpen={showGuestModal}
          onClose={() => setShowGuestModal(false)}
          gameId={gameId}
          gameTitle="cette partie"
        />
      )}
    </VPage>
  )
}
