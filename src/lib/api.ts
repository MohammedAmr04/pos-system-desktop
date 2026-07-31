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
  productId: string
  barcode: string
  isDefault: boolean
  createdAt: string
}

export interface Product {
  id: string
  barcode: string | null
  barcodes?: ProductBarcode[]
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

export interface Invoice {
  id: string
  totalAmount: number
  discount: number
  discountType: string | null
  discountValue: number
  discountAmount: number
  createdAt: string
  invoiceDetail?: InvoiceDetail[]
  InvoiceDetail?: InvoiceDetail[]
}

export interface InvoiceDetail {
  id: string
  invoiceId: string
  productId: string | null
  quantity: number
  buyPrice: number
  salePrice: number
  discountAmount: number
  product: Product | null
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
    get: (id: string) => request<Product>(`/api/products/${id}`),
    create: (data: Partial<Product>) =>
      request<Product>('/api/products', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: Partial<Product>) =>
      request<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    delete: (id: string) =>
      request<{ success: boolean }>(`/api/products/${id}`, { method: 'DELETE' }),
    barcodes: {
      add: (productId: string, barcode: string) =>
        request<ProductBarcode>(`/api/products/${productId}/barcodes`, {
          method: 'POST',
          body: JSON.stringify({ barcode }),
        }),
      remove: (productId: string, barcodeId: string) =>
        request<{ success: boolean }>(`/api/products/${productId}/barcodes/${barcodeId}`, {
          method: 'DELETE',
        }),
      setDefault: (productId: string, barcodeId: string) =>
        request<{ success: boolean }>(`/api/products/${productId}/barcodes/${barcodeId}/default`, {
          method: 'PUT',
        }),
    },
  },

  // Invoices
  invoices: {
    list: () => request<Invoice[]>('/api/invoices'),
    filter: (from?: string, to?: string) => {
      const params = new URLSearchParams()
      if (from) params.set('from', from)
      if (to) params.set('to', to)
      const qs = params.toString()
      return request<Invoice[]>(`/api/invoices/filter${qs ? '?' + qs : ''}`)
    },
    create: (data: { items: { productId: string; name: string; buyPrice: number; salePrice: number; quantity: number; maxStock: number; allowDiscount: boolean }[]; discount: number; discountType?: string; discountValue?: number }) =>
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
