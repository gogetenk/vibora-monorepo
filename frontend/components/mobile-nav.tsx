"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { Home, Calendar, CalendarCheck } from "lucide-react"
import { cn } from "@/lib/utils"
import { PadelBallIcon } from "./icons/padel-ball-icon"
import { motion } from "framer-motion"

const navItems = [
  { href: "/", icon: Home, label: "Accueil" },
  { href: "/my-games", icon: CalendarCheck, label: "Mes Parties" },
]

const NavLink = ({ item, pathname }: { item: (typeof navItems)[0]; pathname: string | null }) => {
  const isActive = item.href === "/" ? pathname === item.href : pathname?.startsWith(item.href)
  const Icon = item.icon
  return (
    <Link href={item.href} className="flex flex-col items-center justify-center w-16 h-16">
      <div className="relative">
        <Icon className={cn("w-6 h-6 transition-colors", isActive ? "text-foreground" : "text-muted-foreground")} />
        {isActive && (
          <motion.div
            layoutId={`active-indicator-${item.href}`}
            className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 w-1 h-1 rounded-full bg-primary"
          />
        )}
      </div>
    </Link>
  )
}

export function MobileNav() {
  const pathname = usePathname()

  const hiddenPaths = ["/payment-flow", "/payment-error", "/club"]
  const shouldHideNavbar = hiddenPaths.some((path) => pathname?.startsWith(path))

  if (shouldHideNavbar) {
    return null
  }

  return (
    <div className="fixed bottom-0 left-0 z-50 w-full h-24 bg-transparent md:hidden">
      <motion.nav
        initial={{ y: 100 }}
        animate={{ y: 0 }}
        transition={{ type: "spring", stiffness: 200, damping: 30, delay: 0.5 }}
        className="absolute bottom-0 w-full"
      >
        <div className="flex items-center justify-between h-16 shadow-2xl shadow-black/20 backdrop-blur-xl px-2 py-12">
          {/* Left item */}
          <div className="flex items-center justify-center flex-1">
            <NavLink item={navItems[0]} pathname={pathname} />
          </div>

          {/* Central Button */}
          <div className="shrink-0">
            <Link href="/play" className="-mt-10">
              <motion.div
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                className="flex items-center justify-center w-16 h-16 rounded-full bg-primary shadow-lg shadow-primary/30"
              >
                <PadelBallIcon className="w-8 h-8 text-primary-foreground" />
              </motion.div>
            </Link>
          </div>

          {/* Right item */}
          <div className="flex items-center justify-center flex-1">
            <NavLink item={navItems[1]} pathname={pathname} />
          </div>
        </div>
      </motion.nav>
    </div>
  )
}
