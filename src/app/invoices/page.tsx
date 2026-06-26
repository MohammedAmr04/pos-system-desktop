import { getInvoices } from "@/features/invoices/actions"
import { InvoicesClient } from "./invoices-client"

export const dynamic = 'force-dynamic'

export default async function InvoicesPage() {
  const invoices = await getInvoices()
  
  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">Today's Invoices</h2>
      </div>
      <InvoicesClient data={invoices} />
    </div>
  )
}
