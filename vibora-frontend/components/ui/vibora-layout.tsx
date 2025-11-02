"use client"

import React from "react"
import { motion } from "framer-motion"
import { cn } from "@/lib/utils"
import { STAGGER_CONTAINER_VARIANTS, FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"

/**
 * Main container following Vibora's layout system
 * Uses the container utility class defined in globals.css
 */
interface VContainerProps {
  children: React.ReactNode
  className?: string
  animate?: boolean
  fullWidth?: boolean
}

export function VContainer({ 
  children, 
  className, 
  animate = false,
  fullWidth = false 
}: VContainerProps) {
  const containerClass = fullWidth ? "w-full px-6" : "container"
  
  if (animate) {
    return (
      <motion.div
        initial="hidden"
        animate="show"
        variants={STAGGER_CONTAINER_VARIANTS}
        className={cn(containerClass, className)}
      >
        {children}
      </motion.div>
    )
  }

  return (
    <div className={cn(containerClass, className)}>
      {children}
    </div>
  )
}

/**
 * Page wrapper with consistent styling and optional animations
 */
interface VPageProps {
  children: React.ReactNode
  className?: string
  animate?: boolean
  withPadding?: boolean
}

export function VPage({ 
  children, 
  className, 
  animate = true,
  withPadding = true 
}: VPageProps) {
  const baseClasses = "min-h-screen bg-background text-foreground"
  const paddingClasses = withPadding ? "pb-32" : ""
  
  if (animate) {
    return (
      <motion.div
        initial="hidden"
        animate="show"
        variants={STAGGER_CONTAINER_VARIANTS}
        className={cn(baseClasses, paddingClasses, className)}
      >
        {children}
      </motion.div>
    )
  }

  return (
    <div className={cn(baseClasses, paddingClasses, className)}>
      {children}
    </div>
  )
}

/**
 * Section wrapper with consistent spacing
 */
interface VSectionProps {
  children: React.ReactNode
  className?: string
  animate?: boolean
  spacing?: "sm" | "md" | "lg"
}

export function VSection({ 
  children, 
  className, 
  animate = false,
  spacing = "md" 
}: VSectionProps) {
  const spacingClasses = {
    sm: "space-y-4",
    md: "space-y-6", 
    lg: "space-y-8"
  }

  if (animate) {
    return (
      <motion.section
        variants={FADE_IN_ANIMATION_VARIANTS}
        className={cn(spacingClasses[spacing], className)}
      >
        {children}
      </motion.section>
    )
  }

  return (
    <section className={cn(spacingClasses[spacing], className)}>
      {children}
    </section>
  )
}

/**
 * Vertical stack with consistent spacing
 */
interface VStackProps {
  children: React.ReactNode
  className?: string
  spacing?: "xs" | "sm" | "md" | "lg" | "xl"
  align?: "start" | "center" | "end" | "stretch"
  animate?: boolean
}

export function VStack({ 
  children, 
  className, 
  spacing = "md",
  align = "stretch",
  animate = false 
}: VStackProps) {
  const spacingClasses = {
    xs: "space-y-2",
    sm: "space-y-3",
    md: "space-y-4",
    lg: "space-y-6",
    xl: "space-y-8"
  }

  const alignClasses = {
    start: "items-start",
    center: "items-center", 
    end: "items-end",
    stretch: "items-stretch"
  }

  const baseClasses = cn(
    "flex flex-col",
    spacingClasses[spacing],
    alignClasses[align]
  )

  if (animate) {
    return (
      <motion.div
        variants={STAGGER_CONTAINER_VARIANTS}
        className={cn(baseClasses, className)}
      >
        {children}
      </motion.div>
    )
  }

  return (
    <div className={cn(baseClasses, className)}>
      {children}
    </div>
  )
}

/**
 * Horizontal scroll container (used for game cards, filters)
 */
interface VScrollContainerProps {
  children: React.ReactNode
  className?: string
  enableSnap?: boolean
  edgeToEdge?: boolean
  padding?: "sm" | "md" | "lg"
}

export function VScrollContainer({ 
  children, 
  className,
  enableSnap = true,
  edgeToEdge = true,
  padding = "md"
}: VScrollContainerProps) {
  const paddingClasses = {
    sm: "px-4",
    md: "px-6", 
    lg: "px-8"
  }

  const outerClasses = edgeToEdge ? "-mx-6" : ""
  const innerClasses = cn(
    "flex gap-4 overflow-x-auto hide-scrollbar",
    enableSnap && "snap-x snap-mandatory scroll-px-6",
    paddingClasses[padding]
  )

  return (
    <div className={outerClasses}>
      <div className={cn(innerClasses, className)}>
        {children}
      </div>
    </div>
  )
}

/**
 * Grid container with responsive columns
 */
interface VGridProps {
  children: React.ReactNode
  className?: string
  cols?: 1 | 2 | 3 | 4 | 5 | 6
  gap?: "sm" | "md" | "lg"
  responsive?: boolean
}

export function VGrid({ 
  children, 
  className,
  cols = 1,
  gap = "md",
  responsive = true 
}: VGridProps) {
  const colClasses = responsive 
    ? "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
    : `grid-cols-${cols}`

  const gapClasses = {
    sm: "gap-3",
    md: "gap-4",
    lg: "gap-6"
  }

  return (
    <div className={cn(
      "grid",
      colClasses,
      gapClasses[gap],
      className
    )}>
      {children}
    </div>
  )
}

/**
 * Header layout component (sticky header with backdrop blur)
 */
interface VHeaderProps {
  children: React.ReactNode
  className?: string
  sticky?: boolean
  animate?: boolean
}

export function VHeader({ 
  children, 
  className,
  sticky = true,
  animate = true 
}: VHeaderProps) {
  const baseClasses = cn(
    "bg-background/80 backdrop-blur-lg border-b border-border/50",
    sticky && "sticky top-0 z-40"
  )

  if (animate) {
    return (
      <motion.header
        variants={FADE_IN_ANIMATION_VARIANTS}
        className={cn(baseClasses, className)}
      >
        {children}
      </motion.header>
    )
  }

  return (
    <header className={cn(baseClasses, className)}>
      {children}
    </header>
  )
}

/**
 * Main content area wrapper
 */
interface VMainProps {
  children: React.ReactNode
  className?: string
  containerized?: boolean
  spacing?: "sm" | "md" | "lg"
}

export function VMain({ 
  children, 
  className,
  containerized = true,
  spacing = "md" 
}: VMainProps) {
  const spacingClasses = {
    sm: "py-4",
    md: "py-6",
    lg: "py-8"
  }

  if (containerized) {
    return (
      <main className={cn(spacingClasses[spacing], className)}>
        <VContainer>
          {children}
        </VContainer>
      </main>
    )
  }

  return (
    <main className={cn(spacingClasses[spacing], className)}>
      {children}
    </main>
  )
}

/**
 * Card-like content wrapper with backdrop blur
 */
interface VContentCardProps {
  children: React.ReactNode
  className?: string
  variant?: "default" | "elevated" | "subtle"
  animate?: boolean
}

export function VContentCard({ 
  children, 
  className,
  variant = "default",
  animate = false 
}: VContentCardProps) {
  const variantClasses = {
    default: "bg-card/80 backdrop-blur-sm border-border/50",
    elevated: "bg-card backdrop-blur-md border-border shadow-lg",
    subtle: "bg-card/60 backdrop-blur-sm border-border/30"
  }

  const baseClasses = cn(
    "rounded-xl border transition-all duration-200",
    variantClasses[variant]
  )

  if (animate) {
    return (
      <motion.div
        variants={FADE_IN_ANIMATION_VARIANTS}
        className={cn(baseClasses, className)}
      >
        {children}
      </motion.div>
    )
  }

  return (
    <div className={cn(baseClasses, className)}>
      {children}
    </div>
  )
}