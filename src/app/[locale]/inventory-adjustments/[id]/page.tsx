import { InventoryAdjustmentSessionPage } from "./inventory-adjustment-session-page"

export function generateStaticParams() {
  return [{ id: "__session__" }]
}

export default function InventoryAdjustmentPage() {
  return <InventoryAdjustmentSessionPage />
}
