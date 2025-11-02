"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { useToast } from "@/components/ui/use-toast"

/**
 * Global flag to track if initial fetch has been done for each cacheKey
 * Persists across StrictMode double mounts
 */
const initialFetchDone = new Map<string, boolean>()

/**
 * Simple localStorage cache utilities
 */
export const cache = {
  save: <T,>(key: string, data: T): void => {
    try {
      localStorage.setItem(key, JSON.stringify(data))
      localStorage.setItem(`${key}_timestamp`, Date.now().toString())
    } catch (e) {
      console.warn(`Failed to save cache for ${key}:`, e)
    }
  },

  load: <T,>(key: string): T | null => {
    try {
      const cached = localStorage.getItem(key)
      return cached ? JSON.parse(cached) : null
    } catch (e) {
      console.warn(`Failed to load cache for ${key}:`, e)
      return null
    }
  },

  clear: (key: string): void => {
    try {
      localStorage.removeItem(key)
      localStorage.removeItem(`${key}_timestamp`)
    } catch (e) {
      console.warn(`Failed to clear cache for ${key}:`, e)
    }
  },

  getAge: (key: string): number | null => {
    try {
      const timestamp = localStorage.getItem(`${key}_timestamp`)
      return timestamp ? Date.now() - parseInt(timestamp) : null
    } catch (e) {
      return null
    }
  }
}

/**
 * Options for useOfflineData hook
 */
interface UseOfflineDataOptions<T> {
  /** Unique cache key for this data */
  cacheKey: string
  /** Function to fetch fresh data from API */
  fetchFn: () => Promise<T>
  /** Enable polling when offline (default: true) */
  enablePolling?: boolean
  /** Initial retry delays in ms (default: [5000, 10000, 20000, 40000, 60000]) */
  retryDelays?: number[]
  /** Show toast on reconnection (default: true) */
  showReconnectToast?: boolean
  /** Timeout for network requests in ms (default: 5000) */
  timeout?: number
}

/**
 * PWA Offline-First Hook
 * 
 * Implements best practices for offline data management:
 * - Cache-first loading strategy
 * - Automatic background refresh on reconnection
 * - Exponential backoff retry mechanism
 * - Device online/offline event detection
 * - Silent background updates
 * 
 * @example
 * ```tsx
 * const { data, isOffline, isLoading, refresh } = useOfflineData({
 *   cacheKey: 'my_games',
 *   fetchFn: async () => {
 *     const result = await api.getMyGames()
 *     return result.data
 *   }
 * })
 * ```
 */
export function useOfflineData<T>({
  cacheKey,
  fetchFn,
  enablePolling = true,
  retryDelays = [5000, 10000, 20000, 40000, 60000],
  showReconnectToast = true,
  timeout = 5000
}: UseOfflineDataOptions<T>) {
  const { toast } = useToast()
  
  // Store ALL unstable dependencies in refs to keep fetchData stable
  const fetchFnRef = useRef(fetchFn)
  const timeoutRef = useRef(timeout)
  const showReconnectToastRef = useRef(showReconnectToast)
  const toastRef = useRef(toast)
  
  useEffect(() => {
    fetchFnRef.current = fetchFn
    timeoutRef.current = timeout
    showReconnectToastRef.current = showReconnectToast
    toastRef.current = toast
  }, [fetchFn, timeout, showReconnectToast, toast])
  
  // Load cache synchronously on mount for instant display
  const [data, setData] = useState<T | null>(() => cache.load<T>(cacheKey))
  const [isLoading, setIsLoading] = useState(() => !cache.load<T>(cacheKey))
  const [isOffline, setIsOffline] = useState(false)
  const [retryAttempt, setRetryAttempt] = useState(0)
  const isMountedRef = useRef(false)
  const pollingTimeoutRef = useRef<NodeJS.Timeout>()
  const dataRef = useRef<T | null>(data)
  
  // Update dataRef when data changes
  useEffect(() => {
    dataRef.current = data
  }, [data])

  /**
   * Fetch with timeout wrapper
   */
  const fetchWithTimeout = useCallback(async <R,>(promise: Promise<R>, timeoutMs: number): Promise<R> => {
    const timeoutPromise = new Promise<never>((_, reject) => {
      setTimeout(() => reject(new Error('Network timeout')), timeoutMs)
    })
    return Promise.race([promise, timeoutPromise])
  }, [])

  /**
   * Main fetch function with cache fallback
   */
  const fetchData = useCallback(async (isBackgroundRefresh = false) => {
    try {
      // Attempt to fetch fresh data - use refs to avoid dependencies
      const freshData = await fetchWithTimeout(fetchFnRef.current(), timeoutRef.current)
      
      // Success!
      setData(freshData)
      cache.save(cacheKey, freshData)
      setIsOffline(false)
      setRetryAttempt(0)

      // Show reconnection toast
      if (isBackgroundRefresh && showReconnectToastRef.current) {
        toastRef.current({
          title: "Connexion rétablie",
          description: "Vos données ont été mises à jour",
          duration: 3000,
        })
      }
    } catch (err) {
      setIsOffline(true)
      
      // Increment retry attempt on failure (only for background refresh/polling)
      if (isBackgroundRefresh) {
        setRetryAttempt(prev => prev + 1)
      }
      
      // If no data yet (no cache), try to load from cache as fallback
      if (!dataRef.current) {
        const cached = cache.load<T>(cacheKey)
        if (cached) {
          setData(cached)
        }
      }
    } finally {
      // Always stop loading after first fetch attempt
      setIsLoading(false)
    }
  }, [cacheKey, fetchWithTimeout])

  /**
   * Manual refresh function
   */
  const refresh = useCallback(async () => {
    await fetchData(true)
  }, [fetchData])

  /**
   * Initial load - avoid double fetch in React StrictMode
   */
  useEffect(() => {
    isMountedRef.current = true
    
    // Skip if already fetched in this session (StrictMode double mount protection)
    if (initialFetchDone.get(cacheKey)) {
      return
    }
    
    initialFetchDone.set(cacheKey, true)
    fetchData()
    
    return () => {
      isMountedRef.current = false
    }
  }, [fetchData, cacheKey])

  /**
   * Device online/offline events
   */
  useEffect(() => {
    const handleOnline = async () => {
      await fetchData(true)
    }

    const handleOffline = () => {
      setIsOffline(true)
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [cacheKey, fetchData])

  /**
   * Exponential backoff polling when offline
   */
  useEffect(() => {
    if (!enablePolling || !isOffline) {
      // Clear any existing timeout
      if (pollingTimeoutRef.current) {
        clearTimeout(pollingTimeoutRef.current)
        pollingTimeoutRef.current = undefined
      }
      return
    }

    const delay = retryDelays[Math.min(retryAttempt, retryDelays.length - 1)]

    pollingTimeoutRef.current = setTimeout(() => {
      fetchData(true)
    }, delay)

    return () => {
      if (pollingTimeoutRef.current) {
        clearTimeout(pollingTimeoutRef.current)
      }
    }
  }, [isOffline, retryAttempt, enablePolling, retryDelays, cacheKey, fetchData])

  return {
    data,
    isLoading,
    isOffline,
    refresh,
    /** Clear cached data */
    clearCache: () => cache.clear(cacheKey),
    /** Get cache age in milliseconds */
    cacheAge: cache.getAge(cacheKey)
  }
}
