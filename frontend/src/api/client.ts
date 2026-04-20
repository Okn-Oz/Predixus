import axios from 'axios'
import { useAuthStore } from '../store/useAuthStore'

const client = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Her isteğe Bearer token ekle
client.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let isRefreshing = false
let failedQueue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = []

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((p) => {
    if (error) p.reject(error)
    else if (token) p.resolve(token)
  })
  failedQueue = []
}

// 401 → refresh token dene, olmadıysa logout
client.interceptors.response.use(
  (res) => res,
  async (error: unknown) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)

    const originalRequest = error.config as typeof error.config & { _retry?: boolean }
    if (error.response?.status !== 401 || originalRequest?._retry) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject })
      }).then((token) => {
        if (originalRequest) {
          originalRequest.headers = originalRequest.headers ?? {}
          originalRequest.headers['Authorization'] = `Bearer ${token}`
          return client(originalRequest)
        }
      })
    }

    originalRequest!._retry = true
    isRefreshing = true

    const refreshToken = useAuthStore.getState().refreshToken
    try {
      const res = await axios.post('/api/auth/refresh', { refreshToken })
      const { accessToken, refreshToken: newRefresh, expiresAt, email } = res.data as {
        accessToken: string
        refreshToken: string
        expiresAt: string
        email: string
      }
      useAuthStore.getState().login({ accessToken, refreshToken: newRefresh, expiresAt, email })
      processQueue(null, accessToken)
      if (originalRequest) {
        originalRequest.headers = originalRequest.headers ?? {}
        originalRequest.headers['Authorization'] = `Bearer ${accessToken}`
        return client(originalRequest)
      }
    } catch (err) {
      processQueue(err, null)
      useAuthStore.getState().logout()
      return Promise.reject(err)
    } finally {
      isRefreshing = false
    }
  }
)

export default client
