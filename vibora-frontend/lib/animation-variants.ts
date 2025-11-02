/**
 * Standardized Framer Motion animation variants for Vibora
 * 
 * These variants maintain the exact look and feel from the existing pages
 * while providing a consistent animation system across the application.
 */

import { Variants } from "framer-motion"

/**
 * Fade in with subtle upward movement
 * Used for: Individual elements, cards, sections
 */
export const FADE_IN_ANIMATION_VARIANTS: Variants = {
  hidden: { opacity: 0, y: 10 },
  show: { 
    opacity: 1, 
    y: 0, 
    transition: { 
      type: "spring" as const,
      stiffness: 300,
      damping: 30
    } 
  },
}

/**
 * Staggered container for child animations
 * Used for: Lists, grids, multiple elements
 */
export const STAGGER_CONTAINER_VARIANTS: Variants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2,
    },
  },
}

/**
 * Slide up with scale for more dramatic entrance
 * Used for: Important elements, modals, headers
 */
export const SLIDE_UP_VARIANTS: Variants = {
  hidden: { opacity: 0, y: 20, scale: 0.95 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      type: "spring" as const,
      stiffness: 300,
      damping: 30,
    },
  },
}

/**
 * Page transition variants for route changes
 * Used for: Page-level transitions in create-game flow
 */
export const PAGE_VARIANTS: Variants = {
  initial: { opacity: 0, x: 30 },
  in: { opacity: 1, x: 0 },
  out: { opacity: 0, x: -30 },
}

/**
 * Scale animation for interactive elements
 * Used for: Hover states, click feedback
 */
export const SCALE_VARIANTS: Variants = {
  hover: { 
    scale: 1.05,
    transition: { 
      type: "spring" as const, 
      stiffness: 400, 
      damping: 25 
    }
  },
  tap: { 
    scale: 0.95,
    transition: { 
      type: "spring" as const, 
      stiffness: 400, 
      damping: 25 
    }
  },
}

/**
 * Subtle scale for buttons and interactive elements
 * Used for: Icon buttons, smaller interactive elements
 */
export const SUBTLE_SCALE_VARIANTS: Variants = {
  hover: { 
    scale: 1.1,
    transition: { 
      type: "spring" as const, 
      stiffness: 400, 
      damping: 25 
    }
  },
  tap: { 
    scale: 0.9,
    transition: { 
      type: "spring" as const, 
      stiffness: 400, 
      damping: 25 
    }
  },
}

/**
 * Common transition settings
 */
export const PAGE_TRANSITION = { 
  duration: 0.4 
}

export const SPRING_TRANSITION = {
  type: "spring" as const,
  stiffness: 300,
  damping: 30,
}

export const QUICK_SPRING_TRANSITION = {
  type: "spring" as const,
  stiffness: 400,
  damping: 25,
}