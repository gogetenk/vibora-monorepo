import { cn } from "@/lib/utils"
import { ReactNode } from "react"

interface ContainerProps {
  children: ReactNode
  className?: string
  fullWidth?: boolean
}

export function Container({ children, className, fullWidth = false }: ContainerProps) {
  return (
    <div
      className={cn(
        "w-full",
        fullWidth ? "px-0" : "px-6", // 24px margin (px-6)
        className
      )}
    >
      {children}
    </div>
  )
}
