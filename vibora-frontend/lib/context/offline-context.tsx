"use client"

import { createContext, useContext, useState, useEffect, type ReactNode } from "react"

interface OfflineContextValue {
  /** Global offline state (true if device is offline OR any critical API is down) */
  isGloballyOffline: boolean
  /** Register an offline state from a component/hook */
  registerOfflineState: (key: string, isOffline: boolean) => void
  /** Unregister when component unmounts */
  unregisterOfflineState: (key: string) => void
  /** Get all registered offline states */
  offlineStates: Record<string, boolean>
}

const OfflineContext = createContext<OfflineContextValue | undefined>(undefined)

/**
 * Offline Context Provider
 * 
 * Manages global offline state across the application.
 * Combines device connectivity with backend availability.
 */
export function OfflineProvider({ children }: { children: ReactNode }) {
  const [offlineStates, setOfflineStates] = useState<Record<string, boolean>>({})
  const [isDeviceOffline, setIsDeviceOffline] = useState(false)

  // Listen to device online/offline events
  useEffect(() => {
    const handleOnline = () => setIsDeviceOffline(false)
    const handleOffline = () => setIsDeviceOffline(true)

    // Check initial state
    setIsDeviceOffline(!navigator.onLine)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  const registerOfflineState = (key: string, isOffline: boolean) => {
    setOfflineStates(prev => ({ ...prev, [key]: isOffline }))
  }

  const unregisterOfflineState = (key: string) => {
    setOfflineStates(prev => {
      const newStates = { ...prev }
      delete newStates[key]
      return newStates
    })
  }

  // Global offline = device offline OR any registered state is offline
  const isGloballyOffline = isDeviceOffline || Object.values(offlineStates).some(state => state)

  return (
    <OfflineContext.Provider value={{
      isGloballyOffline,
      registerOfflineState,
      unregisterOfflineState,
      offlineStates
    }}>
      {children}
    </OfflineContext.Provider>
  )
}

/**
 * Hook to access offline context
 */
export function useOfflineContext() {
  const context = useContext(OfflineContext)
  if (!context) {
    throw new Error('useOfflineContext must be used within OfflineProvider')
  }
  return context
}
