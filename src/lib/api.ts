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
  unitId?: string | null
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
  categoryId?: string | null
  brandId?: string | null
  createdAt: string
  updatedAt: string
}

export interface ProductWriteRequest {
  name: string
  barcode?: string | null
  buyPrice: number
  retailPrice: number
  wholesalePrice?: number | null
  stockQuantity: number
  unitName?: string | null
  unitId?: string | null
  categoryId?: string | null
  brandId?: string | null
  notes?: string | null
  allowDiscount?: boolean
  lowStockThreshold?: number
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
  status?: 'draft' | 'posted' | 'cancelled'
  paymentMethod?: string
  clientId?: string | null
  createdBy?: string | null
  client?: Client | null
  returnStatus?: string | null
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
  totalCost?: number | null
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

export interface PagedSaleReturns {
  items: SaleReturn[]
  total: number
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

export interface Category {
  id: string
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Brand {
  id: string
  name: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface MasterUnit {
  id: string
  name: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PagedMasterData<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface Supplier {
  id: string
  name: string
  phone: string | null
  address: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Client {
  id: string
  name: string
  phone: string | null
  address: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PartyStatement {
  partyId: string
  balance: number
  entries: PartyStatementEntry[]
}

export interface PartyStatementEntry {
  date: string
  description: string
  debit: number
  credit: number
}

export interface PurchaseLineInput {
  productId: string
  productUnitId: string
  quantity: number
  unitCost: number
  newRetailPrice?: number | null
  newWholesalePrice?: number | null
}

export interface SavePurchaseRequest {
  supplierId?: string | null
  supplierInvoiceNumber?: string | null
  date?: string | null
  paymentMethod?: 'cash' | 'credit'
  status?: 'draft' | 'posted'
  discount?: number
  tax?: number
  notes?: string | null
  lines: PurchaseLineInput[]
}

export interface Payment {
  id: string
  amount: number
  paymentMethod: string
  date: string
  invoiceId: string | null
  clientId: string | null
  supplierId: string | null
  reference: string | null
  notes: string | null
  createdBy: string | null
  createdAt: string
  client?: Client | Supplier | null
  supplier?: Client | Supplier | null
  invoiceNumber?: string | null
}

export interface PagedPayments {
  items: Payment[]
  total: number
  page: number
  pageSize: number
}

export interface CreatePaymentRequest {
  amount: number
  paymentMethod: 'cash' | 'card' | 'bank_transfer'
  date?: string | null
  invoiceId?: string | null
  clientId?: string | null
  supplierId?: string | null
  reference?: string | null
  notes?: string | null
}

export interface PurchaseItem {
  id: string
  purchaseInvoiceId: string
  productId: string
  product?: Pick<Product, 'id' | 'name'> | null
  productUnitId: string
  unitName: string
  quantityFactor: number
  quantity: number
  unitCost: number
  lineTotal: number
  newRetailPrice: number | null
  newWholesalePrice: number | null
}

export interface PurchaseInvoice {
  id: string
  invoiceNumber: number
  supplierInvoiceNumber: string | null
  supplierId: string | null
  supplier?: Pick<Supplier, 'id' | 'name'> | null
  date: string
  paymentMethod: string
  status: 'draft' | 'posted' | 'cancelled'
  subtotal: number
  discount: number
  tax: number
  total: number
  notes: string | null
  createdBy: string | null
  createdAt: string
  updatedAt: string
  items?: PurchaseItem[]
  returnStatus?: 'partial' | 'full' | null
}

export interface PagedPurchases {
  items: PurchaseInvoice[]
  total: number
  postedTotal: number
  page: number
  pageSize: number
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
    create: (data: ProductWriteRequest) =>
      request<Product>('/api/products', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: ProductWriteRequest) =>
      request<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    delete: (id: string) =>
      request<{ success: boolean }>(`/api/products/${id}`, { method: 'DELETE' }),
    units: {
      add: (productId: string, data: { unitId?: string | null; unitName: string; quantityFactor: number; retailPrice: number; wholesalePrice?: number | null }) =>
        request<ProductUnit>(`/api/products/${productId}/units`, {
          method: 'POST',
          body: JSON.stringify(data),
        }),
      update: (productId: string, unitId: string, data: { unitId?: string | null; unitName?: string; quantityFactor?: number; retailPrice?: number; wholesalePrice?: number | null }) =>
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

  // Master data: Categories
  categories: {
    list: () => request<Category[]>('/api/categories'),
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedMasterData<Category>>(`/api/categories/paged?${params}`)
    },
    get: (id: string) => request<Category>(`/api/categories/${id}`),
    create: (data: { name: string; description?: string | null }) =>
      request<Category>('/api/categories', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; description?: string | null; isActive?: boolean }) =>
      request<Category>(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  },

  // Master data: Brands
  brands: {
    list: () => request<Brand[]>('/api/brands'),
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedMasterData<Brand>>(`/api/brands/paged?${params}`)
    },
    get: (id: string) => request<Brand>(`/api/brands/${id}`),
    create: (data: { name: string }) =>
      request<Brand>('/api/brands', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; isActive?: boolean }) =>
      request<Brand>(`/api/brands/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: string) =>
      request<{ success: boolean }>(`/api/brands/${id}`, { method: 'DELETE' }),
  },

  // Master data: Units
  units: {
    list: () => request<MasterUnit[]>('/api/units'),
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedMasterData<MasterUnit>>(`/api/units/paged?${params}`)
    },
    get: (id: string) => request<MasterUnit>(`/api/units/${id}`),
    create: (data: { name: string }) =>
      request<MasterUnit>('/api/units', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; isActive?: boolean }) =>
      request<MasterUnit>(`/api/units/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    remove: (id: string) =>
      request<{ success: boolean }>(`/api/units/${id}`, { method: 'DELETE' }),
  },

  // Parties: Suppliers
  suppliers: {
    list: () => request<Supplier[]>('/api/suppliers'),
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedMasterData<Supplier>>(`/api/suppliers/paged?${params}`)
    },
    get: (id: string) => request<Supplier>(`/api/suppliers/${id}`),
    create: (data: { name: string; phone?: string | null; address?: string | null; notes?: string | null }) =>
      request<Supplier>('/api/suppliers', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; phone?: string | null; address?: string | null; notes?: string | null; isActive?: boolean }) =>
      request<Supplier>(`/api/suppliers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    statement: (id: string) => request<PartyStatement>(`/api/suppliers/${id}/statement`),
  },

  // Parties: Clients
  clients: {
    list: () => request<Client[]>('/api/clients'),
    listPaged: (page = 1, pageSize = 20, q?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (q) params.set('q', q)
      return request<PagedMasterData<Client>>(`/api/clients/paged?${params}`)
    },
    getActive: () => request<Client[]>('/api/clients'),
    get: (id: string) => request<Client>(`/api/clients/${id}`),
    create: (data: { name: string; phone?: string | null; address?: string | null; notes?: string | null }) =>
      request<Client>('/api/clients', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: { name?: string; phone?: string | null; address?: string | null; notes?: string | null; isActive?: boolean }) =>
      request<Client>(`/api/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    statement: (id: string) => request<PartyStatement>(`/api/clients/${id}/statement`),
  },

  // Purchases
  purchases: {
    listPaged: (page = 1, pageSize = 20, opts?: { status?: string; q?: string }) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (opts?.status && opts.status !== 'all') params.set('status', opts.status)
      if (opts?.q) params.set('q', opts.q)
      return request<PagedPurchases>(`/api/purchases?${params}`)
    },
    get: (id: string) => request<PurchaseInvoice>(`/api/purchases/${id}`),
    create: (data: SavePurchaseRequest) =>
      request<PurchaseInvoice>('/api/purchases', { method: 'POST', body: JSON.stringify(data) }),
    update: (id: string, data: SavePurchaseRequest) =>
      request<PurchaseInvoice>(`/api/purchases/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    post: (id: string) => request<PurchaseInvoice>(`/api/purchases/${id}/post`, { method: 'POST' }),
    cancel: (id: string) => request<PurchaseInvoice>(`/api/purchases/${id}/cancel`, { method: 'POST' }),
  },

  // Payments
  payments: {
    listPaged: (
      page = 1,
      pageSize = 20,
      opts?: { clientId?: string; supplierId?: string; invoiceId?: string }
    ) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (opts?.clientId) params.set('clientId', opts.clientId)
      if (opts?.supplierId) params.set('supplierId', opts.supplierId)
      if (opts?.invoiceId) params.set('invoiceId', opts.invoiceId)
      return request<PagedPayments>(`/api/payments?${params}`)
    },
    create: (data: CreatePaymentRequest) =>
      request<Payment>('/api/payments', { method: 'POST', body: JSON.stringify(data) }),
    summary: (ids: string[]) =>
      request<{ paid: Record<string, number> }>(`/api/payments/summary?ids=${ids.join(',')}`),
  },

  // Invoices
  invoices: {
    list: () => request<Invoice[]>('/api/invoices'),
    listPaged: (page = 1, pageSize = 20, opts?: { from?: string; to?: string; q?: string; status?: string }) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (opts?.from) params.set('from', opts.from)
      if (opts?.to) params.set('to', opts.to)
      if (opts?.q) params.set('q', opts.q)
      if (opts?.status && opts.status !== 'all') params.set('status', opts.status)
      return request<PagedInvoices>(`/api/invoices/paged?${params}`)
    },
    get: (id: string) => request<Invoice>(`/api/invoices/${id}`),
    create: (data: InvoiceCreatePayload) =>
      request<Invoice>('/api/invoices', { method: 'POST', body: JSON.stringify(data) }),
    updateDraft: (id: string, data: InvoiceCreatePayload) =>
      request<Invoice>(`/api/invoices/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    post: (id: string) => request<Invoice>(`/api/invoices/${id}/post`, { method: 'POST' }),
    cancel: (id: string) => request<Invoice>(`/api/invoices/${id}/cancel`, { method: 'POST' }),
  },

  // Sale returns (Phase 7)
  saleReturns: {
    listPaged: (page = 1, pageSize = 20, invoiceId?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (invoiceId) params.set('invoiceId', invoiceId)
      return request<PagedSaleReturns>(`/api/salereturns?${params}`)
    },
    create: (
      invoiceId: string,
      data: { items: { invoiceDetailId: string; quantity: number }[]; notes?: string }
    ) =>
      request<SaleReturn>(
        `/api/salereturns?invoiceId=${encodeURIComponent(invoiceId)}`,
        { method: 'POST', body: JSON.stringify(data) }
      ),
  },

  // Purchase returns (Phase 8)
  purchaseReturns: {
    listPaged: (page = 1, pageSize = 20, purchaseId?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (purchaseId) params.set('purchaseId', purchaseId)
      return request<PagedPurchaseReturns>(`/api/purchasereturns?${params}`)
    },
    create: (
      purchaseId: string,
      data: { items: { purchaseItemId: string; quantity: number }[]; notes?: string }
    ) =>
      request<PurchaseReturn>(
        `/api/purchasereturns?purchaseId=${encodeURIComponent(purchaseId)}`,
        { method: 'POST', body: JSON.stringify(data) }
      ),
  },

  // Shifts & cash sessions (Phase 9)
  shifts: {
    listPaged: (page = 1, pageSize = 20, status?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (status && status !== 'all') params.set('status', status)
      return request<PagedShifts>(`/api/shifts?${params}`)
    },
    getActive: () => request<Shift | null>('/api/shifts/active'),
    open: (data: { openingCash: number; notes?: string }) =>
      request<Shift>('/api/shifts', { method: 'POST', body: JSON.stringify(data) }),
    close: (id: string, countedCash: number) =>
      request<Shift>(`/api/shifts/${id}/close`, { method: 'POST', body: JSON.stringify({ countedCash }) }),
    report: (id: string) => request<ShiftReport>(`/api/shifts/${id}/report`),
  },

  expenses: {
    listPaged: (page = 1, pageSize = 20, categoryId?: string, from?: string, to?: string) => {
      const params = new URLSearchParams()
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))
      if (categoryId) params.set('categoryId', categoryId)
      if (from) params.set('from', from)
      if (to) params.set('to', to)
      return request<PagedExpenses>(`/api/expenses?${params}`)
    },
    create: (data: { categoryId: string; amount: number; paymentMethod?: string; description?: string; reference?: string }) =>
      request<Expense>('/api/expenses', { method: 'POST', body: JSON.stringify(data) }),
    categories: (includeInactive = false) =>
      request<ExpenseCategory[]>(`/api/expenses/categories?includeInactive=${includeInactive ? 'true' : 'false'}`),
    createCategory: (name: string) =>
      request<ExpenseCategory>('/api/expenses/categories', { method: 'POST', body: JSON.stringify({ name }) }),
    updateCategory: (id: string, data: { name: string; isActive: boolean }) =>
      request<ExpenseCategory>(`/api/expenses/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  },

  // Reports
  reports: {
    lowStock: () => request<Product[]>('/api/reports/low-stock'),
    sales: (from: string, to: string) =>
      request<SalesReport>(`/api/reports/sales?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
    purchases: (from: string, to: string) =>
      request<PurchasesReport>(`/api/reports/purchases?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
    profit: (from: string, to: string) =>
      request<ProfitReport>(`/api/reports/profit?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
    inventory: () => request<InventoryValuationRow[]>('/api/reports/inventory'),
    returns: (from: string, to: string) =>
      request<ReturnsReport>(`/api/reports/returns?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
    expenses: (from: string, to: string) =>
      request<ExpensesReport>(`/api/reports/expenses?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
    cash: (from: string, to: string) =>
      request<CashReport>(`/api/reports/cash?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
  },

  printerSettings: {
    get: () => request<PrinterSettings>('/api/settings/printing'),
    save: (data: PrinterSettings) =>
      request<PrinterSettings>('/api/settings/printing', { method: 'PUT', body: JSON.stringify(data) }),
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
  clientId?: string | null
  paymentMethod?: 'cash' | 'credit'
  status?: 'draft' | 'posted'
}

export interface SaleReturnDetail {
  id: string
  returnId: string
  invoiceDetailId: string
  productId: string
  unitName?: string | null
  quantity: number
  quantityFactor: number
  unitPrice: number
  lineTotal: number
  restoredCost?: number
  product?: Product | null
}

export interface SaleReturn {
  id: string
  number: number
  invoiceId: string
  invoiceNumber?: number
  date: string
  totalAmount: number
  restoredCost?: number
  paymentMethod?: string
  notes?: string | null
  status?: string
  createdAt: string
  details?: SaleReturnDetail[]
}

export interface PurchaseReturnDetail {
  id: string
  returnId: string
  purchaseItemId: string
  productId: string
  unitName?: string | null
  quantity: number
  quantityFactor: number
  unitCost: number
  lineTotal: number
  product?: Product | null
}

export interface PurchaseReturn {
  id: string
  number: number
  purchaseInvoiceId: string
  purchaseInvoiceNumber?: number
  date: string
  totalAmount: number
  paymentMethod?: string
  notes?: string | null
  status?: string
  createdAt: string
  details?: PurchaseReturnDetail[]
}

export interface PagedPurchaseReturns {
  items: PurchaseReturn[]
  total: number
}

export interface Shift {
  id: string
  number: number
  openedBy: string
  openingCash: number
  openedAt: string
  closedAt: string | null
  countedCash: number | null
  expectedCash: number | null
  difference: number | null
  notes?: string | null
  status: 'open' | 'closed'
}

export interface PagedShifts {
  items: Shift[]
  total: number
}

export interface ShiftReportEntry {
  date: string
  description: string
  amount: number
}

export interface ShiftReport {
  shift: Shift
  openingCash: number
  cashSales: number
  saleRefunds: number
  otherCashIn: number
  supplierPaymentsOut: number
  supplierRefundsIn: number
  expensesOut: number
  expectedCash: number
  entries: ShiftReportEntry[]
}

export interface ExpenseCategory {
  id: string
  name: string
  isActive: boolean
  createdAt: string
}

export interface Expense {
  id: string
  categoryId: string
  categoryName?: string | null
  amount: number
  paymentMethod: 'cash' | 'card' | 'bank_transfer'
  date: string
  description?: string | null
  reference?: string | null
  shiftId?: string | null
  createdBy?: string | null
  createdAt: string
}

export interface PagedExpenses {
  items: Expense[]
  total: number
}

export interface SalesByDayRow {
  day: string
  invoiceCount: number
  netSales: number
  discounts: number
}

export interface SalesByMethodRow {
  paymentMethod: string
  invoiceCount: number
  total: number
}

export interface TopProductRow {
  productId: string
  productName: string
  quantity: number
  revenue: number
  cost: number
}

export interface SalesByUserRow {
  userName: string
  invoiceCount: number
  total: number
}

export interface SalesReport {
  grossSales: number
  discounts: number
  netSales: number
  invoiceCount: number
  byDay: SalesByDayRow[]
  byPaymentMethod: SalesByMethodRow[]
  topProducts: TopProductRow[]
  byCashier: SalesByUserRow[]
}

export interface PurchasesByDayRow {
  day: string
  invoiceCount: number
  total: number
}

export interface PurchasesBySupplierRow {
  supplierName: string
  invoiceCount: number
  total: number
}

export interface PurchasesReport {
  total: number
  invoiceCount: number
  byDay: PurchasesByDayRow[]
  bySupplier: PurchasesBySupplierRow[]
}

export interface ProfitByDayRow {
  day: string
  revenue: number
  cost: number
  profit: number
}

export interface ProfitReport {
  grossSales: number
  cogs: number
  saleRefunds: number
  restoredCosts: number
  netRevenue: number
  netCogs: number
  grossProfit: number
  marginPercent: number
  byDay: ProfitByDayRow[]
}

export interface InventoryValuationRow {
  productId: string
  productName: string
  stockQuantity: number
  unitCost: number
  value: number
  categoryName?: string | null
}

export interface ReturnsReport {
  saleReturnCount: number
  saleRefundTotal: number
  purchaseReturnCount: number
  purchaseRefundTotal: number
  saleReturnsByDay: { day: string; count: number; total: number }[]
}

export interface ExpensesReport {
  totalCash: number
  count: number
  byCategory: { categoryName: string; count: number; total: number }[]
}

export interface CashShiftRow {
  number: number
  openedAt: string
  closedAt: string
  openingCash: number
  expectedCash: number
  countedCash: number
  difference: number
}

export interface CashReport {
  totalOpening: number
  totalExpected: number
  totalCounted: number
  totalDifference: number
  shiftCount: number
  shifts: CashShiftRow[]
}

export interface PrinterSettings {
  receiptPrinterName: string
  labelPrinterName: string
  paperWidthMm: 58 | 80
  copies: number
  autoCut: boolean
  openCashDrawer: boolean
  showLogo: boolean
  storeName?: string | null
  storePhone?: string | null
  storeAddress?: string | null
  receiptHeader?: string | null
  receiptFooter?: string | null
}
