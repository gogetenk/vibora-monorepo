"use client"

import { useEffect, useState } from "react"

const DISMISSED_KEY = "vibora_install_dismissed"
const DISMISS_DURATION_MS = 3 * 24 * 60 * 60 * 1000 // 3 days

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>
}

interface UseInstallPromptReturn {
  isInstallable: boolean
  isIOS: boolean
  isStandalone: boolean
  shouldShowPrompt: boolean
  promptInstall: () => Promise<void>
  dismissPrompt: () => void
}

export function useInstallPrompt(): UseInstallPromptReturn {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null)
  const [isInstallable, setIsInstallable] = useState(false)
  const [isIOS, setIsIOS] = useState(false)
  const [isStandalone, setIsStandalone] = useState(false)
  const [isDismissed, setIsDismissed] = useState(true)

  useEffect(() => {
    // SSR safety check
    if (typeof window === "undefined") return

    // Check if already in standalone mode
    const standalone = window.matchMedia("(display-mode: standalone)").matches
    setIsStandalone(standalone)

    // Detect iOS
    const ios = /iPad|iPhone|iPod/.test(navigator.userAgent) && !(window as any).MSStream
    setIsIOS(ios)

    // Check if prompt was recently dismissed
    const dismissedTimestamp = localStorage.getItem(DISMISSED_KEY)
    const now = Date.now()

    if (dismissedTimestamp) {
      const timeSinceDismissed = now - parseInt(dismissedTimestamp, 10)
      setIsDismissed(timeSinceDismissed < DISMISS_DURATION_MS)
    } else {
      setIsDismissed(false)
    }

    // Listen for beforeinstallprompt event (Android/Desktop)
    const handleBeforeInstallPrompt = (e: Event) => {
      e.preventDefault()
      const event = e as BeforeInstallPromptEvent
      setDeferredPrompt(event)
      setIsInstallable(true)
    }

    window.addEventListener("beforeinstallprompt", handleBeforeInstallPrompt)

    return () => {
      window.removeEventListener("beforeinstallprompt", handleBeforeInstallPrompt)
    }
  }, [])

  const promptInstall = async () => {
    if (!deferredPrompt) return

    try {
      await deferredPrompt.prompt()
      const { outcome } = await deferredPrompt.userChoice

      if (outcome === "accepted") {
        console.log("PWA installed successfully")
      }

      // Clear the deferred prompt
      setDeferredPrompt(null)
      setIsInstallable(false)
    } catch (error) {
      console.error("Error prompting install:", error)
    }
  }

  const dismissPrompt = () => {
    if (typeof window !== "undefined") {
      localStorage.setItem(DISMISSED_KEY, Date.now().toString())
    }
    setIsDismissed(true)
  }

  const shouldShowPrompt = (isInstallable || isIOS) && !isStandalone && !isDismissed

  return {
    isInstallable,
    isIOS,
    isStandalone,
    shouldShowPrompt,
    promptInstall,
    dismissPrompt,
  }
}
