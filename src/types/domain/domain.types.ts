// Central domain types shared across the app (API contracts with the C# backend).

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
  productType: 'product' | 'service' | 'bundle'
  serviceCost: number
  buyPrice: number
  salePrice: number
  stockQuantity: number
  notes: string | null
  allowDiscount: boolean
  lowStockThreshold: number
  isHiddenFromPOS: boolean
  categoryId?: string | null
  brandId?: string | null
  createdAt: string
  updatedAt: string
  availableQuantity?: number
  bundleComponents?: BundleComponent[]
}

export interface BundleComponent {
  id?: string
  bundleProductId?: string
  componentProductId: string
  quantity: number
  product?: Product | null
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
  isHiddenFromPOS?: boolean
  productType?: 'product' | 'service' | 'bundle'
  serviceCost?: number
  bundleComponents?: Array<{ productId: string; quantity: number }>
}

export type PriceMode = 'retail' | 'wholesale'
export type PaymentMethod = 'cash' | 'credit' | 'card' | 'bank_transfer'

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
  employeeId?: string | null
  createdBy?: string | null
  client?: Client | null
  employee?: Employee | null
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
  bundleComponents?: InvoiceBundleComponent[]
  product: Product | null
}

export interface InvoiceBundleComponent {
  productId: string
  name: string
  quantity: number
  buyPrice: number
  serviceCost: number
  productType: 'product' | 'service' | 'bundle'
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
  paidByInvoice: Record<string, number>
}

export interface PagedShiftInvoices {
  items: Invoice[]
  total: number
}

export interface PagedSaleReturns {
  items: SaleReturn[]
  total: number
}

export interface LicenseStatus {
  status: 'ok' | 'first_boot' | 'trial' | 'locked' | 'tampered'
  machineId?: string
  daysSinceActivation?: number
  licenseType?: 'trial' | 'monthly' | 'annual' | 'permanent' | string
  trialDays?: number
  licenseStartedAt?: string | null
  licenseExpiresAt?: string | null
  remainingDays?: number | null
}

export interface AccessBundle {
  user: {
    id: string
    name: string
    isActive: boolean
    mustChangePassword: boolean
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
  balance?: number
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
  balance?: number
}

export type BalanceFilter = "all" | "positive" | "negative" | "zero"

export interface Employee {
  id: string
  name: string
  phone: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface EmployeePerformanceRow {
  employeeId: string | null
  employeeName: string | null
  invoiceCount: number
  total: number
  averageTicket: number
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

export interface PartyInvoices {
  items: Invoice[]
  paidByInvoice: Record<string, number>
}

export interface PartyPurchases {
  items: PurchaseInvoice[]
  paidByInvoice: Record<string, number>
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
  productType?: 'product' | 'service' | 'bundle'
}

export interface InvoiceCreatePayload {
  items: InvoiceItemPayload[]
  discount: number
  discountType?: string | null
  discountValue?: number
  priceMode: PriceMode
  clientId?: string | null
  employeeId?: string | null
  paymentMethod?: PaymentMethod
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

export interface AuditLogEntry { id: string; actorUserId?: string | null; action: string; entityType: string; entityId?: string | null; summary: string; createdAt: string }
export interface AlertItem { id: string; type: string; severity: string; entityType?: string | null; entityId?: string | null; message: string; status: 'open' | 'acknowledged'; acknowledgedBy?: string | null; acknowledgedAt?: string | null; createdAt: string }
export interface InventoryAdjustmentLineInput { productId: string; productUnitId: string; countedQuantity: number; unitCost?: number; isMatched: boolean }
export interface InventoryAdjustmentLine { id: string; productId: string; productUnitId: string; productName: string; barcode: string | null; unitName: string; quantityFactor: number; systemQuantity: number; countedQuantity: number; countedBaseQuantity: number; differenceQuantity: number; unitCost: number; buyPrice: number; retailPrice: number; wholesalePrice: number | null; isMatched: boolean; updatedAt: string | null }
export interface PagedAuditLogs { items: AuditLogEntry[]; total: number }
export interface PagedAlerts { items: AlertItem[]; total: number }
export interface InventoryAdjustmentSummary { id: string; number: number; reason: string; notes: string | null; createdBy: string; createdAt: string; status: 'draft' | 'counting' | 'posted' | 'cancelled'; lineCount: number; differenceCount: number }
export interface PagedInventoryAdjustments { items: InventoryAdjustmentSummary[]; total: number }
export interface InventoryAdjustmentDetail extends InventoryAdjustmentSummary { postedAt: string | null; cancelledAt: string | null; lines: InventoryAdjustmentLine[] }
export interface BackupSummary { fileName: string; sizeBytes: number; createdAt: string }
