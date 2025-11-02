import { useEffect, useRef, useCallback } from "react"

/**
 * Hook for intelligent polling with optimizations:
 * - Only polls when page is visible (Page Visibility API)
 * - Stops polling when component unmounts
 * - Configurable interval
 *
 * @param callback - Function to call on each poll
 * @param interval - Polling interval in milliseconds (default: 15000 = 15s)
 * @param enabled - Whether polling is enabled (default: true)
 *
 * @example
 * usePolling(async () => {
 *   const { data } = await fetchGameDetails(gameId)
 *   if (data) setGame(data)
 * }, 15000)
 */
export function usePolling(
  callback: () => void | Promise<void>,
  interval: number = 15000,
  enabled: boolean = true
) {
  const savedCallback = useRef(callback)
  const intervalRef = useRef<NodeJS.Timeout | null>(null)
  const isVisibleRef = useRef(true)

  // Always keep the latest callback
  useEffect(() => {
    savedCallback.current = callback
  }, [callback])

  // Handle page visibility changes
  useEffect(() => {
    const handleVisibilityChange = () => {
      isVisibleRef.current = !document.hidden

      // Resume polling when page becomes visible
      if (!document.hidden && enabled && !intervalRef.current) {
        startPolling()
      }
      // Stop polling when page is hidden
      else if (document.hidden && intervalRef.current) {
        stopPolling()
      }
    }

    document.addEventListener("visibilitychange", handleVisibilityChange)
    return () => {
      document.removeEventListener("visibilitychange", handleVisibilityChange)
    }
  }, [enabled])

  const startPolling = useCallback(() => {
    if (intervalRef.current) return // Already polling

    intervalRef.current = setInterval(async () => {
      // Only execute if page is visible
      if (isVisibleRef.current) {
        await savedCallback.current()
      }
    }, interval)
  }, [interval])

  const stopPolling = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
  }, [])

  // Start/stop polling based on enabled flag
  useEffect(() => {
    if (enabled && isVisibleRef.current) {
      startPolling()
    } else {
      stopPolling()
    }

    return () => stopPolling()
  }, [enabled, startPolling, stopPolling])

  return { stopPolling, startPolling }
}
