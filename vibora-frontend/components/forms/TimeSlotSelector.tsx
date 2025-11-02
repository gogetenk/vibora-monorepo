"use client"

import React from "react"
import { motion } from "framer-motion"
import { Sunrise, Sun, Sunset } from "lucide-react"
import { cn } from "@/lib/utils"

/**
 * TimeSlotSelector - Premium time range selector for game search
 *
 * Replaces precise time input with quick time ranges:
 * - Matinée (Morning): 08:00-12:00 → defaults to 10:00
 * - Après-midi (Afternoon): 12:00-18:00 → defaults to 15:00
 * - Soirée (Evening): 18:00-23:00 → defaults to 20:00
 *
 * Design: Vibora premium style with smooth animations and clear visual feedback
 */

export type TimeSlot = "morning" | "afternoon" | "evening"

interface TimeSlotOption {
  id: TimeSlot
  label: string
  shortLabel: string
  icon: React.ElementType
  defaultTime: string // HH:mm format
  description: string
  gradient: string
  accentColor: string
}

const TIME_SLOTS: TimeSlotOption[] = [
  {
    id: "morning",
    label: "Matinée",
    shortLabel: "Matin",
    icon: Sunrise,
    defaultTime: "10:00",
    description: "08:00 - 12:00",
    gradient: "from-amber-50 via-orange-50 to-yellow-50 dark:from-amber-950/30 dark:via-orange-950/20 dark:to-yellow-950/10",
    accentColor: "border-amber-300 dark:border-amber-700 bg-gradient-to-br from-amber-100 to-orange-100 dark:from-amber-900/50 dark:to-orange-900/30"
  },
  {
    id: "afternoon",
    label: "Après-midi",
    shortLabel: "A-midi",
    icon: Sun,
    defaultTime: "15:00",
    description: "12:00 - 18:00",
    gradient: "from-sky-50 via-blue-50 to-cyan-50 dark:from-sky-950/30 dark:via-blue-950/20 dark:to-cyan-950/10",
    accentColor: "border-sky-300 dark:border-sky-700 bg-gradient-to-br from-sky-100 to-blue-100 dark:from-sky-900/50 dark:to-blue-900/30"
  },
  {
    id: "evening",
    label: "Soirée",
    shortLabel: "Soir",
    icon: Sunset,
    defaultTime: "20:00",
    description: "18:00 - 23:00",
    gradient: "from-indigo-50 via-purple-50 to-violet-50 dark:from-indigo-950/30 dark:via-purple-950/20 dark:to-violet-950/10",
    accentColor: "border-indigo-300 dark:border-indigo-700 bg-gradient-to-br from-indigo-100 to-purple-100 dark:from-indigo-900/50 dark:to-purple-900/30"
  }
]

export interface TimeSlotSelectorProps {
  selectedSlot: TimeSlot | null
  selectedSlots?: TimeSlot[]
  onSlotChange: (slot: TimeSlot, defaultTime: string) => void
  onSlotsChange?: (slots: TimeSlot[]) => void
  className?: string
  disabled?: boolean
  multiSelect?: boolean
}

