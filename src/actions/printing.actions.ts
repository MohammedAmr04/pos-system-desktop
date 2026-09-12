import { request } from "@/lib/api"

export function printInvoiceDocument(invoice: unknown) {
  return request<{ success: boolean; message: string }>('/api/printing/print', {
    method: 'POST',
    body: JSON.stringify({ invoice }),
  })
}

export function printBarcodeLabel(data: { barcode: string; name?: string; price?: number; count?: number }) {
  return request<{ success: boolean; message: string }>('/api/printing/print-barcode', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
