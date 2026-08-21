import { clearStoredSession, getStoredToken } from "@/lib/auth-storage"

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001'

export const AUTH_EXPIRED_EVENT = 'pos:auth-expired'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getStoredToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...((options?.headers as Record<string, string>) ?? {}),
  }
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  const bodyText = await res.text()

  if (!res.ok) {
    let message = bodyText || `HTTP ${res.status}`
    try {
      const parsed = JSON.parse(bodyText) as { message?: string; error?: string }
      message = parsed.message ?? parsed.error ?? message
    } catch {
      // keep raw text as message
    }
    if (res.status === 401 && typeof window !== 'undefined') {
      clearStoredSession()
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT))
    }
    throw new ApiError(res.status, message)
  }

  if (!bodyText) return null as T
  try {
    return JSON.parse(bodyText) as T
  } catch {
    throw new ApiError(res.status, `Unexpected non-JSON response (HTTP ${res.status})`)
  }
}

export interface ProductBarcode {
  id: string
  productUnitId: string
  barcode: string
  isDefault: boolean
  createdAt: string
}

export interface ProductUnit {
  id: string
  productId: string
  unitName: string
  quantityFactor: number
  retailPrice: number
  wholesalePrice: number | null
  isBaseUnit: boolean
  createdAt: string
  barcodes?: ProductBarcode[]
}

export interface Product {
  id: string
  barcode: string | null
  barcodes?: ProductBarcode[]
  units?: ProductUnit[]
  name: string
  buyPrice: number
  salePrice: number
  stockQuantity: number
  notes: string | null
  allowDiscount: boolean
  lowStockThreshold: number
  createdAt: string
  updatedAt: string
}

export type PriceMode = 'retail' | 'wholesale'

export interface Invoice {
  id: string
  invoiceNumber: number
  totalAmount: number
  discount: number
  discountType: string | null
  discountValue: number
  discountAmount: number
  priceMode?: PriceMode | null
  createdAt: string
  invoiceDetail?: InvoiceDetail[]
  InvoiceDetail?: InvoiceDetail[]
}

export interface InvoiceDetail {
  id: string
  invoiceId: string
  productId: string | null
  productUnitId?: string | null
  unitName?: string | null
  quantity: number
  buyPrice: number
  salePrice: number
  originalUnitPrice?: number
  unitPrice?: number
  discountType?: string | null
  discountValue?: number
  discountAmount: number
  lineSubtotal?: number
  finalTotal?: number
  priceEditNote?: string | null
  product: Product | null
}

export interface PagedProducts {
  items: Product[]
  total: number
  page: number
  pageSize: number
}

export interface PagedInvoices {
  items: Invoice[]
  total: number
  page: number
  pageSize: number
  totals: { revenue: number; discounts: number }
}

export interface LicenseStatus {
  status: 'ok' | 'first_boot' | 'locked' | 'tampered'
  machineId?: string
  daysSinceActivation?: number
}

export interface AccessBundle {
  user: {
    id: string
    name: string
    isActive: boolean
    tenantId: string
  }
  roles: string[]
  permissions: string[]
  features: string[]
}

export interface LoginResponse {
  token: string
  access: AccessBundle
}

