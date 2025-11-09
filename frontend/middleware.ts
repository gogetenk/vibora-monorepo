// ============================================================================
// Middleware - Route Protection
// ============================================================================
// Protège les routes privées et redirige vers /auth/login si non authentifié
// Extrait l'ExternalId du JWT Supabase

import { createMiddlewareClient } from "@supabase/auth-helpers-nextjs"
import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

// ============================================================================
// Routes Configuration
// ============================================================================

// Routes qui nécessitent une authentification
const PROTECTED_ROUTES = [
  "/my-games",
  "/settings",
  "/games/[id]/edit", // Si implémenté plus tard
  // Note: /create-game est PUBLIC (mode invité autorisé)
]

// Routes d'authentification (rediriger vers / si déjà connecté)
const AUTH_ROUTES = [
  "/auth/login",
  "/auth/signup",
]

// Routes publiques (pas de vérification)
const PUBLIC_ROUTES = [
  "/",
  "/games",
  "/join", // Magic Links - public
  "/auth/callback",
]

// ============================================================================
// Middleware
// ============================================================================

export async function middleware(request: NextRequest) {
  const res = NextResponse.next()
  const pathname = request.nextUrl.pathname

  // Créer le client Supabase pour le middleware
  const supabase = createMiddlewareClient({ req: request, res })

  // Récupérer la session
  const {
    data: { session },
  } = await supabase.auth.getSession()

  // ============================================================================
  // 1. Protection des routes privées
  // ============================================================================
  const isProtectedRoute = PROTECTED_ROUTES.some((route) => {
    if (route.includes("[")) {
      // Handle dynamic routes like /games/[id]
      const regex = new RegExp("^" + route.replace(/\[.*?\]/g, "[^/]+") + "$")
      return regex.test(pathname)
    }
    return pathname.startsWith(route)
  })

  if (isProtectedRoute && !session) {
    // Rediriger vers login avec l'URL de retour
    const redirectUrl = new URL("/auth/login", request.url)
    redirectUrl.searchParams.set("redirectTo", pathname)
    return NextResponse.redirect(redirectUrl)
  }

  // ============================================================================
  // 2. Redirection des routes auth si déjà connecté
  // ============================================================================
  const isAuthRoute = AUTH_ROUTES.some((route) => pathname.startsWith(route))

  if (isAuthRoute && session) {
    // Si déjà connecté, rediriger vers la page d'accueil
    const redirectTo = request.nextUrl.searchParams.get("redirectTo") || "/"
    return NextResponse.redirect(new URL(redirectTo, request.url))
  }

  // ============================================================================
  // 3. Logging (dev only) - ExternalId extraction
  // ============================================================================
  if (process.env.NODE_ENV === "development" && session) {
    console.log("🔐 Middleware - User authenticated:", {
      externalId: session.user.id, // sub claim = ExternalId in Vibora backend
      email: session.user.email,
      pathname,
    })
  }

  return res
}

// ============================================================================
// Matcher Configuration
// ============================================================================
// Applique le middleware uniquement aux routes spécifiées

export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public files (images, etc.)
     */
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
}
