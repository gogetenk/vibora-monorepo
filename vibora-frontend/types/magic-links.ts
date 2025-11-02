export interface MagicLinkToken {
  id: string
  gameId: string
  token: string
  createdBy: string
  expiresAt: Date
  isActive: boolean
  maxUses?: number
  currentUses: number
  createdAt: Date
  updatedAt: Date
}

export interface MagicLinkValidation {
  isValid: boolean
  isExpired: boolean
  game: GameData | null
  error: string | null
}

export interface GameData {
  id: string
  status: 'open' | 'full' | 'cancelled' | 'completed'
  date: string
  time: string
  endTime: string
  level: number
  price: number
  description?: string
  club: ClubData
  creator: PlayerData
  players: PlayerData[]
  maxPlayers: number
  spotsLeft: number
  isFull: boolean
  magicLinkCreatedBy: string
}

export interface ClubData {
  id: string
  name: string
  address: string
  distance: number
  indoor: boolean
  rating: number
  imageUrl?: string
  facilities?: string[]
}

export interface PlayerData {
  id: string
  name: string
  avatar?: string
  level: number
  status: 'confirmed' | 'pending' | 'cancelled'
  isCreator: boolean
  gamesPlayed?: number
}

export interface GuestPlayerData {
  name: string
  phone: string
  level: string
  gameId: string
  magicLinkToken: string
}

export interface MagicLinkAnalytics {
  linkId: string
  gameId: string
  totalViews: number
  uniqueViews: number
  conversions: number
  conversionRate: number
  guestParticipants: number
  accountCreations: number
  shareMetrics: {
    whatsapp: number
    email: number
    direct: number
    other: number
  }
}

export interface ConversionOffer {
  isGuest: boolean
  playerName: string
  gameId: string
  gameData: {
    date: string
    time: string
    club: {
      name: string
      address: string
    }
    creator: {
      name: string
    }
  }
  benefits: string[]
  emailPlaceholder?: string
}

// API Response types
export interface MagicLinkCreateResponse {
  success: boolean
  token: string
  link: string
  expiresAt: string
  error?: string
}

export interface MagicLinkValidateResponse {
  success: boolean
  validation: MagicLinkValidation
  error?: string
}

export interface GuestJoinResponse {
  success: boolean
  playerId?: string
  gameId: string
  redirectUrl: string
  error?: string
}

export interface ConversionResponse {
  success: boolean
  userId?: string
  onboardingUrl?: string
  error?: string
}

// Error types
export enum MagicLinkError {
  INVALID_TOKEN = 'INVALID_TOKEN',
  EXPIRED_TOKEN = 'EXPIRED_TOKEN',
  GAME_NOT_FOUND = 'GAME_NOT_FOUND',
  GAME_FULL = 'GAME_FULL',
  GAME_CANCELLED = 'GAME_CANCELLED',
  MAX_USES_REACHED = 'MAX_USES_REACHED',
  VALIDATION_ERROR = 'VALIDATION_ERROR',
  SERVER_ERROR = 'SERVER_ERROR'
}

export interface MagicLinkErrorDetails {
  code: MagicLinkError
  message: string
  details?: any
  retryable: boolean
}