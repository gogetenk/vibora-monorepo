"use client"

import { useState } from "react"
import { motion } from "framer-motion"
import { GooglePlacesInput } from "@/components/forms/GooglePlacesInput"
import { RadiusSlider } from "@/components/forms/RadiusSlider"
import { TimeSlotSelector, type TimeSlot } from "@/components/forms/TimeSlotSelector"
import { VInput, VLevelSelector } from "@/components/ui/vibora-form"
import { VStack } from "@/components/ui/vibora-layout"
import { MapPin, Loader2 } from "lucide-react"
import { FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"
import { reverseGeocode } from "@/lib/google-maps"

interface GameFormFieldsProps {
  // Date
  selectedDate: string
  onDateChange: (date: string) => void
  minDate: string

  // Time slots (for search/multi-select)
  selectedTimeSlots?: TimeSlot[]
  onTimeSlotsChange?: (slots: TimeSlot[]) => void
  multiSelect?: boolean

  // Single time (for create game)
  selectedTime?: string
  onTimeChange?: (time: string) => void

  // Location
  location: string
  onLocationChange: (value: string, coords?: { lat: number; lng: number }) => void
  coordinates?: { lat: number; lng: number } | null
  
  // Radius (optional, for search with GPS)
  radius?: number
  onRadiusChange?: (radius: number) => void
  showRadius?: boolean

  // Skill level
  skillLevel: string
  onSkillLevelChange: (level: string) => void
  
  // Auth context
  isAuthenticated?: boolean
  currentUserSkillLevel?: number
}

export function GameFormFields({
  selectedDate,
  onDateChange,
  minDate,
  selectedTimeSlots,
  onTimeSlotsChange,
  multiSelect = false,
  selectedTime,
  onTimeChange,
  location,
  onLocationChange,
  coordinates,
  radius = 10,
  onRadiusChange,
  showRadius = false,
  skillLevel,
  onSkillLevelChange,
  isAuthenticated = false,
  currentUserSkillLevel
}: GameFormFieldsProps) {
  const [isGeolocating, setIsGeolocating] = useState(false)

  const handleUseMyLocation = async () => {
    setIsGeolocating(true)

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const lat = position.coords.latitude
        const lng = position.coords.longitude

        // Reverse geocode to get city name
        const cityName = await reverseGeocode(lat, lng)

        // Use city name if available, fallback to "Ma position actuelle"
        onLocationChange(
          cityName || "Ma position actuelle",
          { lat, lng }
        )

        setIsGeolocating(false)
      },
      (error) => {
        console.error("Geolocation error:", error)
        setIsGeolocating(false)
      }
    )
  }

  return (
    <VStack spacing="xl">
      {/* Date & Time Section */}
      <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-3.5">
        <h2 className="text-base font-semibold">Quand ?</h2>
        
        <VStack spacing="md">
          <VInput
            label="Date"
            type="date"
            value={selectedDate}
            min={minDate}
            onChange={(e) => onDateChange(e.target.value)}
            required
          />

          {/* Multi-select time slots (for search) */}
          {multiSelect && selectedTimeSlots && onTimeSlotsChange ? (
            <div className="space-y-2">
              <label className="text-sm font-medium">
                Moment de la journée
                <span className="text-destructive ml-1">*</span>
              </label>
              <TimeSlotSelector
                multiSelect={true}
                selectedSlots={selectedTimeSlots}
                onSlotsChange={onTimeSlotsChange}
                selectedSlot={null}
                onSlotChange={() => {}}
              />
            </div>
          ) : (
            /* Single time input (for create game) */
            selectedTime !== undefined && onTimeChange && (
              <VInput
                label="Heure"
                type="time"
                value={selectedTime}
                onChange={(e) => onTimeChange(e.target.value)}
                required
              />
            )
          )}
        </VStack>
      </motion.div>

      {/* Divider */}
      <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="border-t border-border/20" />

      {/* Location Section */}
      <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-3.5">
        <h2 className="text-base font-semibold">Où ?</h2>
        
        <div className="space-y-3">
          <GooglePlacesInput
            value={location}
            onChange={onLocationChange}
            placeholder="Nom du club ou adresse"
          />

          {/* Geolocation button */}
          <button
            type="button"
            onClick={handleUseMyLocation}
            disabled={isGeolocating}
            className="text-[13px] text-primary hover:underline flex items-center gap-1.5 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isGeolocating ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <MapPin className="h-3.5 w-3.5" />
            )}
            {isGeolocating ? "Détection de la ville..." : "Utiliser ma position"}
          </button>

          {/* Radius slider (optional, for search) */}
          {showRadius && coordinates && onRadiusChange && (
            <RadiusSlider
              value={radius}
              onChange={onRadiusChange}
              min={5}
              max={50}
            />
          )}
        </div>
      </motion.div>

      {/* Divider */}
      <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="border-t border-border/20" />

      {/* Skill Level Section */}
      <motion.div variants={FADE_IN_ANIMATION_VARIANTS} className="space-y-3.5">
        <h2 className="text-base font-semibold">Niveau</h2>
        
        <VLevelSelector
          selectedLevel={skillLevel}
          onLevelChange={onSkillLevelChange}
          showCompatibilityHint={false}
        />
      </motion.div>
    </VStack>
  )
}
