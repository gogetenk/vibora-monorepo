// ============================================================================
// Vibora Backend API Client
// ============================================================================
// Client HTTP centralisé pour communiquer avec le backend Vibora (.NET)
// Architecture: Headers JWT via Supabase, error handling unifié

import { getSupabaseClient } from "@/lib/supabase-client"
import { getGuestToken } from "@/lib/auth/guest-auth"
import type {
  GameDto,
  CreateGameRequest,
  CreateGameResponse,
  GetAvailableGamesQuery,
  PaginatedGamesResponse,
  MyGamesResult,
  JoinGameRequest,
  JoinGameAsGuestRequest,
  CreateGameShareResponse,
  SearchGamesRequest,
  SearchGamesResponse,
  GetShareByTokenResponse,
  GameShareDto,
  ShareMetadataDto,
  UserProfileDto,
  UpdateUserProfileRequest,
  ApiResponse,
  ApiErrorResponse,
} from "./vibora-types"

// ============================================================================
// Configuration
// ============================================================================

const VIBORA_API_BASE_URL = process.env.NEXT_PUBLIC_VIBORA_API_URL || "http://localhost:5000"

// ============================================================================
// Helper: Get JWT from Supabase or custom token
// ============================================================================

async function getViboraAuthHeaders(customToken?: string): Promise<HeadersInit> {
  const headers: HeadersInit = {
    "Content-Type": "application/json",
  }

  // Use custom token if provided (e.g., guest user token for specific call)
  if (customToken) {
    headers["Authorization"] = `Bearer ${customToken}`
    return headers
  }

  // Try Supabase session token first
  const supabase = getSupabaseClient()
  const { data: { session } } = await supabase.auth.getSession()

  if (session?.access_token) {
    headers["Authorization"] = `Bearer ${session.access_token}`
    return headers
  }

  // Fallback to guest token if no Supabase session
  const guestToken = getGuestToken()
  if (guestToken) {
    headers["Authorization"] = `Bearer ${guestToken}`
  }

  return headers
}

// ============================================================================
// Helper: Fetch wrapper with error handling
// ============================================================================

async function fetchVibora<T>(
  endpoint: string,
  options: RequestInit & { customToken?: string } = {}
): Promise<ApiResponse<T>> {
  try {
    const { customToken, ...fetchOptions } = options
    const url = `${VIBORA_API_BASE_URL}${endpoint}`
    const headers = await getViboraAuthHeaders(customToken)

    // Debug logs
    if (process.env.NODE_ENV === 'development') {
      console.log('🔌 Vibora API Call:', {
        url,
        method: fetchOptions.method || 'GET',
        hasAuth: !!(headers as Record<string, string>)['Authorization'],
        customToken: !!customToken,
      })
    }

    const response = await fetch(url, {
      ...fetchOptions,
      headers: {
        ...headers,
        ...fetchOptions.headers,
      },
    })

    // Handle non-JSON responses (e.g., 204 No Content)
    if (response.status === 204) {
      return { data: undefined as T }
    }

    const data = await response.json()

    if (!response.ok) {
      return {
        error: {
          statusCode: response.status,
          message: data.message || data.title || "Une erreur est survenue",
          errors: data.errors,
        },
      }
    }

    return { data }
  } catch (error) {
    console.error("Vibora API Error:", error)
    return {
      error: {
        statusCode: 500,
        message: "Erreur de connexion au serveur",
      },
    }
  }
}

// ============================================================================
// Games API
// ============================================================================

