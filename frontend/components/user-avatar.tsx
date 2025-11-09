import { useEffect, useState } from "react"
import type { UserProfileDto } from "@/lib/api/vibora-types"

interface UserAvatarProps {
  user: UserProfileDto | null
  size?: "sm" | "md" | "lg"
  className?: string
}

export function UserAvatar({ user, size = "md", className = "" }: UserAvatarProps) {
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!user) return

    // Charger la photo depuis localStorage (fallback) ou depuis l'API
    if (user.profilePhotoUrl) {
      setPhotoUrl(`${process.env.NEXT_PUBLIC_VIBORA_API_URL}${user.profilePhotoUrl}`)
    } else {
      try {
        const savedPhoto = localStorage.getItem(`profile_photo_${user.externalId}`)
        if (savedPhoto) {
          setPhotoUrl(savedPhoto)
        }
      } catch (e) {
        console.warn("Could not load photo from localStorage:", e)
      }
    }
  }, [user])

  const sizeClasses = {
    sm: "w-8 h-8 text-xs",
    md: "w-10 h-10 text-sm",
    lg: "w-24 h-24 text-2xl",
  }

  const selectedSize = sizeClasses[size]

  if (photoUrl) {
    return (
      <img
        src={photoUrl}
        alt={user?.displayName || "User"}
        className={`${selectedSize} rounded-full object-cover border-2 border-border ${className}`}
      />
    )
  }

  // Fallback: initiales
  const initials = user
    ? `${(user.firstName?.[0] || "").toUpperCase()}${(user.lastName?.[0] || "").toUpperCase()}`
    : "?"

  return (
    <div
      className={`${selectedSize} rounded-full bg-success/20 border-2 border-border flex items-center justify-center font-bold text-success ${className}`}
    >
      {initials}
    </div>
  )
}
