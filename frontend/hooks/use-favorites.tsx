"use client"

import { useState, useEffect, useCallback } from "react"

// Type pour les clubs favoris
export type FavoriteClub = {
  id: string
  name: string
  timestamp: number
}

export function useFavorites() {
  const [favorites, setFavorites] = useState<FavoriteClub[]>([])
  const [isLoaded, setIsLoaded] = useState(false)

  // Charger les favoris depuis le localStorage au montage du composant
  useEffect(() => {
    const loadFavorites = () => {
      try {
        const storedFavorites = localStorage.getItem("favoriteClubs")
        if (storedFavorites) {
          setFavorites(JSON.parse(storedFavorites))
        }
      } catch (error) {
        console.error("Erreur lors du chargement des favoris:", error)
      } finally {
        setIsLoaded(true)
      }
    }

    loadFavorites()
  }, [])

  // Sauvegarder les favoris dans le localStorage à chaque changement
  useEffect(() => {
    if (isLoaded) {
      try {
        localStorage.setItem("favoriteClubs", JSON.stringify(favorites))
      } catch (error) {
        console.error("Erreur lors de la sauvegarde des favoris:", error)
      }
    }
  }, [favorites, isLoaded])

  // Vérifier si un club est dans les favoris
  const isFavorite = useCallback(
    (clubId: string) => {
      return favorites.some((club) => club.id === clubId)
    },
    [favorites],
  )

  // Ajouter un club aux favoris
  const addFavorite = useCallback(
    (club: { id: string; name: string }) => {
      if (!isFavorite(club.id)) {
        setFavorites((prev) => [
          ...prev,
          {
            id: club.id,
            name: club.name,
            timestamp: Date.now(),
          },
        ])
        return true
      }
      return false
    },
    [isFavorite],
  )

  // Supprimer un club des favoris
  const removeFavorite = useCallback(
    (clubId: string) => {
      if (isFavorite(clubId)) {
        setFavorites((prev) => prev.filter((club) => club.id !== clubId))
        return true
      }
      return false
    },
    [isFavorite],
  )

  // Basculer l'état favori d'un club
  const toggleFavorite = useCallback(
    (club: { id: string; name: string }) => {
      if (isFavorite(club.id)) {
        return removeFavorite(club.id)
      } else {
        return addFavorite(club)
      }
    },
    [isFavorite, removeFavorite, addFavorite],
  )

  return {
    favorites,
    isFavorite,
    addFavorite,
    removeFavorite,
    toggleFavorite,
    isLoaded,
  }
}
