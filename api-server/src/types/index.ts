interface AppSettings {
  id: string;
  machineId: string;
  activatedAt: Date;
  lastCheckedAt: Date;
  unlocked: boolean;
  createdAt: Date;
  updatedAt: Date;
}

interface Product {
  id: string;
  barcode: string | null;
  name: string;
  buyPrice: number;
  salePrice: number;
  stockQuantity: number;
  notes: string | null;
  createdAt: Date;
  updatedAt: Date;
}

interface CreateProductData {
  barcode?: string | null;
  name: string;
  buyPrice: number;
  salePrice: number;
  stockQuantity: number;
  notes?: string | null;
}

interface UpdateProductData extends Partial<CreateProductData> {
  id: string;
}

interface Invoice {
  id: string;
  totalAmount: number;
  discount: number;
  createdAt: Date;
  InvoiceDetail: InvoiceDetail[];
}

interface InvoiceDetail {
  id: string;
  invoiceId: string;
  productId: string | null;
  product: Product | null;
  quantity: number;
  buyPrice: number;
  salePrice: number;
}

interface CartItem {
  id: string;
  productId: string;
  name: string;
  buyPrice: number;
  salePrice: number;
  quantity: number;
  maxStock: number;
}

interface LicenseStatus {
  status: "ok" | "first_boot" | "tampered" | "locked";
  machineId?: string;
  daysSinceActivation?: number;
}

export type {
  AppSettings,
  Product,
  CreateProductData,
  UpdateProductData,
  Invoice,
  InvoiceDetail,
  CartItem,
  LicenseStatus,
};