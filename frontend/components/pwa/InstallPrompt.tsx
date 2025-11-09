"use client"

import { useEffect, useState } from "react"
import { X, Download, Share } from "lucide-react"
import { motion, AnimatePresence } from "framer-motion"
import { useInstallPrompt } from "@/lib/hooks/useInstallPrompt"

/**
 * InstallPrompt - PWA Installation Banner
 *
 * Displays AFTER first user action (join/create game), not on page load.
 * - Android/Desktop: Uses beforeinstallprompt API
 * - iOS Safari: Shows manual instructions (Share -> Add to Home Screen)
 * - Dismissable: Hides for 3 days when closed
 * - Auto-hides: When app is already installed/in standalone mode
 */
export function InstallPrompt() {
  const { shouldShowPrompt, isIOS, promptInstall, dismissPrompt } = useInstallPrompt()
  const [hasInteracted, setHasInteracted] = useState(false)

  useEffect(() => {
    // SSR safety check
    if (typeof window === "undefined") return

    // Check if user has already interacted (stored in sessionStorage to persist during session)
    const interacted = sessionStorage.getItem("vibora_has_interacted")
    if (interacted === "true") {
      setHasInteracted(true)
    }

    // Listen for first interaction events (game actions)
    const handleInteraction = () => {
      sessionStorage.setItem("vibora_has_interacted", "true")
      setHasInteracted(true)
    }

    // Custom events dispatched from game join/create actions
    window.addEventListener("vibora:game-action", handleInteraction)

    return () => {
      window.removeEventListener("vibora:game-action", handleInteraction)
    }
  }, [])

  const handleInstallClick = async () => {
    if (isIOS) {
      // iOS requires manual installation - instructions shown in banner
      return
    }
    await promptInstall()
  }

  const handleDismiss = () => {
    dismissPrompt()
  }

  // Only show after user interaction AND if should show prompt
  if (!hasInteracted || !shouldShowPrompt) {
    return null
  }

  return (
    <AnimatePresence>
      <motion.div
        initial={{ y: 100, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        exit={{ y: 100, opacity: 0 }}
        transition={{ type: "spring", stiffness: 300, damping: 30 }}
        className="fixed bottom-20 left-4 right-4 z-50 md:left-auto md:right-4 md:max-w-md"
      >
        <div className="relative overflow-hidden rounded-2xl border border-border/50 bg-card/95 p-4 shadow-2xl backdrop-blur-md">
          {/* Gradient accent */}
          <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-emerald-500 to-emerald-600" />

          {/* Close button */}
          <button
            onClick={handleDismiss}
            className="absolute right-3 top-3 rounded-full p-1 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            aria-label="Fermer"
          >
            <X className="h-4 w-4" />
          </button>

          <div className="flex items-start gap-3">
            {/* Icon */}
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-emerald-500/10">
              {isIOS ? (
                <Share className="h-6 w-6 text-emerald-500" />
              ) : (
                <Download className="h-6 w-6 text-emerald-500" />
              )}
            </div>

            {/* Content */}
            <div className="flex-1 space-y-1 pr-6">
              <h3 className="font-semibold text-foreground">
                Installer Vibora
              </h3>
              {isIOS ? (
                <p className="text-sm text-muted-foreground">
                  Appuyez sur <Share className="inline h-4 w-4" /> puis &quot;Sur l&apos;écran d&apos;accueil&quot;
                </p>
              ) : (
                <p className="text-sm text-muted-foreground">
                  Accédez rapidement à vos parties depuis votre écran d&apos;accueil
                </p>
              )}
            </div>
          </div>

          {/* Install button (Android/Desktop only) */}
          {!isIOS && (
            <button
              onClick={handleInstallClick}
              className="mt-3 w-full rounded-lg bg-emerald-500 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-emerald-600 active:scale-95"
            >
              Installer
            </button>
          )}
        </div>
      </motion.div>
    </AnimatePresence>
  )
}
