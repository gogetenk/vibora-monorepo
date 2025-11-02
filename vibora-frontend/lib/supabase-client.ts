import { createClientComponentClient } from "@supabase/auth-helpers-nextjs"
import type { Database } from "@/types/supabase"

/**
 * Get Supabase client for browser/client components
 * Creates a new instance each time to ensure PKCE flow works correctly
 */
export const getSupabaseClient = () => {
  return createClientComponentClient<Database>()
}
