"use client"

import React, { useState, useRef, useEffect } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Button } from "@/components/ui/button"
import { MapPin, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { FADE_IN_ANIMATION_VARIANTS } from "@/lib/animation-variants"
import { useLocationSuggestions } from "@/hooks/use-geolocation"

/**
 * Styled input component with Vibora's form styling
 */
interface VInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  required?: boolean
  error?: string
  containerClassName?: string
}

export const VInput = React.forwardRef<HTMLInputElement, VInputProps>(
  ({ label, required, error, containerClassName, className, ...props }, ref) => {
    return (
      <div className={cn("space-y-3", containerClassName)}>
        {label && (
          <Label className="font-medium">
            {label}
            {required && <span className="text-destructive ml-1">*</span>}
          </Label>
        )}
        <Input
          ref={ref}
          className={cn(
            "bg-card border-border h-12 rounded-xl text-base focus:ring-2 focus:ring-primary focus:border-primary",
            error && "border-destructive focus:ring-destructive focus:border-destructive",
            className
          )}
          {...props}
        />
        {error && (
          <p className="text-sm text-destructive mt-1">{error}</p>
        )}
      </div>
    )
  }
)
VInput.displayName = "VInput"

/**
 * Styled select component with Vibora's form styling
 */
interface VSelectProps {
  label?: string
  required?: boolean
  error?: string
  placeholder?: string
  value?: string
  onValueChange?: (value: string) => void
  children: React.ReactNode
  containerClassName?: string
  triggerClassName?: string
  disabled?: boolean
}

export function VSelect({
  label,
  required,
  error,
  placeholder,
  value,
  onValueChange,
  children,
  containerClassName,
  triggerClassName,
  disabled,
}: VSelectProps) {
  return (
    <div className={cn("space-y-3", containerClassName)}>
      {label && (
        <Label className="font-medium">
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </Label>
      )}
      <Select value={value} onValueChange={onValueChange} disabled={disabled}>
        <SelectTrigger
          className={cn(
            "h-12 rounded-xl bg-card border-border text-base focus:ring-2 focus:ring-primary focus:border-primary",
            error && "border-destructive focus:ring-destructive focus:border-destructive",
            triggerClassName
          )}
        >
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {children}
        </SelectContent>
      </Select>
      {error && (
        <p className="text-sm text-destructive mt-1">{error}</p>
      )}
    </div>
  )
}

/**
 * Styled button with Vibora's form button styling
 */
interface VButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "outline" | "ghost"
  size?: "default" | "sm" | "lg" | "form"
  loading?: boolean
  children: React.ReactNode
}

export const VButton = React.forwardRef<HTMLButtonElement, VButtonProps>(
  ({ variant = "primary", size = "default", loading, children, className, disabled, ...props }, ref) => {
    const getVariantClasses = () => {
      switch (variant) {
        case "primary":
          return "bg-primary text-primary-foreground hover:bg-primary/90"
        case "secondary":
          return "bg-secondary text-secondary-foreground hover:bg-secondary/80"
        case "outline":
          return "border border-border bg-card hover:bg-accent hover:text-accent-foreground"
        case "ghost":
          return "hover:bg-accent hover:text-accent-foreground"
        default:
          return "bg-primary text-primary-foreground hover:bg-primary/90"
      }
    }

    const getSizeClasses = () => {
      switch (size) {
        case "sm":
          return "h-9 px-3 text-sm rounded-lg"
        case "lg":
          return "h-12 px-6 text-base rounded-xl"
        case "form":
          return "w-full h-14 text-md font-md rounded-xl"
        default:
          return "h-10 px-4 text-sm rounded-xl"
      }
    }

    return (
      <Button
        ref={ref}
        className={cn(
          "transition-all duration-200 focus:ring-2 focus:ring-ring focus:ring-offset-2",
          getVariantClasses(),
          getSizeClasses(),
          loading && "opacity-50 cursor-not-allowed",
          className
        )}
        disabled={disabled || loading}
        {...props}
      >
        {loading ? (
          <div className="flex items-center gap-2">
            <div className="animate-spin rounded-full h-4 w-4 border-2 border-current border-t-transparent" />
            {children}
          </div>
        ) : (
          children
        )}
      </Button>
    )
  }
)
VButton.displayName = "VButton"

