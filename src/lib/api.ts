const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  })
  if (!res.ok) {
    const body = await res.text()
    throw new Error(body || `HTTP ${res.status}`)
  }
  return res.json()
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

export const api = {
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
    unlock: (machineId: string) =>
      request<{ success: boolean; machineId: string }>('/api/license/unlock', {
        method: 'POST',
        body: JSON.stringify({ machineId }),
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
