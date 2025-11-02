// ============================================================================
// Vibora Backend API Types
// ============================================================================
// Ces types correspondent aux DTOs du backend .NET (Vibora API)

// ============================================================================
// Game Types
// ============================================================================

export type GameStatus = "Open" | "Full" | "Canceled" | "Completed"

export interface GameDto {
  id: string
  dateTime: string // ISO 8601 format
  location: string
  skillLevel?: number | null
  maxPlayers: number
  currentPlayers: number
  status: GameStatus
  hostExternalId: string
  hostDisplayName: string
  createdAt: string
  participants: ParticipantInfoDto[]
}

export interface ParticipantInfoDto {
  type: "User" | "Guest"
  participationId: string
  identifier: string // UserId or "Guest: {Name}"
  displayName: string
  skillLevel?: number | null // Only for Users
  contactInfo?: string | null // Only for Guests (phone/email)
  isHost: boolean
  joinedAt: string
}

export interface CreateGameRequest {
  dateTime: string // ISO 8601
  location: string
  skillLevel?: number | null
  maxPlayers: number
  latitude?: number | null // GPS coordinates from Google Places
  longitude?: number | null
}

export interface CreateGameResponse {
  id: string // Guid from backend
  dateTime: string
  location: string
  skillLevel: string
  maxPlayers: number
  hostExternalId: string
  currentPlayers: number
  participants: Array<{
    externalId: string
    name: string
    skillLevel: string
  }>
}

export interface GetAvailableGamesQuery {
  location?: string
  skillLevel?: number
  fromDate?: string // ISO 8601
  toDate?: string // ISO 8601
  pageNumber?: number
  pageSize?: number
}

export interface PaginatedGamesResponse {
  items: GameDto[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface MyGamesResult {
  games: GameDto[]
  totalCount: number
}

export interface JoinGameRequest {
  userName: string
  userSkillLevel: string
}

export interface JoinGameAsGuestRequest {
  name: string
  phoneNumber?: string | null
  email?: string | null
}

// ============================================================================
// Game Share Types (Magic Links)
// ============================================================================

export interface CreateGameShareRequest {
  expiresAt?: string | null // ISO 8601 format, optional
}

export interface CreateGameShareResponse {
  gameShareId: string
  shareToken: string
  shareUrl: string
}

export interface GameShareDto {
  id: string
  gameId: string
  sharedByUserExternalId: string
  shareToken: string
  viewCount: number
  createdAt: string
  expiresAt?: string | null
  game?: GameDto // Populated when querying by token
}

export interface GetShareByTokenResponse {
  gameId: string
  gameShareId: string
  shareToken: string
  viewCount: number
  isExpired: boolean
  game: GameSummaryDto
}

export interface GameSummaryDto {
  id: string
  dateTime: string
  location: string
  skillLevel: string
  maxPlayers: number
  currentPlayers: number
  status: string
  hostDisplayName: string
}

export interface ShareMetadataDto {
  title: string
  description: string
  location: string
  gameDateTime: string // Backend retourne gameDateTime, pas dateTime
  skillLevel: string // Backend retourne un string, pas un number
  currentPlayers: number
  maxPlayers: number
  gameStatus: string
}

// ============================================================================
// User Types
// ============================================================================

export interface UserProfileDto {
  externalId: string
  firstName: string
  lastName: string
  displayName: string // "FirstName LastNameInitial"
  skillLevel?: number | null
  bio?: string | null
  profilePhotoUrl?: string | null
  gamesPlayedCount: number
  memberSince: string
}

export interface UpdateUserProfileRequest {
  firstName?: string
  lastName?: string
  skillLevel?: number | null
  bio?: string | null
  // Photo is uploaded separately via multipart/form-data
}

export interface SyncUserFromAuthRequest {
  externalId: string
  email?: string | null
  phoneNumber?: string | null
}

// ============================================================================
// Error Response
// ============================================================================

export interface ApiErrorResponse {
  statusCode: number
  message: string
  errors?: Record<string, string[]>
}

// ============================================================================
// Helper Types
// ============================================================================

export interface ApiResponse<T> {
  data?: T
  error?: ApiErrorResponse
}

// Explicit export to ensure this file is treated as a module
export {}

// ============================================================================
// Game Search Types (Feature "Play" - US29)
// ============================================================================

export interface SearchGamesRequest {
  when: string // ISO 8601 format (dateTime)
  where?: string | null // Location string (optional if GPS provided)
  skillLevel?: number | null // Skill level (1-10), optional for guests
  latitude?: number | null // User's current GPS latitude
  longitude?: number | null // User's current GPS longitude
  radiusKm?: number // Search radius in kilometers (default: 10)
}

export interface GameMatchDto {
  id: string
  dateTime: string
  location: string
  skillLevel?: number | null
  maxPlayers: number
  currentPlayers: number
  status: GameStatus
  hostDisplayName: string
  matchScore: number // Scoring algorithm (0=no match, 4=perfect match with GPS)
  distanceKm?: number | null // Distance from user in kilometers (null if no GPS)
}

export interface SearchGamesResponse {
  perfectMatches: GameMatchDto[]
  partialMatches: GameMatchDto[]
}
