// ============================================================================
// Guest Authentication Management
// ============================================================================
// Gère la persistence et la récupération du token guest

const GUEST_TOKEN_KEY = 'vibora_guest_token'
const GUEST_USER_KEY = 'vibora_guest_user'

export interface GuestUser {
  externalId: string
  name: string
  skillLevel: number // 1-10 scale
  token: string
}

/**
 * Stocke le token et les infos du guest user
 */
export function setGuestAuth(guestUser: GuestUser): void {
  if (typeof window === 'undefined') return
  
  localStorage.setItem(GUEST_TOKEN_KEY, guestUser.token)
  localStorage.setItem(GUEST_USER_KEY, JSON.stringify({
    externalId: guestUser.externalId,
    name: guestUser.name,
    skillLevel: guestUser.skillLevel,
  }))
}

/**
 * Récupère le token guest s'il existe
 */
export function getGuestToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem(GUEST_TOKEN_KEY)
}

/**
 * Récupère les infos du guest user
 */
export function getGuestUser(): Omit<GuestUser, 'token'> | null {
  if (typeof window === 'undefined') return null
  
  const userStr = localStorage.getItem(GUEST_USER_KEY)
  if (!userStr) return null
  
  try {
    return JSON.parse(userStr)
  } catch {
    return null
  }
}

/**
 * Vérifie si l'utilisateur est en mode guest
 */
export function isGuestMode(): boolean {
  return !!getGuestToken()
}

/**
 * Efface l'authentification guest (lors du login normal)
 */
export function clearGuestAuth(): void {
  if (typeof window === 'undefined') return
  
  localStorage.removeItem(GUEST_TOKEN_KEY)
  localStorage.removeItem(GUEST_USER_KEY)
}
