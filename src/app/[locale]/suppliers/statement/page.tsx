"use client"

import { Suspense } from "react"
import { SupplierStatementPage } from "./supplier-statement-page"

export default function SupplierStatementRoute() {
  return (
    <Suspense>
      <SupplierStatementPage />
    </Suspense>
  )
}
