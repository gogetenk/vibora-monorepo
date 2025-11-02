// ============================================================================
// Supabase Auth Helpers
// ============================================================================
// Wrapper functions pour l'authentification Supabase
// Intégration avec le backend Vibora via webhook POST /users/sync

import { getSupabaseClient } from "@/lib/supabase-client"
import type { AuthError, Session, User } from "@supabase/supabase-js"

// ============================================================================
// Types
// ============================================================================

export interface AuthResult {
  user: User | null
  session: Session | null
  error: AuthError | null
}

export interface SignUpWithEmailParams {
  email: string
  password: string
  firstName?: string
  lastName?: string
  skillLevel?: number
}

export interface SignInWithEmailParams {
  email: string
  password: string
}

// ============================================================================
// Authentication Functions
// ============================================================================

/**
 * Inscrit un nouvel utilisateur avec email/password
 * Supabase Auth va automatiquement appeler le webhook POST /users/sync
 */
export async function signUp({
  email,
  password,
  firstName,
  lastName,
  skillLevel,
}: SignUpWithEmailParams): Promise<AuthResult> {
  const supabase = getSupabaseClient()

  const { data, error } = await supabase.auth.signUp({
    email,
    password,
    options: {
      data: {
        first_name: firstName,
        last_name: lastName,
        skill_level: skillLevel || 5, // Default: 5 (Intermediate) on 1-10 scale
      },
      // Redirection après confirmation email (si activée)
      emailRedirectTo: `${window.location.origin}/auth/callback`,
    },
  })

  return {
    user: data.user,
    session: data.session,
    error,
  }
}

/**
 * Connecte un utilisateur avec email/password
 */
export async function signIn({
  email,
  password,
}: SignInWithEmailParams): Promise<AuthResult> {
  const supabase = getSupabaseClient()

  const { data, error } = await supabase.auth.signInWithPassword({
    email,
    password,
  })

  return {
    user: data.user,
    session: data.session,
    error,
  }
}

/**
 * Connexion avec Magic Link (email passwordless)
 * Conforme à MAGIC_LINKS_IMPLEMENTATION.md
 */
export async function signInWithMagicLink(email: string): Promise<{ error: AuthError | null }> {
  const supabase = getSupabaseClient()

  const { error } = await supabase.auth.signInWithOtp({
    email,
    options: {
      emailRedirectTo: `${window.location.origin}/auth/callback`,
    },
  })

  return { error }
}

/**
 * Connexion OAuth (Google, Apple, etc.)
 */
export async function signInWithOAuth(
  provider: "google" | "apple"
): Promise<{ error: AuthError | null }> {
  const supabase = getSupabaseClient()

  const { error } = await supabase.auth.signInWithOAuth({
    provider,
    options: {
      redirectTo: `${window.location.origin}/auth/callback`,
    },
  })

  return { error }
}

/**
 * Déconnexion
 */
export async function signOut(): Promise<{ error: AuthError | null }> {
  const supabase = getSupabaseClient()
  const { error } = await supabase.auth.signOut()
  return { error }
}

/**
 * Récupère la session actuelle
 */
export async function getSession(): Promise<{ session: Session | null; error: AuthError | null }> {
  const supabase = getSupabaseClient()
  const { data, error } = await supabase.auth.getSession()
  return { session: data.session, error }
}

/**
 * Récupère l'utilisateur actuel
 */
export async function getCurrentUser(): Promise<{ user: User | null; error: AuthError | null }> {
  const supabase = getSupabaseClient()
  const { data, error } = await supabase.auth.getUser()
  return { user: data.user, error }
}

/**
 * Rafraîchit la session (appelé automatiquement par Supabase)
 */
export async function refreshSession(): Promise<{ session: Session | null; error: AuthError | null }> {
  const supabase = getSupabaseClient()
  const { data, error } = await supabase.auth.refreshSession()
  return { session: data.session, error }
}

/**
 * Écoute les changements d'état d'authentification
 * Utile pour les composants qui doivent réagir aux changements de session
 */
export function onAuthStateChange(
  callback: (event: string, session: Session | null) => void
) {
  const supabase = getSupabaseClient()
  
  const { data: { subscription } } = supabase.auth.onAuthStateChange((event, session) => {
    callback(event, session)
  })

  // Retourne une fonction pour cleanup
  return () => {
    subscription.unsubscribe()
  }
}

// ============================================================================
// Helper: Get Vibora Auth Headers
// ============================================================================
// Utilisé par vibora-client.ts

export async function getViboraAuthHeaders(): Promise<HeadersInit> {
  const { session } = await getSession()
  
  const headers: HeadersInit = {
    "Content-Type": "application/json",
  }

  if (session?.access_token) {
    headers["Authorization"] = `Bearer ${session.access_token}`
  }

  return headers
}

/**
 * Extrait l'ExternalId (sub) du JWT Supabase
 * Correspond au Guid ExternalId dans le backend Vibora
 */
export async function getCurrentUserExternalId(): Promise<string | null> {
  const { user } = await getCurrentUser()
  return user?.id || null // user.id = sub claim in JWT
}
