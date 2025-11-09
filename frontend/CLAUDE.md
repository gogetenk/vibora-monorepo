# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

- **Development server**: `npm run dev` - Starts Next.js development server on port 3000
- **Build**: `npm run build` - Creates production build
- **Production start**: `npm run start` - Starts production server
- **Linting**: `npm run lint` - Runs Next.js ESLint checks

## Project Overview

Vibora is a padel matchmaking PWA built with Next.js 15, React 19, and Supabase. The application focuses on connecting padel players for matches through an intuitive, mobile-first interface.

### Tech Stack
- **Frontend**: Next.js 15 (App Router) + React 19
- **UI Framework**: Tailwind CSS v4 + Radix UI components
- **Database**: Supabase (PostgreSQL + Auth + Real-time)
- **State Management**: React built-in (useState, useEffect, Zustand for complex state)
- **Animations**: Framer Motion
- **Deployment**: Vercel (hobby plan)
- **Styling**: Tailwind CSS with custom design system

## Architecture

### Database Schema (Supabase)
The database follows a club-centric model with these main tables:
- `profiles` - User profiles linked to Supabase auth
- `clubs` - Padel clubs with location data
- `courts` - Individual courts within clubs
- `games` - Match instances with scheduling data
- `game_participants` - Many-to-many relationship for game membership
- `tournaments` - Tournament events
- `tournament_participants` - Tournament membership

### Key Features Architecture
1. **Authentication**: Supabase Auth with magic links
2. **Real-time Updates**: Supabase real-time subscriptions for live data
3. **PWA**: Next.js PWA configuration for mobile app-like experience
4. **Theme System**: Dark/light theme with next-themes
5. **Mobile Navigation**: Bottom tab navigation pattern

## File Structure

```
app/                          # Next.js App Router pages
├── (auth)/                   # Authentication routes
├── club/                     # Club management features  
├── courts/[id]/             # Court detail pages
├── create-game/             # Game creation flow
├── games/[id]/              # Game detail pages
├── join-game/[id]/          # Game joining flow
├── my-games/                # User's games overview
├── search/                  # Game search functionality
└── settings/                # User settings pages

components/                   # Reusable UI components
├── ui/                      # Base UI primitives (Radix + custom)
├── club/                    # Club-specific components
└── [feature-components]     # Feature-specific components

lib/                         # Utility libraries
├── supabase-client.ts       # Supabase client configuration
└── utils.ts                 # General utilities

types/                       # TypeScript type definitions
└── supabase.ts             # Generated Supabase types
```

## Code Conventions

### Component Organization
- Use Server Components by default, Client Components only when needed
- Place "use client" directive at the top of client components
- Prefer composition over complex prop drilling
- Use TypeScript interfaces for component props

### Styling Guidelines
- Use Tailwind CSS utility classes
- Follow mobile-first responsive design
- Implement dark/light theme support using CSS variables
- Use Framer Motion for animations with consistent variants

### Data Fetching Patterns
- Server Components: Direct Supabase calls in component
- Client Components: Use Zustand stores or React hooks
- Real-time features: Supabase real-time subscriptions
- Error handling: Try-catch blocks with user-friendly messages

## Development Guidelines

### Component Development
- Build components mobile-first (the app is primarily mobile)
- Use consistent animation patterns from Framer Motion
- Implement loading states and error boundaries
- Follow accessibility best practices (WCAG guidelines)

### Database Interactions
- Use Row Level Security (RLS) policies for data access control
- Prefer server-side data fetching when possible
- Use TypeScript types generated from Supabase schema
- Handle real-time subscriptions cleanup properly

### Authentication Flow
- Leverage Supabase Auth magic links for seamless login
- Implement proper session management
- Handle authentication state changes reactively
- Secure sensitive routes with middleware

## Key Business Logic

### Game Matching System
The core functionality revolves around connecting players for padel games:
- Players can create games with specific time slots and skill levels
- Other players can discover and join available games
- Real-time updates show game status changes
- Integration with club/court availability

### User Experience Patterns
- **Magic Links**: Allow participation without full registration
- **Progressive Enhancement**: Guest users → registered users
- **Smart Notifications**: Context-aware push notifications
- **Offline Support**: PWA caching for core functionality

## Third-Party Integrations

### Supabase Configuration
- Database URL and anon key configured via environment variables
- RLS policies enforce data security
- Real-time subscriptions for live updates
- Auth helpers for Next.js integration

### UI Components
- Radix UI for accessible component primitives
- Lucide React for consistent iconography
- Framer Motion for smooth animations
- Next Themes for theme switching

## Performance Considerations

- Next.js Image optimization for all images
- Lazy loading for non-critical components  
- PWA caching strategies for offline functionality
- Optimistic UI updates for better perceived performance

## Environment Setup

Required environment variables:
- `NEXT_PUBLIC_SUPABASE_URL`
- `NEXT_PUBLIC_SUPABASE_ANON_KEY`
- `SUPABASE_SERVICE_ROLE_KEY` (for server-side operations)
- `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` (requires Geocoding API, Places API, and Maps JavaScript API enabled)

The application is configured to ignore TypeScript and ESLint errors during builds for rapid iteration (see next.config.mjs).