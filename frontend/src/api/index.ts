import axios from 'axios'
import client from './client'
import type { AuthResponse, PredictionResponse, AccuracyResponse, StockPricePoint, UserSummary, AdminStats, AlertItem, SyncResult } from '../types'

// ── Auth ──────────────────────────────────────────────────────────────────────

export async function login(email: string, password: string): Promise<AuthResponse> {
  const res = await axios.post<AuthResponse>('/api/auth/login', { email, password })
  return res.data
}

export async function register(email: string, password: string): Promise<AuthResponse> {
  const res = await axios.post<AuthResponse>('/api/auth/register', { email, password })
  return res.data
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await client.put('/auth/change-password', { currentPassword, newPassword })
}

// ── Predictions ───────────────────────────────────────────────────────────────

export async function predict(): Promise<PredictionResponse> {
  const res = await client.post<PredictionResponse>('/predictions')
  return res.data
}

export async function getHistory(count = 30): Promise<PredictionResponse[]> {
  const res = await client.get<PredictionResponse[]>(`/predictions/history?count=${count}`)
  return res.data
}

export async function getAccuracy(predictionId: string): Promise<AccuracyResponse> {
  const res = await client.get<AccuracyResponse>(`/predictions/${predictionId}/accuracy`)
  return res.data
}

// ── BIST 100 ──────────────────────────────────────────────────────────────────

export async function getBist100Prices(days = 60): Promise<StockPricePoint[]> {
  const res = await client.get<StockPricePoint[]>(`/bist100/prices?days=${days}`)
  return res.data
}

// ── Admin ─────────────────────────────────────────────────────────────────────

export async function getAdminStats(): Promise<AdminStats> {
  const res = await client.get<AdminStats>('/admin/stats')
  return res.data
}

export async function getAdminUsers(): Promise<UserSummary[]> {
  const res = await client.get<UserSummary[]>('/admin/users')
  return res.data
}

export async function toggleUserActive(userId: string): Promise<void> {
  await client.put(`/admin/users/${userId}/toggle-active`)
}

export async function setUserRole(userId: string, role: string): Promise<void> {
  await client.put(`/admin/users/${userId}/role`, { role })
}

export async function forceSync(): Promise<SyncResult> {
  const res = await client.post<SyncResult>('/bist100/sync')
  return res.data
}

// ── Alerts ────────────────────────────────────────────────────────────────────

export async function getAlerts(): Promise<AlertItem[]> {
  const res = await client.get<AlertItem[]>('/alerts')
  return res.data
}

export async function createAlert(condition: string, targetPrice: number): Promise<AlertItem> {
  const res = await client.post<AlertItem>('/alerts', { condition, targetPrice })
  return res.data
}

export async function deleteAlert(alertId: string): Promise<void> {
  await client.delete(`/alerts/${alertId}`)
}
