"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { LayoutDashboard, Calendar, Users, Trophy, Settings, LogOut, Menu, X, Home, TrendingUp } from "lucide-react"
import { cn } from "@/lib/utils"

export function Sidebar() {
  const pathname = usePathname()
  const router = useRouter()
  const [clubName, setClubName] = useState("Club de Padel")
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  useEffect(() => {
    // Récupérer le nom du club depuis le localStorage
    try {
      const authData = localStorage.getItem("club_auth")
      if (authData) {
        const { profile } = JSON.parse(authData)
        if (profile && profile.name) {
          setClubName(profile.name)
        }
      }
    } catch (error) {
      console.error("Erreur lors de la récupération du profil:", error)
    }
  }, [])

  const handleSignOut = async () => {
    try {
      // Supprimer les données d'authentification du localStorage
      localStorage.removeItem("club_auth")
      router.push("/club/login")
    } catch (error) {
      console.error("Erreur lors de la déconnexion:", error)
    }
  }

  // Modifier le tableau de navigation pour ajouter l'entrée "Joueurs"
  const navigation = [
    { name: "Dashboard", href: "/club/dashboard", icon: LayoutDashboard },
    { name: "Réservations", href: "/club/reservations", icon: Calendar },
    { name: "Joueurs", href: "/club/players", icon: Users },
    { name: "Terrains", href: "/club/courts", icon: Users },
    { name: "Tournois", href: "/club/tournaments", icon: Trophy },
    { name: "Profil", href: "/club/profile", icon: Settings },
    {
      name: "Yield Management",
      href: "/club/yield-management",
      icon: TrendingUp,
    },
  ]

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen)
  }

  return (
    <>
      {/* Mobile menu button */}
      <div className="fixed left-4 top-4 z-50 block md:hidden">
        <Button variant="outline" size="icon" onClick={toggleMobileMenu}>
          {isMobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </Button>
      </div>

      {/* Sidebar for desktop */}
      <div
        className={cn(
          "fixed inset-y-0 left-0 z-40 w-64 transform bg-white shadow-lg transition-transform duration-200 ease-in-out md:relative md:translate-x-0",
          isMobileMenuOpen ? "translate-x-0" : "-translate-x-full",
        )}
      >
        <div className="flex h-full flex-col">
          <div className="flex h-16 items-center justify-between border-b px-4">
            <div className="text-lg font-bold">{clubName}</div>
            <Link href="/" className="text-gray-500 hover:text-gray-700">
              <Home className="h-5 w-5" />
            </Link>
          </div>
          <div className="flex-1 overflow-y-auto py-4">
            <nav className="space-y-1 px-2">
              {navigation.map((item) => (
                <Link
                  key={item.name}
                  href={item.href}
                  className={cn(
                    "group flex items-center rounded-md px-2 py-2 text-sm font-medium",
                    pathname === item.href
                      ? "bg-primary text-white"
                      : "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
                  )}
                >
                  <item.icon
                    className={cn(
                      "mr-3 h-5 w-5",
                      pathname === item.href ? "text-white" : "text-gray-400 group-hover:text-gray-500",
                    )}
                  />
                  {item.name}
                </Link>
              ))}
            </nav>
          </div>
          <div className="border-t p-4">
            <Button variant="outline" className="flex w-full items-center justify-center" onClick={handleSignOut}>
              <LogOut className="mr-2 h-4 w-4" />
              Déconnexion
            </Button>
          </div>
        </div>
      </div>

      {/* Overlay for mobile */}
      {isMobileMenuOpen && (
        <div className="fixed inset-0 z-30 bg-gray-600 bg-opacity-50 md:hidden" onClick={toggleMobileMenu}></div>
      )}
    </>
  )
}
