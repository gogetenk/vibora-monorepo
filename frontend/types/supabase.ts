export type Json = string | number | boolean | null | { [key: string]: Json | undefined } | Json[]

export interface Database {
  public: {
    Tables: {
      clubs: {
        Row: {
          id: string
          name: string
          address: string
          city: string
          postal_code: string
          latitude: number | null
          longitude: number | null
          phone: string | null
          email: string | null
          website: string | null
          description: string | null
          created_at: string
          updated_at: string
        }
        Insert: {
          id: string
          name: string
          address: string
          city: string
          postal_code: string
          latitude?: number | null
          longitude?: number | null
          phone?: string | null
          email?: string | null
          website?: string | null
          description?: string | null
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          name?: string
          address?: string
          city?: string
          postal_code?: string
          latitude?: number | null
          longitude?: number | null
          phone?: string | null
          email?: string | null
          website?: string | null
          description?: string | null
          created_at?: string
          updated_at?: string
        }
      }
      courts: {
        Row: {
          id: string
          club_id: string
          name: string
          is_indoor: boolean
          description: string | null
          price_per_hour: number
          created_at: string
          updated_at: string
        }
        Insert: {
          id?: string
          club_id: string
          name: string
          is_indoor?: boolean
          description?: string | null
          price_per_hour: number
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          club_id?: string
          name?: string
          is_indoor?: boolean
          description?: string | null
          price_per_hour?: number
          created_at?: string
          updated_at?: string
        }
      }
      game_participants: {
        Row: {
          id: string
          game_id: string
          user_id: string
          status: string
          created_at: string
          updated_at: string
        }
        Insert: {
          id?: string
          game_id: string
          user_id: string
          status?: string
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          game_id?: string
          user_id?: string
          status?: string
          created_at?: string
          updated_at?: string
        }
      }
      games: {
        Row: {
          id: string
          court_id: string
          creator_id: string | null
          date: string
          start_time: string
          end_time: string
          level: string | null
          is_private: boolean
          players_needed: number
          status: string
          created_at: string
          updated_at: string
        }
        Insert: {
          id?: string
          court_id: string
          creator_id?: string | null
          date: string
          start_time: string
          end_time: string
          level?: string | null
          is_private?: boolean
          players_needed?: number
          status?: string
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          court_id?: string
          creator_id?: string | null
          date?: string
          start_time?: string
          end_time?: string
          level?: string | null
          is_private?: boolean
          players_needed?: number
          status?: string
          created_at?: string
          updated_at?: string
        }
      }
      profiles: {
        Row: {
          id: string
          username: string | null
          full_name: string | null
          avatar_url: string | null
          level: string | null
          phone: string | null
          created_at: string
          updated_at: string
        }
        Insert: {
          id: string
          username?: string | null
          full_name?: string | null
          avatar_url?: string | null
          level?: string | null
          phone?: string | null
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          username?: string | null
          full_name?: string | null
          avatar_url?: string | null
          level?: string | null
          phone?: string | null
          created_at?: string
          updated_at?: string
        }
      }
      tournament_participants: {
        Row: {
          id: string
          tournament_id: string
          user_id: string
          status: string
          created_at: string
          updated_at: string
        }
        Insert: {
          id?: string
          tournament_id: string
          user_id: string
          status?: string
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          tournament_id?: string
          user_id?: string
          status?: string
          created_at?: string
          updated_at?: string
        }
      }
      tournaments: {
        Row: {
          id: string
          club_id: string
          name: string
          type: string | null
          date: string
          start_time: string
          end_time: string
          level: string | null
          max_participants: number
          price: number
          description: string | null
          format: string | null
          status: string
          created_at: string
          updated_at: string
        }
        Insert: {
          id?: string
          club_id: string
          name: string
          type?: string | null
          date: string
          start_time: string
          end_time: string
          level?: string | null
          max_participants: number
          price: number
          description?: string | null
          format?: string | null
          status?: string
          created_at?: string
          updated_at?: string
        }
        Update: {
          id?: string
          club_id?: string
          name?: string
          type?: string | null
          date?: string
          start_time?: string
          end_time?: string
          level?: string | null
          max_participants?: number
          price?: number
          description?: string | null
          format?: string | null
          status?: string
          created_at?: string
          updated_at?: string
        }
      }
    }
  }
}
