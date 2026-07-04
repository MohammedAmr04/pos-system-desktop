"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { InvoicesClient } from "./invoices-client"
import { api, Invoice } from "@/lib/api"

export default function InvoicesPage() {
  const t = useTranslations("Invoices")
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [loading, setLoading] = useState(true)

  const fetchInvoices = () => {
    setLoading(true)
    api.invoices.list().then(setInvoices).finally(() => setLoading(false))
  }

  useEffect(() => {
    fetchInvoices()
  }, [])

  if (loading) {
    return (
      <div className="flex-1 space-y-4 pt-6">
        <div className="flex items-center justify-between space-y-2">
          <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        </div>
        <div className="flex justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <InvoicesClient data={invoices} onRefresh={fetchInvoices} />
    </div>
  )
}
