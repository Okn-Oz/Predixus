import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthResponse } from '../types'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  email: string | null
  role: string | null
  isAuthenticated: boolean
  login: (response: AuthResponse) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      email: null,
      role: null,
      isAuthenticated: false,

      login: (response) =>
        set({
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          email: response.email,
          role: response.role,
          isAuthenticated: true,
        }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          email: null,
          role: null,
          isAuthenticated: false,
        }),
    }),
    {
      name: 'predixus-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        email: state.email,
        role: state.role,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