export function TimeSlotSelector({
  selectedSlot,
  selectedSlots = [],
  onSlotChange,
  onSlotsChange,
  className,
  disabled = false,
  multiSelect = false
}: TimeSlotSelectorProps) {

  const handleSlotClick = (slot: TimeSlotOption) => {
    if (disabled) return
    
    if (multiSelect && onSlotsChange) {
      // Multi-select logic
      const isSelected = selectedSlots.includes(slot.id)
      if (isSelected) {
        // Deselect
        onSlotsChange(selectedSlots.filter(s => s !== slot.id))
      } else {
        // Select
        onSlotsChange([...selectedSlots, slot.id])
      }
    } else {
      // Single select logic
      onSlotChange(slot.id, slot.defaultTime)
    }
  }

  return (
    <div className={cn("space-y-4", className)}>
      {/* Grid of time slot chips */}
      <div className="grid grid-cols-3 gap-3">
        {TIME_SLOTS.map((slot, index) => {
          const isSelected = multiSelect ? selectedSlots.includes(slot.id) : selectedSlot === slot.id
          const Icon = slot.icon

          return (
            <motion.button
              key={slot.id}
              type="button"
              onClick={() => handleSlotClick(slot)}
              disabled={disabled}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{
                delay: index * 0.08,
                type: "spring",
                stiffness: 400,
                damping: 25
              }}
              whileHover={!disabled ? {
                y: -4,
                scale: 1.02,
                transition: { duration: 0.2 }
              } : {}}
              whileTap={!disabled ? {
                scale: 0.97,
                transition: { duration: 0.1 }
              } : {}}
              className={cn(
                "relative flex flex-col items-center gap-3 p-4 rounded-2xl",
                "border-2 transition-all duration-300",
                "focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2",
                "group overflow-hidden",
                disabled && "opacity-50 cursor-not-allowed",
                isSelected
                  ? cn(
                      slot.accentColor,
                      "shadow-lg hover:shadow-xl"
                    )
                  : "border-border bg-card/50 hover:bg-card hover:border-border/80 hover:shadow-md"
              )}
            >
              {/* Animated gradient background (only when selected) */}
              {isSelected && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ duration: 0.4 }}
                  className={cn(
                    "absolute inset-0 opacity-20",
                    `bg-gradient-to-br ${slot.gradient}`
                  )}
                />
              )}

              {/* Icon with smooth color transition */}
              <div className={cn(
                "relative w-10 h-10 rounded-full flex items-center justify-center",
                "transition-all duration-300",
                isSelected
                  ? "bg-white/80 dark:bg-black/20"
                  : "bg-muted/50 group-hover:bg-muted"
              )}>
                <Icon
                  className={cn(
                    "w-5 h-5 transition-colors duration-300",
                    isSelected
                      ? "text-foreground"
                      : "text-muted-foreground group-hover:text-foreground"
                  )}
                  strokeWidth={2.5}
                />
              </div>

              {/* Label */}
              <div className="relative text-center space-y-0.5">
                <div className={cn(
                  "font-semibold text-sm transition-colors duration-200",
                  isSelected
                    ? "text-foreground"
                    : "text-foreground/80 group-hover:text-foreground"
                )}>
                  {slot.label}
                </div>
                <div className={cn(
                  "text-xs transition-colors duration-200",
                  isSelected
                    ? "text-foreground/70"
                    : "text-muted-foreground group-hover:text-muted-foreground/90"
                )}>
                  {slot.description}
                </div>
              </div>

              {/* Selected indicator - subtle pulse */}
              {isSelected && (
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{
                    type: "spring",
                    stiffness: 500,
                    damping: 20
                  }}
                  className="absolute top-2 right-2"
                >
                  <div className="w-2 h-2 rounded-full bg-primary animate-pulse shadow-sm" />
                </motion.div>
              )}
            </motion.button>
          )
        })}
      </div>

      {/* Hint text - shows selected time(s) */}
      {!multiSelect && selectedSlot && (
        <motion.div
          initial={{ opacity: 0, y: -5 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="text-center"
        >
          <p className="text-sm text-muted-foreground">
            Recherche autour de{" "}
            <span className="font-medium text-foreground">
              {TIME_SLOTS.find(s => s.id === selectedSlot)?.defaultTime}
            </span>
          </p>
        </motion.div>
      )}
      {multiSelect && selectedSlots.length > 0 && (
        <motion.div
          initial={{ opacity: 0, y: -5 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="text-center"
        >
          <p className="text-sm text-muted-foreground">
            {selectedSlots.length} moment{selectedSlots.length > 1 ? 's' : ''} sélectionné{selectedSlots.length > 1 ? 's' : ''}
          </p>
        </motion.div>
      )}
    </div>
  )
}

/**
 * Helper function: Get TimeSlot from time string (HH:mm)
 * Used for reverse mapping when prefilling from URL params
 */
export function getTimeSlotFromTime(time: string): TimeSlot {
  const [hours] = time.split(":").map(Number)

  if (hours >= 8 && hours < 12) return "morning"
  if (hours >= 12 && hours < 18) return "afternoon"
  return "evening"
}

/**
 * Helper function: Get default time for a TimeSlot
 */
export function getDefaultTimeForSlot(slot: TimeSlot): string {
  return TIME_SLOTS.find(s => s.id === slot)?.defaultTime || "10:00"
}