export const gamesApi = {
  /**
   * GET /games - Récupère la liste des parties disponibles avec filtres
   */
  async getAvailableGames(
    query?: GetAvailableGamesQuery
  ): Promise<ApiResponse<PaginatedGamesResponse>> {
    const params = new URLSearchParams()
    if (query?.location) params.append("location", query.location)
    if (query?.skillLevel) params.append("skillLevel", query.skillLevel.toString())
    if (query?.fromDate) params.append("fromDate", query.fromDate)
    if (query?.toDate) params.append("toDate", query.toDate)
    if (query?.pageNumber) params.append("pageNumber", query.pageNumber.toString())
    if (query?.pageSize) params.append("pageSize", query.pageSize.toString())

    const queryString = params.toString()
    const endpoint = queryString ? `/games?${queryString}` : "/games"

    return fetchVibora<PaginatedGamesResponse>(endpoint)
  },

  /**
   * GET /games/me - Récupère les parties de l'utilisateur connecté
   * Retourne un objet { games: GameDto[], totalCount: number }
   */
  async getMyGames(): Promise<ApiResponse<MyGamesResult>> {
    return fetchVibora<MyGamesResult>("/games/me")
  },

  /**
   * GET /games/{id} - Récupère les détails d'une partie
   */
  async getGameDetails(gameId: string): Promise<ApiResponse<GameDto>> {
    return fetchVibora<GameDto>(`/games/${gameId}`)
  },

  /**
   * POST /games - Crée une nouvelle partie
   * @param request - Les détails de la partie à créer
   * @param customToken - Token JWT optionnel (pour les guest users)
   */
  async createGame(
    request: CreateGameRequest,
    customToken?: string | null
  ): Promise<ApiResponse<CreateGameResponse>> {
    return fetchVibora<CreateGameResponse>("/games", {
      method: "POST",
      body: JSON.stringify(request),
      customToken: customToken || undefined,
    })
  },

  /**
   * POST /games/{id}/players - Rejoindre une partie (utilisateur authentifié)
   */
  async joinGame(gameId: string, request: JoinGameRequest): Promise<ApiResponse<void>> {
    return fetchVibora<void>(`/games/${gameId}/players`, {
      method: "POST",
      body: JSON.stringify(request),
    })
  },

  /**
   * POST /games/{id}/players/guest - Rejoindre une partie en tant qu'invité (PUBLIC)
   */
  async joinGameAsGuest(
    gameId: string,
    request: JoinGameAsGuestRequest,
    customToken?: string
  ): Promise<ApiResponse<void>> {
    return fetchVibora<void>(`/games/${gameId}/players/guest`, {
      method: "POST",
      body: JSON.stringify(request),
      customToken: customToken || undefined,
    })
  },

  /**
   * DELETE /games/{id}/players - Quitter une partie
   */
  async leaveGame(gameId: string): Promise<ApiResponse<void>> {
    return fetchVibora<void>(`/games/${gameId}/players`, {
      method: "DELETE",
    })
  },

  /**
   * POST /games/{id}/cancel - Annuler une partie (hôte uniquement)
   */
  async cancelGame(gameId: string): Promise<ApiResponse<void>> {
    return fetchVibora<void>(`/games/${gameId}/cancel`, {
      method: "POST",
    })
  },
  /**
   * GET /games/search - Recherche de parties correspondant aux critères
   * Feature "Play" (US29) - Retourne perfect matches + partial matches
   * Supports GPS-based search with radius filtering
   */
  async searchGames(request: SearchGamesRequest): Promise<ApiResponse<SearchGamesResponse>> {
    const params = new URLSearchParams()
    params.append("when", request.when)

    // Location (text-based, optional if GPS provided)
    if (request.where) params.append("where", request.where)

    // Skill level (optional)
    if (request.skillLevel) params.append("level", request.skillLevel.toString())

    // GPS coordinates (both must be provided together)
    if (request.latitude !== null && request.latitude !== undefined) {
      params.append("latitude", request.latitude.toString())
    }
    if (request.longitude !== null && request.longitude !== undefined) {
      params.append("longitude", request.longitude.toString())
    }

    // Radius (default handled by backend: 10km)
    if (request.radiusKm) params.append("radiusKm", request.radiusKm.toString())

    return fetchVibora<SearchGamesResponse>(`/games/search?${params.toString()}`)
  },
}

// ============================================================================
// Game Shares API (Magic Links)
// ============================================================================

