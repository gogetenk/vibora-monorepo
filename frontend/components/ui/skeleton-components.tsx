"use client"

import React from "react"
import { cn } from "@/lib/utils"

interface SkeletonProps {
  className?: string
  children?: React.ReactNode
}

/**
 * Base skeleton component with Vibora's styling
 */
export function SkeletonCard({ className = "", children }: SkeletonProps) {
  return (
    <div className={cn("animate-pulse bg-muted/50 rounded-lg", className)}>
      {children}
    </div>
  )
}

/**
 * Skeleton avatar with configurable size
 */
export function SkeletonAvatar({ 
  size = "w-10 h-10",
  className 
}: { 
  size?: string
  className?: string 
}) {
  return (
    <div className={cn(`${size} bg-muted/60 rounded-full animate-pulse`, className)} />
  )
}

/**
 * Skeleton text with configurable dimensions
 */
export function SkeletonText({ 
  width = "w-full", 
  height = "h-4",
  className 
}: { 
  width?: string
  height?: string
  className?: string 
}) {
  return (
    <div className={cn(`${width} ${height} bg-muted/60 rounded animate-pulse`, className)} />
  )
}

/**
 * Skeleton for game cards in the upcoming games section
 */
export function SkeletonGameCard({ className }: { className?: string }) {
  return (
    <SkeletonCard className={cn("shrink-0 w-[280px] h-[240px] snap-start", className)}>
      <div className="h-40 bg-muted/70 rounded-t-lg animate-pulse" />
      <div className="p-3 space-y-3">
        <div className="flex items-center justify-between">
          <SkeletonText width="w-24" height="h-3" />
          <div className="flex -space-x-2">
            <SkeletonAvatar size="w-6 h-6" />
            <SkeletonAvatar size="w-6 h-6" />
            <SkeletonAvatar size="w-6 h-6" />
          </div>
        </div>
      </div>
    </SkeletonCard>
  )
}

/**
 * Skeleton for available game cards
 */
export function SkeletonAvailableGameCard({ className }: { className?: string }) {
  return (
    <SkeletonCard className={cn("p-4", className)}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="text-center space-y-1">
            <SkeletonText width="w-8" height="h-6" />
            <SkeletonText width="w-6" height="h-3" />
          </div>
          <div className="space-y-2">
            <SkeletonText width="w-32" height="h-4" />
            <SkeletonText width="w-24" height="h-3" />
          </div>
        </div>
        <SkeletonAvatar size="w-10 h-10" />
      </div>
    </SkeletonCard>
  )
}

/**
 * Skeleton for filter chips
 */
export function SkeletonFilterChip({ className }: { className?: string }) {
  return (
    <SkeletonCard className={cn("shrink-0 h-9 w-20 rounded-full", className)} />
  )
}

/**
 * Skeleton for section headers
 */
export function SkeletonSectionHeader({ className }: { className?: string }) {
  return (
    <div className={cn("flex items-center justify-between mb-4", className)}>
      <SkeletonText width="w-32" height="h-5" />
      <SkeletonText width="w-16" height="h-4" />
    </div>
  )
}

/**
 * Skeleton for form inputs
 */
export function SkeletonInput({ className }: { className?: string }) {
  return (
    <SkeletonCard className={cn("h-12 rounded-xl", className)} />
  )
}

/**
 * Skeleton for buttons
 */
export function SkeletonButton({ 
  className,
  size = "default"
}: { 
  className?: string
  size?: "default" | "sm" | "lg"
}) {
  const sizeClasses = {
    default: "h-10",
    sm: "h-9",
    lg: "h-14"
  }
  
  return (
    <SkeletonCard className={cn(
      `${sizeClasses[size]} rounded-xl px-8`,
      className
    )} />
  )
}

/**
 * Skeleton for the main page header with avatar and actions
 */
export function SkeletonPageHeader({ className }: { className?: string }) {
  return (
    <div className={cn("flex items-center justify-between h-20", className)}>
      <SkeletonAvatar size="w-10 h-10" />
      <div className="flex items-center gap-2">
        <SkeletonAvatar size="w-10 h-10" />
        <SkeletonAvatar size="w-10 h-10" />
        <SkeletonAvatar size="w-10 h-10" />
      </div>
    </div>
  )
}

/**
 * Complete skeleton for the home page
 */
export function SkeletonHomePage() {
  return (
    <div className="min-h-screen bg-background text-foreground pb-32">
      <header className="sticky top-0 z-40 bg-background/80 backdrop-blur-lg">
        <div className="container">
          <SkeletonPageHeader />
        </div>
      </header>

      <main className="container space-y-8 pt-6">
        {/* Welcome message */}
        <div className="space-y-2">
          <SkeletonText width="w-48" height="h-8" />
          <SkeletonText width="w-36" height="h-6" />
        </div>

        {/* Quick filters */}
        <div className="-mx-6">
          <div className="flex gap-3 pb-2 px-6 overflow-hidden">
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonFilterChip key={i} />
            ))}
          </div>
        </div>

        {/* Upcoming games */}
        <div>
          <SkeletonSectionHeader />
          <div className="-mx-6">
            <div className="flex gap-4 px-6 overflow-hidden">
              {Array.from({ length: 3 }).map((_, i) => (
                <SkeletonGameCard key={i} />
              ))}
            </div>
          </div>
        </div>

        {/* Available games */}
        <div>
          <SkeletonSectionHeader />
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <SkeletonAvailableGameCard key={i} />
            ))}
          </div>
        </div>
      </main>
    </div>
  )
}