export const api = {
  // Auth
  auth: {
    login: (username: string, password: string) =>
      request<LoginResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ username, password }) }),
    me: () => request<AccessBundle>('/api/auth/me'),
  },

  // Products
  products: {
    list: () => request<Product[]>('/api/products'),
    search: (q: string, limit = 20, signal?: AbortSignal) => {
      const params = new URLSearchParams()
      params.set('q', q)
      if (limit) params.set('limit', String(limit))
      return request<Product[]>(`/api/products/search?${params}`, { signal })
    },
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedProducts>(`/api/products/paged?${params}`)
    },
    count: () => request<number>('/api/products/count'),
    get: (id: string) => request<Product>(`/api/products/${id}`),
    create: (data: Partial<Product>) =>
      request<Product>('/api/products', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: Partial<Product>) =>
      request<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    delete: (id: string) =>
      request<{ success: boolean }>(`/api/products/${id}`, { method: 'DELETE' }),
    units: {
      add: (productId: string, data: { unitName: string; quantityFactor: number; retailPrice: number; wholesalePrice?: number | null }) =>
        request<ProductUnit>(`/api/products/${productId}/units`, {
          method: 'POST',
          body: JSON.stringify(data),
        }),
      update: (productId: string, unitId: string, data: { unitName?: string; quantityFactor?: number; retailPrice?: number; wholesalePrice?: number | null }) =>
        request<ProductUnit>(`/api/products/${productId}/units/${unitId}`, {
          method: 'PUT',
          body: JSON.stringify(data),
        }),
      remove: (productId: string, unitId: string) =>
        request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}`, {
          method: 'DELETE',
        }),
      barcodes: {
        add: (productId: string, unitId: string, barcode: string) =>
          request<ProductBarcode>(`/api/products/${productId}/units/${unitId}/barcodes`, {
            method: 'POST',
            body: JSON.stringify({ barcode }),
          }),
        remove: (productId: string, unitId: string, barcodeId: string) =>
          request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}/barcodes/${barcodeId}`, {
            method: 'DELETE',
          }),
        setDefault: (productId: string, unitId: string, barcodeId: string) =>
          request<{ success: boolean }>(`/api/products/${productId}/units/${unitId}/barcodes/${barcodeId}/default`, {
            method: 'PUT',
          }),
      },
    },
  },

  // Invoices
  invoices: {
    list: () => request<Invoice[]>('/api/invoices'),
    listPaged: (page = 1, pageSize = 20, opts?: { from?: string; to?: string; q?: string }) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (opts?.from) params.set('from', opts.from)
      if (opts?.to) params.set('to', opts.to)
      if (opts?.q) params.set('q', opts.q)
      return request<PagedInvoices>(`/api/invoices/paged?${params}`)
    },
    get: (id: string) => request<Invoice>(`/api/invoices/${id}`),
    filter: (from?: string, to?: string) => {
      const params = new URLSearchParams()
      if (from) params.set('from', from)
      if (to) params.set('to', to)
      const qs = params.toString()
      return request<Invoice[]>(`/api/invoices/filter${qs ? '?' + qs : ''}`)
    },
    create: (data: InvoiceCreatePayload) =>
      request<Invoice>('/api/invoices', { method: 'POST', body: JSON.stringify(data) }),
  },

  // Reports
  reports: {
    lowStock: () => request<Product[]>('/api/reports/low-stock'),
  },

  // License
  license: {
    check: () => request<LicenseStatus>('/api/license'),
    unlock: (machineId: string, code: string) =>
      request<{ success: boolean; machineId: string }>('/api/license/unlock', {
        method: 'POST',
        body: JSON.stringify({ machineId, code }),
      }),
  },

  // Printing
  printing: {
    print: (invoice: unknown) =>
      request<{ success: boolean; message: string }>('/api/printing/print', {
        method: 'POST',
        body: JSON.stringify({ invoice }),
      }),
    printBarcode: (data: { barcode: string; name?: string; price?: number; count?: number }) =>
      request<{ success: boolean; message: string }>('/api/printing/print-barcode', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
  },

  // Administration: Roles & Permissions
  roles: {
    list: () => request<RoleSummary[]>('/api/roles'),
    get: (id: string) => request<RoleSummary>(`/api/roles/${id}`),
    create: (data: { name: string; description?: string | null }) =>
      request<RoleSummary>('/api/roles', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name: string; description?: string | null }) =>
      request<RoleSummary>(`/api/roles/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: string) =>
      request<{ success: boolean }>(`/api/roles/${id}`, { method: 'DELETE' }),
    getPermissions: (id: string) =>
      request<{ roleId: string; permissionIds: string[] }>(`/api/roles/${id}/permissions`),
    setPermissions: (id: string, permissionIds: string[]) =>
      request<{ success: boolean; permissionIds: string[] }>(`/api/roles/${id}/permissions`, {
        method: 'PUT',
        body: JSON.stringify({ permissionIds }),
      }),
  },
  permissions: {
    list: () => request<PermissionInfo[]>('/api/permissions'),
  },

  // Administration: Users
  users: {
    list: () => request<UserSummary[]>('/api/users'),
    create: (data: { name: string; username: string; password: string; roleIds: string[] }) =>
      request<UserSummary>('/api/users', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; username?: string; password?: string; isActive?: boolean; roleIds?: string[] }) =>
      request<UserSummary>(`/api/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  },

  // Administration: Tenant Features
  tenant: {
    features: () => request<{ features: TenantFeature[] }>('/api/tenant/features'),
    setFeatures: (features: TenantFeature[]) =>
      request<{ success: boolean }>('/api/tenant/features', {
        method: 'PUT',
        body: JSON.stringify({ features }),
      }),
  },
}

export interface RoleSummary {
  id: string
  name: string
  description: string | null
  isSystem: boolean
  userCount: number
  permissionCount: number
}

export interface PermissionInfo {
  id: string
  key: string
  name: string
  description: string | null
  resource: string
  action: string
}

export interface UserSummary {
  id: string
  name: string
  username: string
  isActive: boolean
  roleIds: string[]
  createdAt: string
}

export interface TenantFeature {
  key: string
  enabled: boolean
}

export interface InvoiceItemPayload {
  productId: string
  productUnitId: string
  unitName: string
  name: string
  buyPrice: number
  salePrice: number
  originalUnitPrice: number
  unitPrice: number
  quantity: number
  maxStock: number
  allowDiscount: boolean
  discountType?: string | null
  discountValue?: number
  quantityFactor: number
  priceEditNote?: string | null
}

export interface InvoiceCreatePayload {
  items: InvoiceItemPayload[]
  discount: number
  discountType?: string | null
  discountValue?: number
  priceMode: PriceMode
}