/**
 * Form container with consistent spacing and animation
 */
interface VFormProps {
  children: React.ReactNode
  className?: string
  animate?: boolean
}

export function VForm({ children, className, animate = true }: VFormProps) {
  if (animate) {
    return (
      <motion.div
        initial="hidden"
        animate="show"
        variants={FADE_IN_ANIMATION_VARIANTS}
        className={cn("space-y-8", className)}
      >
        {children}
      </motion.div>
    )
  }

  return (
    <div className={cn("space-y-8", className)}>
      {children}
    </div>
  )
}

/**
 * Form field wrapper with consistent spacing
 */
interface VFormFieldProps {
  children: React.ReactNode
  className?: string
}

export function VFormField({ children, className }: VFormFieldProps) {
  return (
    <div className={cn("space-y-3", className)}>
      {children}
    </div>
  )
}

/**
 * Form section wrapper with larger spacing
 */
interface VFormSectionProps {
  children: React.ReactNode
  className?: string
  title?: string
  subtitle?: string
}

export function VFormSection({ children, className, title, subtitle }: VFormSectionProps) {
  return (
    <div className={cn("space-y-6", className)}>
      {(title || subtitle) && (
        <div className="space-y-1">
          {title && <h3 className="text-lg font-semibold">{title}</h3>}
          {subtitle && <p className="text-sm text-muted-foreground">{subtitle}</p>}
        </div>
      )}
      {children}
    </div>
  )
}

/**
 * Time slot buttons grid (used in create-game)
 */
interface VTimeSlotProps {
  options: Array<{ id: string; label: string }>
  selectedIds: string[]
  onToggle: (id: string) => void
  className?: string
}

export function VTimeSlotGrid({ options, selectedIds, onToggle, className }: VTimeSlotProps) {
  return (
    <div className={cn("grid grid-cols-3 gap-3", className)}>
      {options.map((option) => (
        <VButton
          key={option.id}
          variant="outline"
          onClick={() => onToggle(option.id)}
          className={cn(
            "h-12 rounded-xl border-border bg-card hover:bg-accent hover:text-accent-foreground",
            selectedIds.includes(option.id) &&
              "bg-primary text-primary-foreground border-primary hover:bg-primary/90"
          )}
        >
          {option.label.split(" ")[0]}
        </VButton>
      ))}
    </div>
  )
}

/**
 * Smart location input with geolocation and suggestions
 */
interface VLocationInputProps {
  label?: string
  placeholder?: string
  value: string
  onChange: (value: string) => void
  required?: boolean
  error?: string
  className?: string
}

