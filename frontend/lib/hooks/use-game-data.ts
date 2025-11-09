"use client"

import { useEffect, useRef, useCallback } from "react"
import { useOfflineData } from "./use-offline-data"
import { viboraApi } from "@/lib/api/vibora-client"
import { getSession } from "@/lib/auth/supabase-auth"
import { isGuestMode } from "@/lib/auth/guest-auth"
import type { GameDto, UserProfileDto, GameMatchDto } from "@/lib/api/vibora-types"

/**
 * Hook for user profile with offline support
 */
export function useUserProfile() {
  const fetchFn = useCallback(async () => {
    const { session } = await getSession()
    if (!session) return null
    
    const { data } = await viboraApi.users.getCurrentUserProfile()
    return data || null
  }, [])
  
  return useOfflineData<UserProfileDto | null>({
    cacheKey: 'vibora_user_profile',
    fetchFn,
    showReconnectToast: false // Don't show toast for profile updates
  })
}

/**
 * Hook for my games with offline support
 */
export function useMyGames() {
  const fetchFn = useCallback(async () => {
    const { session } = await getSession()
    const isGuest = isGuestMode()
    
    if (!session && !isGuest) return []
    
    const { data, error } = await viboraApi.games.getMyGames()
    if (error || !data) return []
    
    // Filter future games only
    const futureGames = data.games.filter(g => new Date(g.dateTime) > new Date())
    return futureGames
  }, [])
  
  return useOfflineData<GameDto[]>({
    cacheKey: 'vibora_my_games',
    fetchFn
  })
}

/**
 * Hook for available games with offline support
 * Uses a stable cache key to prevent re-fetching when myGamesIds array reference changes
 */
export function useAvailableGames(myGamesIds: string[] = []) {
  // Store the IDs in a ref to use in fetchFn without causing re-fetches
  const idsRef = useRef<string[]>(myGamesIds)
  
  // Update ref when IDs change
  useEffect(() => {
    idsRef.current = myGamesIds
  }, [myGamesIds])
  
  const fetchFn = useCallback(async () => {
    const { data, error } = await viboraApi.games.getAvailableGames({
      pageSize: 20,
      pageNumber: 1,
    })
    
    if (error || !data) return []
    
    // Filter out games user is already in - use ref to get latest value
    const allGames = data.items || []
    return allGames.filter(game => !idsRef.current.includes(game.id))
  }, [])
  
  return useOfflineData<GameDto[]>({
    cacheKey: 'vibora_available_games',
    fetchFn
  })
}

/**
 * Hook for my games with tabs (upcoming/past) - for /my-games page
 */
export function useMyGamesWithTabs() {
  const fetchFn = useCallback(async () => {
    const { session } = await getSession()
    const isGuest = isGuestMode()
    
    if (!session && !isGuest) return { upcoming: [], past: [] }
    
    const { data, error } = await viboraApi.games.getMyGames()
    if (error || !data) return { upcoming: [], past: [] }
    
    const now = new Date()
    const upcoming = data.games.filter(g => new Date(g.dateTime) >= now)
    const past = data.games.filter(g => new Date(g.dateTime) < now)
    
    return { upcoming, past }
  }, [])
  
  return useOfflineData<{ upcoming: GameDto[], past: GameDto[] }>({
    cacheKey: 'vibora_my_games_tabs',
    fetchFn
  })
}

/**
 * Hook for game details by ID
 */
export function useGameDetails(gameId: string) {
  const fetchFn = useCallback(async () => {
    const { data, error } = await viboraApi.games.getGameDetails(gameId)
    if (error || !data) return null
    return data
  }, [gameId])
  
  return useOfflineData<GameDto | null>({
    cacheKey: `vibora_game_${gameId}`,
    fetchFn,
    showReconnectToast: false
  })
}

/**
 * Hook for searching games (Play feature)
 */
export function useSearchGames(params?: {
  when?: string  // ISO date string
  where?: string  // Location
  skillLevel?: number
}) {
  const paramsRef = useRef(params)
  
  useEffect(() => {
    paramsRef.current = params
  }, [params])
  
  const fetchFn = useCallback(async () => {
    if (!paramsRef.current?.when || !paramsRef.current?.where) {
      return { perfectMatches: [], partialMatches: [] }
    }
    
    const { data, error } = await viboraApi.games.searchGames({
      when: paramsRef.current.when,
      where: paramsRef.current.where,
      skillLevel: paramsRef.current.skillLevel
    })
    
    if (error || !data) {
      return { perfectMatches: [], partialMatches: [] }
    }
    
    return data
  }, [])
  
  return useOfflineData<{ perfectMatches: GameMatchDto[], partialMatches: GameMatchDto[] }>({
    cacheKey: 'vibora_search_games',
    fetchFn,
    showReconnectToast: false
  })
}
