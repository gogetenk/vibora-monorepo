"use client"

import { Bell, Search, ArrowLeft } from "lucide-react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { cn } from "@/lib/utils"
import { Avatar, AvatarImage, AvatarFallback } from "@/components/ui/avatar"
import { ThemeToggle } from "@/components/ui/theme-toggle"

interface HeaderProps {
  /**
   * Titre optionnel : si fourni, il s’affiche au centre.
   * S’il n’est pas défini, on reste sur la disposition “avatar à gauche”.
   */
  title?: string
  /**
   * Afficher un bouton retour (←) à gauche.
   * Si `back` est vrai ET `title` undefined, l’avatar est masqué pour éviter les collisions.
   */
  back?: boolean
  /**
   * Masquer l’icône de recherche.
   */
  hideSearch?: boolean
  /**
   * Masquer l’icône de notifications.
   */
  hideNotifications?: boolean
  className?: string
}

export default function Header({ title, back, hideSearch, hideNotifications, className }: HeaderProps) {
  const router = useRouter()

  return (
    <header
      className={cn(
        "sticky top-0 z-20 flex items-center justify-between gap-4 px-4 py-3",
        // Transparent header with subtle backdrop
        "bg-transparent backdrop-blur-sm",
        "border-b border-border/20",
        className,
      )}
    >
      {/* Zone gauche : soit bouton retour, soit avatar */}
      <div className="flex items-center gap-2">
        {back ? (
          <button
            onClick={() => router.back()}
            className="rounded-full p-2 transition-colors hover:bg-accent/50"
            aria-label="Retour"
          >
            <ArrowLeft className="h-4 w-4 text-foreground" />
          </button>
        ) : (
          // Avatar de l’utilisateur
          <Link href="/settings/profile" aria-label="Mon profil">
            <Avatar className="h-8 w-8 ring-1 ring-border/50">
              {/* Remplacez l’URL par l’avatar réel de l’utilisateur si disponible */}
              <AvatarImage src="/woman-profile.png" alt="Avatar utilisateur" className="object-cover" />
              <AvatarFallback>TU</AvatarFallback>
            </Avatar>
          </Link>
        )}
      </div>

      {/* Titre centré facultatif */}
      {title && (
        <h1 className="flex-1 text-center font-display text-base font-semibold tracking-tight text-foreground">{title}</h1>
      )}

      {/* Zone droite : actions */}
      <div className="flex items-center gap-2">
        {!hideSearch && (
          <Link href="/search" className="rounded-full p-2 transition-colors hover:bg-accent/50" aria-label="Rechercher">
            <Search className="h-4 w-4 text-foreground" />
          </Link>
        )}
        {!hideNotifications && (
          <Link
            href="/notifications"
            className="relative rounded-full p-2 transition-colors hover:bg-accent/50"
            aria-label="Notifications"
          >
            <Bell className="h-4 w-4 text-foreground" />
            {/* Badge non-lu : à brancher sur vos données */}
            <span className="absolute -right-0.5 -top-0.5 block h-2 w-2 rounded-full bg-success" />
          </Link>
        )}
        <ThemeToggle />
      </div>
    </header>
  )
}
