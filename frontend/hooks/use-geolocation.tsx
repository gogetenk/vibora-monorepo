"use client"

import { useState } from "react"
import { reverseGeocode } from "@/lib/google-maps"

export function useLocationSuggestions() {
  const [recentLocations, setRecentLocations] = useState<string[]>([])
  
  const popularLocations = [
    { id: '1', name: '15ème arrondissement, Paris', description: 'Le plus populaire', type: 'popular' },
    { id: '2', name: '16ème arrondissement, Paris', description: 'Auteuil, Passy', type: 'popular' },
    { id: '3', name: '17ème arrondissement, Paris', description: 'Batignolles, Monceau', type: 'popular' },
    { id: '4', name: 'Boulogne-Billancourt', description: 'Hauts-de-Seine', type: 'popular' },
    { id: '5', name: 'Neuilly-sur-Seine', description: 'Hauts-de-Seine', type: 'popular' },
    { id: '6', name: 'Levallois-Perret', description: 'Hauts-de-Seine', type: 'popular' },
  ]

  const suggestions = [
    ...recentLocations.map((loc, idx) => ({
      id: `recent-${idx}`,
      name: loc,
      type: 'recent' as const,
      description: undefined
    })),
    ...popularLocations
  ]

  const searchSuggestions = (query: string) => {
    if (!query) return suggestions
    
    const allLocations = [
      "1er arrondissement, Paris",
      "2ème arrondissement, Paris",
      "3ème arrondissement, Paris",
      "4ème arrondissement, Paris",
      "5ème arrondissement, Paris",
      "6ème arrondissement, Paris",
      "7ème arrondissement, Paris",
      "8ème arrondissement, Paris",
      "9ème arrondissement, Paris",
      "10ème arrondissement, Paris",
      "11ème arrondissement, Paris",
      "12ème arrondissement, Paris",
      "13ème arrondissement, Paris",
      "14ème arrondissement, Paris",
      "15ème arrondissement, Paris",
      "16ème arrondissement, Paris",
      "17ème arrondissement, Paris",
      "18ème arrondissement, Paris",
      "19ème arrondissement, Paris",
      "20ème arrondissement, Paris",
      "Boulogne-Billancourt",
      "Neuilly-sur-Seine",
      "Levallois-Perret",
      "Issy-les-Moulineaux",
      "Vincennes",
      "Saint-Mandé",
      "Montreuil",
      "Bagnolet",
      "La Défense"
    ]
    
    return allLocations
      .filter(loc => loc.toLowerCase().includes(query.toLowerCase()))
      .slice(0, 6)
      .map((loc, idx) => ({
        id: `search-${idx}`,
        name: loc,
        type: 'search' as const,
        description: undefined
      }))
  }

  const addRecentLocation = (location: string) => {
    setRecentLocations(prev => {
      const filtered = prev.filter(loc => loc !== location)
      return [location, ...filtered].slice(0, 3)
    })
  }

  const getCurrentLocationSuggestion = async () => {
    return new Promise<{ name: string } | null>((resolve) => {
      if (!navigator.geolocation) {
        resolve(null)
        return
      }

      navigator.geolocation.getCurrentPosition(
        async (position) => {
          const { latitude, longitude } = position.coords

          // Use real reverse geocoding to get city name
          const cityName = await reverseGeocode(latitude, longitude)

          if (cityName) {
            resolve({ name: cityName })
          } else {
            // Fallback to "Ma position actuelle" if reverse geocoding fails
            resolve({ name: "Ma position actuelle" })
          }
        },
        () => resolve(null),
        { timeout: 10000 }
      )
    })
  }

  return {
    suggestions,
    searchSuggestions,
    addRecentLocation,
    getCurrentLocationSuggestion
  }
}