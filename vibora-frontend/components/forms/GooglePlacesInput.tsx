"use client"

import { useEffect, useRef, useState, useCallback } from "react"
import { MapPin, Loader2 } from "lucide-react"
import { motion, AnimatePresence } from "framer-motion"

interface GooglePlacesInputProps {
  value: string
  onChange: (value: string, coordinates?: { lat: number; lng: number }) => void
  placeholder?: string
  className?: string
}

// Using any for Google Maps API types which are dynamic
type PlaceSuggestion = any

export function GooglePlacesInput({
  value,
  onChange,
  placeholder = "Rechercher un club...",
  className
}: GooglePlacesInputProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const [isLoaded, setIsLoaded] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [suggestions, setSuggestions] = useState<any[]>([])
  const [isOpen, setIsOpen] = useState(false)
  const [isSearching, setIsSearching] = useState(false)
  const sessionTokenRef = useRef<any>(null)

  // Load Google Maps API with new Places library using bootstrap loader
  useEffect(() => {
    const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY

    if (!apiKey) {
      setError("Google Maps API key manquante. Ajoutez NEXT_PUBLIC_GOOGLE_MAPS_API_KEY dans .env.local")
      return
    }

    // Check if already loaded
    if (typeof window !== "undefined" && window.google?.maps?.importLibrary !== undefined) {
      initializePlacesLibrary()
      return
    }

    // Load the Maps JavaScript API bootstrap loader
    if (typeof window !== "undefined") {
      const script = document.createElement("script")
      script.innerHTML = `
        (g=>{var h,a,k,p="The Google Maps JavaScript API",c="google",l="importLibrary",q="__ib__",m=document,b=window;b=b[c]||(b[c]={});var d=b.maps||(b.maps={}),r=new Set,e=new URLSearchParams,u=()=>h||(h=new Promise(async(f,n)=>{await (a=m.createElement("script"));e.set("libraries",[...r]+"");for(k in g)e.set(k.replace(/[A-Z]/g,t=>"_"+t[0].toLowerCase()),g[k]);e.set("callback",c+".maps."+q);a.src=\`https://maps.\${c}apis.com/maps/api/js?\`+e;d[q]=f;a.onerror=()=>h=n(Error(p+" could not load."));a.nonce=m.querySelector("script[nonce]")?.nonce||"";m.head.append(a)}));d[l]?console.warn(p+" only loads once. Ignoring:",g):d[l]=(f,...n)=>r.add(f)&&u().then(()=>d[l](f,...n))})({ 
          key: "${apiKey}", 
          v: "weekly"
        });
      `
      document.head.appendChild(script)

      // Wait a bit for the bootstrap to load, then initialize
      setTimeout(() => {
        initializePlacesLibrary()
      }, 500)
    }

    async function initializePlacesLibrary() {
      try {
        if (!window.google?.maps?.importLibrary) {
          setTimeout(() => initializePlacesLibrary(), 200)
          return
        }

        // Import the places library using the new API
        await window.google.maps.importLibrary("places")
        
        // Initialize session token
        if (window.google?.maps?.places?.AutocompleteSessionToken) {
          sessionTokenRef.current = new google.maps.places.AutocompleteSessionToken()
          setIsLoaded(true)
        } else {
          setError("Places library non disponible")
        }
      } catch (err) {
        console.error("Error loading Places library:", err)
        setError("Erreur chargement Google Places API")
      }
    }
  }, [])

  // Fetch autocomplete suggestions using the new API
  const fetchSuggestions = useCallback(async (input: string) => {
    if (!input.trim() || !isLoaded || !window.google?.maps?.places?.AutocompleteSuggestion) {
      setSuggestions([])
      return
    }

    setIsSearching(true)
    try {
      const request = {
        input: input,
        sessionToken: sessionTokenRef.current,
        // No type filter = allows cities, clubs, establishments, addresses, etc.
        includedRegionCodes: ["FR"] // Limit to France
      }

      const { suggestions } = await google.maps.places.AutocompleteSuggestion.fetchAutocompleteSuggestions(request)
      
      // Filter out suggestions with null placePrediction
      const validSuggestions = (suggestions || []).filter((s: any) => s.placePrediction !== null)
      setSuggestions(validSuggestions)
      setIsOpen(validSuggestions.length > 0)
    } catch (error) {
      console.error("Error fetching suggestions:", error)
      setSuggestions([])
    } finally {
      setIsSearching(false)
    }
  }, [isLoaded])

  // Debounced input handler
  useEffect(() => {
    const timer = setTimeout(() => {
      if (value.length > 2) {
        fetchSuggestions(value)
      } else {
        setSuggestions([])
        setIsOpen(false)
      }
    }, 300)

    return () => clearTimeout(timer)
  }, [value, fetchSuggestions])

  // Handle suggestion selection
  const handleSelectSuggestion = async (suggestion: any) => {
    if (!suggestion?.placePrediction) return
    
    try {
      const place = suggestion.placePrediction.toPlace()
      
      // Fetch place details with the new API
      await place.fetchFields({
        fields: ["displayName", "location", "formattedAddress"]
      })

      const coordinates = place.location ? {
        lat: place.location.lat(),
        lng: place.location.lng()
      } : undefined

      onChange(place.displayName || suggestion.placePrediction.text?.text || "", coordinates)
      setIsOpen(false)
      setSuggestions([])
      
      // Create a new session token for the next search
      sessionTokenRef.current = new google.maps.places.AutocompleteSessionToken()
    } catch (error) {
      console.error("Error fetching place details:", error)
      onChange(suggestion.placePrediction.text?.text || "")
      setIsOpen(false)
    }
  }

  // Close dropdown on click outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        inputRef.current &&
        !inputRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  return (
    <div className="relative">
      <div className="relative">
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => {
            if (suggestions.length > 0) setIsOpen(true)
          }}
          placeholder={placeholder}
          className={
            className ||
            "w-full px-4 py-3 rounded-xl border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
          }
          disabled={!!error}
          autoComplete="off"
        />
        {isSearching ? (
          <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground animate-spin" />
        ) : (
          <MapPin className="absolute right-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground pointer-events-none" />
        )}
      </div>

      {/* Suggestions dropdown */}
      <AnimatePresence>
        {isOpen && suggestions.length > 0 && (
          <motion.div
            ref={dropdownRef}
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.2 }}
            className="absolute z-50 w-full mt-2 bg-card border border-border rounded-xl shadow-lg max-h-60 overflow-y-auto"
          >
            {suggestions.map((suggestion, index) => {
              if (!suggestion.placePrediction) return null
              
              const prediction = suggestion.placePrediction
              const mainText = prediction.structuredFormat?.mainText?.text || prediction.text?.text || "Lieu"
              const secondaryText = prediction.structuredFormat?.secondaryText?.text || ""
              
              return (
                <button
                  key={prediction.placeId}
                  type="button"
                  onClick={() => handleSelectSuggestion(suggestion)}
                  className="w-full px-4 py-3 text-left hover:bg-muted transition-colors flex items-start gap-3 border-b border-border last:border-0"
                >
                  <MapPin className="h-4 w-4 text-primary mt-1 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-foreground text-sm">
                      {mainText}
                    </div>
                    {secondaryText && (
                      <div className="text-xs text-muted-foreground truncate">
                        {secondaryText}
                      </div>
                    )}
                  </div>
                </button>
              )
            })}
          </motion.div>
        )}
      </AnimatePresence>

      {error && (
        <p className="text-xs text-destructive mt-1">
          {error}
        </p>
      )}

      {!isLoaded && !error && (
        <p className="text-xs text-muted-foreground mt-1">
          Chargement Google Places...
        </p>
      )}
    </div>
  )
}
