"use client"

import React from "react"
import { motion, MotionProps } from "framer-motion"
import { Button, ButtonProps } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { SCALE_VARIANTS, SUBTLE_SCALE_VARIANTS } from "@/lib/animation-variants"

interface InteractiveButtonProps extends Omit<ButtonProps, "whileHover" | "whileTap"> {
  /**
   * Animation intensity
   * - "subtle": Small scale animation (1.1/0.9) for icon buttons
   * - "normal": Medium scale animation (1.05/0.95) for regular buttons  
   * - "none": No animation
   */
  animation?: "subtle" | "normal" | "none"
  /**
   * Custom motion props - will override default animations
   */
  motionProps?: MotionProps
}

/**
 * Interactive button with built-in Framer Motion animations
 * Based on the animation patterns used throughout Vibora
 */
export const InteractiveButton = React.forwardRef<
  HTMLButtonElement,
  InteractiveButtonProps
>(({ 
  className, 
  animation = "normal",
  motionProps,
  children,
  ...props 
}, ref) => {
  // Select animation variant based on animation prop
  const getAnimationProps = () => {
    if (animation === "none") {
      return {}
    }
    if (animation === "subtle") {
      return {
        whileHover: "hover",
        whileTap: "tap",
        variants: SUBTLE_SCALE_VARIANTS,
        ...motionProps,
      }
    }
    // Default to normal animation
    return {
      whileHover: "hover", 
      whileTap: "tap",
      variants: SCALE_VARIANTS,
      ...motionProps,
    }
  }

  const MotionButton = motion(Button)

  return (
    <MotionButton
      ref={ref}
      className={className}
      {...getAnimationProps()}
      {...props}
    >
      {children}
    </MotionButton>
  )
})

InteractiveButton.displayName = "InteractiveButton"

/**
 * Specialized interactive button variants for common use cases
 */

// Icon button with subtle animation (matches the header icons)
export const InteractiveIconButton = React.forwardRef<
  HTMLButtonElement,
  Omit<InteractiveButtonProps, "animation">
>(({ className, ...props }, ref) => {
  return (
    <InteractiveButton
      ref={ref}
      variant="ghost"
      size="icon"
      animation="subtle"
      className={cn("rounded-full hover:bg-accent/50 transition-colors", className)}
      {...props}
    />
  )
})

InteractiveIconButton.displayName = "InteractiveIconButton"

// Primary action button with normal animation
export const InteractivePrimaryButton = React.forwardRef<
  HTMLButtonElement,
  Omit<InteractiveButtonProps, "animation" | "variant">
>(({ className, ...props }, ref) => {
  return (
    <InteractiveButton
      ref={ref}
      variant="default"
      animation="normal"
      className={cn("rounded-xl", className)}
      {...props}
    />
  )
})

InteractivePrimaryButton.displayName = "InteractivePrimaryButton"

// Large form button (matches create-game button style)
export const InteractiveFormButton = React.forwardRef<
  HTMLButtonElement,
  Omit<InteractiveButtonProps, "animation" | "size">
>(({ className, ...props }, ref) => {
  return (
    <InteractiveButton
      ref={ref}
      size="lg"
      animation="normal"
      className={cn("w-full h-14 text-md font-md rounded-xl", className)}
      {...props}
    />
  )
})

InteractiveFormButton.displayName = "InteractiveFormButton"

// Avatar button with special hover effect (matches profile avatar)
export const InteractiveAvatarButton = React.forwardRef<
  HTMLButtonElement,
  Omit<InteractiveButtonProps, "animation" | "variant" | "size">
>(({ className, children, ...props }, ref) => {
  return (
    <motion.button
      ref={ref}
      className={cn("focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 rounded-full", className)}
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      transition={{ type: "spring", stiffness: 400, damping: 25 }}
      {...props}
    >
      {children}
    </motion.button>
  )
})

InteractiveAvatarButton.displayName = "InteractiveAvatarButton"