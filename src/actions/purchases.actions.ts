import { request } from "@/lib/api"
import { PurchaseInvoice, SavePurchaseRequest } from "@/types/domain/domain.types"

export function createPurchase(data: SavePurchaseRequest) {
  return request<PurchaseInvoice>('/api/purchases', { method: 'POST', body: JSON.stringify(data) })
}

export function updatePurchase(id: string, data: SavePurchaseRequest) {
  return request<PurchaseInvoice>(`/api/purchases/${id}`, { method: 'PUT', body: JSON.stringify(data) })
}

export function postPurchase(id: string) {
  return request<PurchaseInvoice>(`/api/purchases/${id}/post`, { method: 'POST' })
}

export function cancelPurchase(id: string) {
  return request<PurchaseInvoice>(`/api/purchases/${id}/cancel`, { method: 'POST' })
}
