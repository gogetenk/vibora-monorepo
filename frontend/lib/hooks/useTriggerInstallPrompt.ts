"use client"

/**
 * Trigger Install Prompt Hook
 *
 * Dispatch a custom event to signal that the user has performed
 * a meaningful action (join/create game). This triggers the PWA
 * install prompt to show.
 *
 * Usage in game action pages:
 * ```tsx
 * import { useTriggerInstallPrompt } from "@/lib/hooks/useTriggerInstallPrompt"
 *
 * function CreateGamePage() {
 *   const triggerInstallPrompt = useTriggerInstallPrompt()
 *
 *   const handleCreateGame = async () => {
 *     await createGame(...)
 *     triggerInstallPrompt() // Show install prompt after success
 *   }
 * }
 * ```
 */
export function useTriggerInstallPrompt() {
  const trigger = () => {
    if (typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent("vibora:game-action"))
    }
  }

  return trigger
}
