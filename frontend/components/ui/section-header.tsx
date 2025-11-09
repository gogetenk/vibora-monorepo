"use client"

import React from "react"
import Link from "next/link"
import { motion } from "framer-motion"
import { cn } from "@/lib/utils"

interface SectionHeaderProps {
  title: string
  subtitle?: string
  actionLabel?: string
  actionHref?: string
  onActionClick?: () => void
  className?: string
  titleClassName?: string
  actionClassName?: string
  variant?: "default" | "large" | "compact"
  align?: "left" | "center"
}

export function SectionHeader({
  title,
  subtitle,
  actionLabel,
  actionHref,
  onActionClick,
  className,
  titleClassName,
  actionClassName,
  variant = "default",
  align = "left",
}: SectionHeaderProps) {
  const renderAction = () => {
    if (!actionLabel) return null

    const actionContent = (
      <motion.span
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        transition={{ type: "spring", stiffness: 400, damping: 25 }}
        className={cn(
          "text-sm font-medium text-primary hover:text-primary/80 transition-colors cursor-pointer",
          actionClassName
        )}
      >
        {actionLabel}
      </motion.span>
    )

    if (actionHref) {
      return <Link href={actionHref}>{actionContent}</Link>
    }

    if (onActionClick) {
      return (
        <button onClick={onActionClick} className="focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 rounded">
          {actionContent}
        </button>
      )
    }

    return actionContent
  }

  return (
    <div
      className={cn(
        "flex items-center justify-between mb-4",
        align === "center" && "text-center flex-col gap-2",
        className
      )}
    >
      <div className={cn(align === "center" && "text-center")}>
        <h2
          className={cn(
            "font-semibold",
            variant === "large" && "text-xl font-bold",
            variant === "compact" && "text-base",
            variant === "default" && "text-lg",
            titleClassName
          )}
        >
          {title}
        </h2>
        {subtitle && (
          <p
            className={cn(
              "text-muted-foreground mt-1",
              variant === "large" && "text-base",
              variant === "compact" && "text-xs",
              variant === "default" && "text-sm"
            )}
          >
            {subtitle}
          </p>
        )}
      </div>
      {renderAction()}
    </div>
  )
}

// Specialized variants for common use cases
export function PageHeader({
  title,
  subtitle,
  actionLabel,
  actionHref,
  onActionClick,
  className,
}: Omit<SectionHeaderProps, "variant">) {
  return (
    <SectionHeader
      title={title}
      subtitle={subtitle}
      actionLabel={actionLabel}
      actionHref={actionHref}
      onActionClick={onActionClick}
      variant="large"
      className={cn("mb-6", className)}
    />
  )
}

export function CompactSectionHeader({
  title,
  actionLabel,
  actionHref,
  onActionClick,
  className,
}: Omit<SectionHeaderProps, "variant" | "subtitle">) {
  return (
    <SectionHeader
      title={title}
      actionLabel={actionLabel}
      actionHref={actionHref}
      onActionClick={onActionClick}
      variant="compact"
      className={cn("mb-3", className)}
    />
  )
}

export function CenterSectionHeader({
  title,
  subtitle,
  className,
}: Omit<SectionHeaderProps, "variant" | "align" | "actionLabel" | "actionHref" | "onActionClick">) {
  return (
    <SectionHeader
      title={title}
      subtitle={subtitle}
      variant="large"
      align="center"
      className={cn("mb-8", className)}
    />
  )
}