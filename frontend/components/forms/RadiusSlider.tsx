"use client"

import { Slider } from "@/components/ui/slider"

interface RadiusSliderProps {
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  className?: string
}

export function RadiusSlider({
  value,
  onChange,
  min = 5,
  max = 50,
  className
}: RadiusSliderProps) {
  return (
    <div className={`space-y-2 ${className || ""}`}>
      <div className="flex items-center justify-between">
        <label className="text-sm font-medium text-foreground">
          Rayon de recherche
        </label>
        <span className="text-sm font-semibold text-primary">
          {value} km
        </span>
      </div>

      <Slider
        value={[value]}
        onValueChange={([newValue]) => onChange(newValue)}
        min={min}
        max={max}
        step={5}
        className="w-full"
      />

      <div className="flex justify-between text-xs text-muted-foreground">
        <span>{min} km</span>
        <span>{max} km</span>
      </div>
    </div>
  )
}