export const sharesApi = {
  /**
   * POST /games/{id}/shares - Génère un lien de partage pour une partie
   * @param gameId - ID de la partie
   * @param expiresAt - Date d'expiration optionnelle (ISO 8601)
   */
  async createGameShare(
    gameId: string, 
    expiresAt?: string | null
  ): Promise<ApiResponse<CreateGameShareResponse>> {
    return fetchVibora<CreateGameShareResponse>(`/games/${gameId}/shares`, {
      method: "POST",
      body: expiresAt ? JSON.stringify({ expiresAt }) : undefined,
    })
  },

  /**
   * GET /shares/{token} - Récupère les détails d'une partie via le token
   * PUBLIC - Pas d'auth requise
   */
  async getShareByToken(token: string): Promise<ApiResponse<GetShareByTokenResponse>> {
    return fetchVibora<GetShareByTokenResponse>(`/shares/${token}`)
  },

  /**
   * GET /shares/{token}/metadata - Récupère les métadonnées Open Graph
   * PUBLIC - Pour SSR et preview WhatsApp/Telegram
   */
  async getShareMetadata(token: string): Promise<ApiResponse<ShareMetadataDto>> {
    return fetchVibora<ShareMetadataDto>(`/shares/${token}/metadata`)
  },
}

// ============================================================================
// Users API
// ============================================================================

export const usersApi = {
  /**
   * POST /users/guest - Crée un utilisateur invité
   * Retourne un token JWT à utiliser pour les requêtes suivantes
   */
  async createGuestUser(request: {
    name: string
    skillLevel: number // 1-10 scale (REQUIS par le backend)
    phoneNumber?: string
    email?: string
  }): Promise<ApiResponse<{
    externalId: string
    name: string
    skillLevel: number
    token: string
    phoneNumber?: string
    email?: string
  }>> {
    return fetchVibora<{
      externalId: string
      name: string
      skillLevel: number
      token: string
      phoneNumber?: string
      email?: string
    }>("/users/guest", {
      method: "POST",
      body: JSON.stringify(request),
    })
  },

  /**
   * GET /users/profile - Récupère le profil de l'utilisateur connecté
   */
  async getCurrentUserProfile(): Promise<ApiResponse<UserProfileDto>> {
    return fetchVibora<UserProfileDto>("/users/profile")
  },

  /**
   * PUT /users/profile - Met à jour le profil de l'utilisateur
   */
  async updateProfile(request: UpdateUserProfileRequest): Promise<ApiResponse<UserProfileDto>> {
    return fetchVibora<UserProfileDto>("/users/profile", {
      method: "PUT",
      body: JSON.stringify(request),
    })
  },

  /**
   * PUT /users/profile - Upload photo de profil (multipart/form-data)
   */
  async uploadProfilePhoto(file: File): Promise<ApiResponse<UserProfileDto>> {
    const formData = new FormData()
    formData.append("photo", file)

    const headers = await getViboraAuthHeaders()
    delete (headers as Record<string, string>)["Content-Type"] // Let browser set multipart boundary

    return fetchVibora<UserProfileDto>("/users/profile", {
      method: "PUT",
      headers,
      body: formData,
    })
  },

  /**
   * GET /users/{externalId}/profile - Récupère le profil public d'un utilisateur
   */
  async getUserPublicProfile(externalId: string): Promise<ApiResponse<UserProfileDto>> {
    return fetchVibora<UserProfileDto>(`/users/${externalId}/profile`)
  },

  /**
   * POST /users/claim-guest-participations - Récupère les participations invité
   * (Automatique au signup via webhook, mais disponible manuellement)
   */
  async claimGuestParticipations(): Promise<ApiResponse<{ claimedCount: number }>> {
    return fetchVibora<{ claimedCount: number }>("/users/claim-guest-participations", {
      method: "POST",
    })
  },
}

// ============================================================================
// Exports
// ============================================================================

export const viboraApi = {
  games: gamesApi,
  shares: sharesApi,
  users: usersApi,
}

export default viboraApi
