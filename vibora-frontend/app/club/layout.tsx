"use client"

import type React from "react"

import { useEffect, useState } from "react"
import { usePathname, useRouter } from "next/navigation"
import { Sidebar } from "@/components/club/sidebar"
import { Loader2 } from "lucide-react"

export default function ClubLayout({ children }: { children: React.ReactNode }) {
  const [isLoading, setIsLoading] = useState(true)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const pathname = usePathname()
  const router = useRouter()

  useEffect(() => {
    // Vérifier si l'utilisateur est connecté
    const checkAuth = async () => {
      try {
        const authData = localStorage.getItem("club_auth")

        if (!authData) {
          // Si on n'est pas sur la page de login, rediriger
          if (pathname !== "/club/login") {
            router.push("/club/login")
          }
          setIsAuthenticated(false)
        } else {
          // Si on est sur la page de login et déjà authentifié, rediriger vers le dashboard
          if (pathname === "/club/login") {
            router.push("/club/dashboard")
          }
          setIsAuthenticated(true)
        }
      } catch (error) {
        console.error("Erreur lors de la vérification de l'authentification:", error)
        if (pathname !== "/club/login") {
          router.push("/club/login")
        }
        setIsAuthenticated(false)
      } finally {
        setIsLoading(false)
      }
    }

    checkAuth()
  }, [pathname, router])

  // Si on est sur la page de login, afficher directement le contenu
  if (pathname === "/club/login") {
    return <>{children}</>
  }

  // Afficher un loader pendant la vérification
  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  // Si non authentifié, ne rien afficher (la redirection est gérée dans l'effet)
  if (!isAuthenticated) {
    return null
  }

  // Si authentifié, afficher le layout avec la sidebar
  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />
      <div className="flex-1 overflow-auto p-6">{children}</div>
    </div>
  )
}
