"use client"

import { Suspense } from "react"
import { ClientStatementPage } from "./client-statement-page"

export default function ClientStatementRoute() {
  return (
    <Suspense>
      <ClientStatementPage />
    </Suspense>
  )
}
