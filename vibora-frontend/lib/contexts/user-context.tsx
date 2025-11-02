import { createContext, useContext, useState, useEffect, ReactNode } from "react"
import { viboraApi } from "@/lib/api/vibora-client"
import { getSession } from "@/lib/auth/supabase-auth"
import type { UserProfileDto } from "@/lib/api/vibora-types"

interface UserContextType {
  user: UserProfileDto | null
  isLoading: boolean
  refetch: () => Promise<void>
}

const UserContext = createContext<UserContextType | undefined>(undefined)

export function UserProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfileDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const fetchUser = async () => {
    try {
      const { session } = await getSession()
      if (!session) {
        setUser(null)
        return
      }

      const { data } = await viboraApi.users.getCurrentUserProfile()
      if (data) {
        setUser(data)
      }
    } catch (err) {
      console.error("Error fetching user profile:", err)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    fetchUser()
  }, [])

  return (
    <UserContext.Provider value={{ user, isLoading, refetch: fetchUser }}>
      {children}
    </UserContext.Provider>
  )
}

export function useUser() {
  const context = useContext(UserContext)
  if (!context) {
    throw new Error("useUser must be used within UserProvider")
  }
  return context
}
