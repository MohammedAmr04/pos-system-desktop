"use client"

import { Suspense } from "react"
import { Loader2 } from "lucide-react"
import { ShiftInvoicesClient } from "./shift-invoices-client"

export default function ShiftInvoicesPage() {
  return (
    <Suspense fallback={<Loader2 className="mx-auto mt-10 h-6 w-6 animate-spin" />}>
      <ShiftInvoicesClient />
    </Suspense>
  )
}