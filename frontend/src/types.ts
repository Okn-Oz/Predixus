export interface PredictionResponse {
  predictionId: string
  predictedPrice: number
  predictedAt: string
  fromCache: boolean
}

export interface AccuracyResponse {
  predictionId: string
  totalPoints: number
  actualizedPoints: number
  meanAbsoluteError: number | null
  meanAbsolutePercentageError: number | null
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  email: string
  role: string
}

export interface UserSummary {
  id: string
  email: string
  role: string
  isActive: boolean
  createdAt: string
  predictionCount: number
}

export interface AdminStats {
  totalUsers: number
  activeUsers: number
  totalPredictions: number
}

export interface StockPricePoint {
  date: string          // "2024-04-15" — DateOnly from backend
  open: number
  high: number
  low: number
  close: number
  volume: number
  dailyChangePercent: number
}

export interface AlertItem {
  id: string
  condition: string     // "ABOVE" | "BELOW"
  targetPrice: number
  isActive: boolean
  isTriggered: boolean
  createdAt: string
}

export interface SyncResult {
  message: string
  newRecords: number
}
