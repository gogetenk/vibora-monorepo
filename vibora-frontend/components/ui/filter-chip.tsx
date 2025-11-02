"use client"

import React from "react"
import { motion } from "framer-motion"
import { cn } from "@/lib/utils"

interface FilterChipProps {
  id: string
  label: string
  count?: number
  active?: boolean
  onClick?: (id: string) => void
  className?: string
  variant?: "default" | "compact"
  disabled?: boolean
}

export function FilterChip({
  id,
  label,
  count,
  active = false,
  onClick,
  className,
  variant = "default",
  disabled = false,
}: FilterChipProps) {
  const handleClick = () => {
    if (!disabled && onClick) {
      onClick(id)
    }
  }

  return (
    <motion.button
      whileHover={!disabled ? { scale: 1.02 } : {}}
      whileTap={!disabled ? { scale: 0.98 } : {}}
      transition={{ type: "spring", stiffness: 400, damping: 25 }}
      onClick={handleClick}
      disabled={disabled}
      className={cn(
        "shrink-0 flex items-center gap-2 text-xs font-medium transition-all duration-200 border-0 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed",
        // Size variants
        variant === "default" && "px-4 py-2.5 rounded-full",
        variant === "compact" && "px-3 py-2 rounded-lg",
        // State variants
        active
          ? "bg-success text-success-foreground shadow-md shadow-success/25"
          : "bg-secondary/60 text-muted-foreground hover:bg-secondary/80 hover:text-foreground",
        className
      )}
    >
      <span>{label}</span>
      {count !== undefined && (
        <span
          className={cn(
            "px-1.5 py-0.5 rounded-full text-xs font-semibold",
            active
              ? "bg-primary-foreground/40 text-success-foreground"
              : "bg-muted text-muted-foreground"
          )}
        >
          {count}
        </span>
      )}
    </motion.button>
  )
}

interface FilterChipGroupProps {
  filters: Array<{
    id: string
    label: string
    count?: number
    active?: boolean
  }>
  onFilterToggle: (filterId: string) => void
  className?: string
  variant?: "default" | "compact"
  multipleSelection?: boolean
}

export function FilterChipGroup({
  filters,
  onFilterToggle,
  className,
  variant = "default",
  multipleSelection = true,
}: FilterChipGroupProps) {
  return (
    <div className={cn("flex gap-3 pb-2 overflow-x-auto hide-scrollbar", className)}>
      {filters.map((filter) => (
        <FilterChip
          key={filter.id}
          id={filter.id}
          label={filter.label}
          count={filter.count}
          active={filter.active}
          onClick={onFilterToggle}
          variant={variant}
        />
      ))}
    </div>
  )
}

// Specialized variants for common use cases
export function QuickFilters({
  filters,
  onFilterToggle,
  className,
}: {
  filters: FilterChipGroupProps["filters"]
  onFilterToggle: FilterChipGroupProps["onFilterToggle"]
  className?: string
}) {
  return (
    <div className={cn("-mx-6", className)}>
      <div className="px-6">
        <FilterChipGroup
          filters={filters}
          onFilterToggle={onFilterToggle}
          variant="default"
          multipleSelection={true}
        />
      </div>
    </div>
  )
}

export function CompactFilters({
  filters,
  onFilterToggle,
  className,
}: {
  filters: FilterChipGroupProps["filters"]
  onFilterToggle: FilterChipGroupProps["onFilterToggle"]
  className?: string
}) {
  return (
    <FilterChipGroup
      filters={filters}
      onFilterToggle={onFilterToggle}
      variant="compact"
      multipleSelection={true}
      className={className}
    />
  )
}