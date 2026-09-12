import { useQuery } from "@tanstack/react-query"
import { getPrinterSettings } from "@/api/printer-settings"

export const printerSettingsKeys = {
  current: () => ["printer-settings"] as const,
}

export function usePrinterSettings() {
  return useQuery({
    queryKey: printerSettingsKeys.current(),
    queryFn: getPrinterSettings,
  })
}