export function VLocationInput({
  label = "Lieu",
  placeholder = "Zone, club, adresse...",
  value,
  onChange,
  required,
  error,
  className
}: VLocationInputProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState("")
  const [isGettingLocation, setIsGettingLocation] = useState(false)
  
  const { 
    suggestions, 
    searchSuggestions, 
    addRecentLocation, 
    getCurrentLocationSuggestion 
  } = useLocationSuggestions()
  
  const inputRef = useRef<HTMLInputElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  // Fermeture du dropdown au clic extérieur
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
        setSearchQuery("")
      }
    }

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside)
      return () => document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [isOpen])

  // Gestion de la géolocalisation
  const handleGetCurrentLocation = async () => {
    setIsGettingLocation(true)
    try {
      const locationSuggestion = await getCurrentLocationSuggestion()
      if (locationSuggestion) {
        onChange(locationSuggestion.name)
        addRecentLocation(locationSuggestion.name)
        setIsOpen(false)
        setSearchQuery("")
      }
    } finally {
      setIsGettingLocation(false)
    }
  }

  // Gestion de la sélection d'une suggestion
  const handleSelectSuggestion = (suggestion: { name: string }) => {
    onChange(suggestion.name)
    addRecentLocation(suggestion.name)
    setIsOpen(false)
    setSearchQuery("")
    inputRef.current?.blur()
  }

  // Gestion de la saisie
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value
    onChange(newValue)
    setSearchQuery(newValue)
    setIsOpen(newValue.length > 0 || suggestions.length > 0)
  }

  // Obtention des suggestions à afficher
  const displaySuggestions = searchQuery.length > 0 
    ? searchSuggestions(searchQuery)
    : suggestions

  const getSuggestionIcon = (type: string) => {
    switch (type) {
      case "current": return <MapPin className="w-4 h-4 text-primary" />
      case "recent": return <MapPin className="w-4 h-4 text-muted-foreground" />
      case "popular": return <MapPin className="w-4 h-4 text-success" />
      default: return <MapPin className="w-4 h-4 text-muted-foreground" />
    }
  }

  return (
    <div ref={containerRef} className={cn("relative space-y-3", className)}>
      {label && (
        <Label className="font-medium">
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </Label>
      )}
      
      <div className="relative">
        <Input
          ref={inputRef}
          value={value}
          onChange={handleInputChange}
          onFocus={() => setIsOpen(true)}
          placeholder={placeholder}
          className={cn(
            "bg-card border-border h-12 rounded-xl text-base pr-12 focus:ring-2 focus:ring-primary focus:border-primary",
            error && "border-destructive focus:ring-destructive focus:border-destructive"
          )}
        />
        
        {/* Bouton de géolocalisation */}
        <motion.button
          type="button"
          onClick={handleGetCurrentLocation}
          disabled={isGettingLocation}
          whileTap={{ scale: 0.95 }}
          className={cn(
            "absolute right-2 top-1/2 -translate-y-1/2 w-8 h-8 rounded-lg",
            "flex items-center justify-center transition-colors",
            "hover:bg-primary/10 focus:bg-primary/10 focus:outline-none",
            isGettingLocation && "opacity-50 cursor-not-allowed"
          )}
        >
          {isGettingLocation ? (
            <div className="animate-spin w-4 h-4 border-2 border-primary border-t-transparent rounded-full" />
          ) : (
            <MapPin className="w-4 h-4 text-primary" />
          )}
        </motion.button>
      </div>

      {/* Dropdown des suggestions */}
      <AnimatePresence>
        {isOpen && displaySuggestions.length > 0 && (
          <motion.div
            initial={{ opacity: 0, y: -10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -10, scale: 0.95 }}
            transition={{ duration: 0.15 }}
            className={cn(
              "absolute top-full left-0 right-0 z-50 mt-2",
              "bg-card border border-border rounded-xl shadow-xl overflow-hidden"
            )}
          >
            {/* Option de géolocalisation en première position si pas de recherche */}
            {searchQuery.length === 0 && (
              <motion.button
                type="button"
                onClick={handleGetCurrentLocation}
                disabled={isGettingLocation}
                className={cn(
                  "w-full flex items-center gap-3 p-4 text-left transition-colors",
                  "hover:bg-primary/5 focus:bg-primary/5 focus:outline-none",
                  "border-b border-border/50",
                  isGettingLocation && "opacity-50 cursor-not-allowed"
                )}
                whileHover={{ backgroundColor: "rgba(var(--primary), 0.05)" }}
                whileTap={{ scale: 0.98 }}
              >
                <div className={cn(
                  "w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center",
                  isGettingLocation && "animate-pulse"
                )}>
                  {isGettingLocation ? (
                    <div className="animate-spin w-4 h-4 border-2 border-primary border-t-transparent rounded-full" />
                  ) : (
                    <MapPin className="w-5 h-5 text-primary" />
                  )}
                </div>
                <div className="flex-1">
                  <div className="font-semibold text-primary">
                    Me localiser
                  </div>
                  <div className="text-sm text-muted-foreground">
                    Utiliser ma position actuelle
                  </div>
                </div>
              </motion.button>
            )}

            {/* Suggestions */}
            <div className="max-h-64 overflow-y-auto">
              {displaySuggestions.map((suggestion, index) => (
                <motion.button
                  key={suggestion.id}
                  type="button"
                  onClick={() => handleSelectSuggestion(suggestion)}
                  className={cn(
                    "w-full flex items-center gap-3 p-4 text-left transition-colors",
                    "hover:bg-accent focus:bg-accent focus:outline-none"
                  )}
                  whileHover={{ backgroundColor: "rgba(var(--accent), 0.8)" }}
                  whileTap={{ scale: 0.98 }}
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: index * 0.05 }}
                >
                  <div className="w-10 h-10 rounded-full bg-accent/50 flex items-center justify-center">
                    {getSuggestionIcon(suggestion.type)}
                  </div>
                  <div className="flex-1">
                    <div className="font-medium">{suggestion.name}</div>
                    {suggestion.description && (
                      <div className="text-sm text-muted-foreground">
                        {suggestion.description}
                      </div>
                    )}
                  </div>
                </motion.button>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {error && (
        <p className="text-sm text-destructive mt-1">{error}</p>
      )}
    </div>
  )
}

/**
 * Level selector grid (used in create-game details)
 */
interface VLevelSelectorProps {
  selectedLevel: string
  onLevelChange: (level: string, levelRange?: { min: number, max: number }) => void
  maxLevel?: number
  className?: string
  showCompatibilityHint?: boolean
}

export function VLevelSelector({ 
  selectedLevel, 
  onLevelChange, 
  maxLevel = 10, 
  className,
  showCompatibilityHint = true
}: VLevelSelectorProps) {
  const selected = parseInt(selectedLevel)
  
  // Calcul des niveaux compatibles (±1 niveau)
  const getCompatibilityStatus = (level: number) => {
    if (level === selected) return "selected"
    if (Math.abs(level - selected) === 1) return "active" // Niveaux adjacents sont "actifs"
    return "neutral"
  }

  const getButtonVariant = (status: string) => {
    switch (status) {
      case "selected":
        return "bg-primary text-primary-foreground border-primary hover:bg-primary/90"
      case "active":
        return "bg-primary/80 text-primary-foreground border-primary hover:bg-primary/90" // Style plus visible pour les adjacents
      default:
        return "bg-card border-border text-foreground hover:bg-accent hover:text-accent-foreground"
    }
  }

  const formatCompatibilityRange = () => {
    const min = Math.max(1, selected - 1)
    const max = Math.min(maxLevel, selected + 1)
    if (min === max) return `Niveau ${selected}`
    return `Niveaux ${min} à ${max}`
  }

  return (
    <div className={cn("space-y-4", className)}>
      {/* En-tête avec icône */}
      <div className="flex items-center gap-3 mb-4">
        <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
          <div className="w-5 h-5 rounded-full bg-primary text-primary-foreground text-xs font-bold flex items-center justify-center">
            {selected || "?"}
          </div>
        </div>
        <div>
          <h2 className="text-lg font-semibold text-foreground">Niveau</h2>
          <p className="text-sm text-muted-foreground">Sélectionnez votre niveau de jeu</p>
        </div>
      </div>

      {/* Grille des niveaux */}
      <div className={cn("grid grid-cols-5 gap-3")}>
        {Array.from({ length: maxLevel }, (_, i) => {
          const level = i + 1
          const status = getCompatibilityStatus(level)
          
          return (
            <motion.div
              key={level}
              whileTap={{ scale: 0.95 }}
              transition={{ duration: 0.15 }}
            >
              <VButton
                variant="outline"
                onClick={() => {
                  const min = Math.max(1, level - 1)
                  const max = Math.min(maxLevel, level + 1)
                  onLevelChange(String(level), { min, max })
                }}
                className={cn(
                  "h-12 w-full rounded-xl transition-all duration-200 font-semibold",
                  getButtonVariant(status)
                )}
              >
                {level}
              </VButton>
            </motion.div>
          )
        })}
      </div>
      
      {/* Indicateur de compatibilité */}
      {showCompatibilityHint && selectedLevel && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="bg-muted/50 rounded-xl p-3 text-center"
        >
          <p className="text-sm text-muted-foreground">
            Accepte les <span className="font-medium text-foreground">{formatCompatibilityRange()}</span>
          </p>
        </motion.div>
      )}
    </div>
  )
}