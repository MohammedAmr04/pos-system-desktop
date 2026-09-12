import { request } from "@/lib/api"
import { PrinterSettings } from "@/types/domain/domain.types"

export function getPrinterSettings() {
  return request<PrinterSettings>('/api/settings/printing')
}
