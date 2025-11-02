"use client"

import { useEffect, useState } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { getSupabaseClient } from "@/lib/supabase-client"
import { Loader2 } from "lucide-react"
import { motion } from "framer-motion"
import { FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"

/**
 * Auth Callback Page
 * Gère la redirection après authentification OAuth ou Magic Link
 */
export default function AuthCallbackPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading")
  const [errorMessage, setErrorMessage] = useState("")

  useEffect(() => {
    const handleAuthCallback = async () => {
      try {
        const supabase = getSupabaseClient()

        // Échanger le code pour une session
        const { data, error } = await supabase.auth.exchangeCodeForSession(
          window.location.search
        )

        if (error) {
          console.error("Auth callback error:", error)
          setStatus("error")
          setErrorMessage(error.message)
          return
        }

        if (data.session) {
          setStatus("success")

          // Récupérer le redirectTo des query params (si disponible)
          const redirectTo = searchParams.get("redirectTo") || "/"

          // Petit délai pour montrer le succès
          setTimeout(() => {
            router.push(redirectTo)
            router.refresh()
          }, 500)
        } else {
          setStatus("error")
          setErrorMessage("Aucune session créée")
        }
      } catch (err) {
        console.error("Unexpected error in auth callback:", err)
        setStatus("error")
        setErrorMessage("Une erreur inattendue s'est produite")
      }
    }

    handleAuthCallback()
  }, [router, searchParams])

  return (
    <div className="min-h-screen flex items-center justify-center p-6 bg-gradient-to-br from-background to-muted/20">
      <motion.div
        variants={FADE_IN_ANIMATION_VARIANTS}
        initial="hidden"
        animate="show"
        className="text-center space-y-4"
      >
        {status === "loading" && (
          <>
            <Loader2 className="h-8 w-8 animate-spin mx-auto text-primary" />
            <p className="text-muted-foreground">Connexion en cours...</p>
          </>
        )}

        {status === "success" && (
          <>
            <div className="h-8 w-8 mx-auto rounded-full bg-success/20 flex items-center justify-center">
              <svg
                className="h-5 w-5 text-success"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <p className="text-muted-foreground">Connexion réussie ! Redirection...</p>
          </>
        )}

        {status === "error" && (
          <>
            <div className="h-8 w-8 mx-auto rounded-full bg-destructive/20 flex items-center justify-center">
              <svg
                className="h-5 w-5 text-destructive"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </div>
            <p className="text-muted-foreground">Erreur de connexion</p>
            <p className="text-sm text-destructive">{errorMessage}</p>
            <button
              onClick={() => router.push("/auth/login")}
              className="mt-4 text-primary hover:underline"
            >
              Retour à la connexion
            </button>
          </>
        )}
      </motion.div>
    </div>
  )
}
