import { request } from "@/lib/api"
import { PrinterSettings } from "@/types/domain/domain.types"

export function savePrinterSettings(data: PrinterSettings) {
  return request<PrinterSettings>('/api/settings/printing', {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}
