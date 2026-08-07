"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { InvoicesClient } from "./invoices-client"
import { api, Invoice } from "@/lib/api"
import { format } from "date-fns"

const PAGE_SIZE = 20

export default function InvoicesPage() {
  const t = useTranslations("Invoices")
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [total, setTotal] = useState(0)
  const [totals, setTotals] = useState({ revenue: 0, discounts: 0 })
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [fromDate, setFromDate] = useState<Date | undefined>(undefined)
  const [toDate, setToDate] = useState<Date | undefined>(undefined)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    api.invoices
      .listPaged(page, PAGE_SIZE, {
        from: fromDate ? format(fromDate, "yyyy-MM-dd") : undefined,
        to: toDate ? format(toDate, "yyyy-MM-dd") : undefined,
        q: query.trim() || undefined,
      })
      .then((res) => {
        if (cancelled) return
        setInvoices(res.items)
        setTotal(res.total)
        setTotals(res.totals)
      })
      .catch(() => {
        if (cancelled) return
        setInvoices([])
        setTotal(0)
        setTotals({ revenue: 0, discounts: 0 })
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, query, fromDate, toDate])

  const handleQueryChange = useCallback((q: string) => {
    setLoading(true)
    setQuery(q)
    setPage(1)
  }, [])

  const handleDateChange = useCallback((from?: Date, to?: Date) => {
    setLoading(true)
    setFromDate(from)
    setToDate(to)
    setPage(1)
  }, [])

  const handlePageChange = useCallback((p: number) => {
    setLoading(true)
    setPage(p)
  }, [])

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <InvoicesClient
        items={invoices}
        total={total}
        page={page}
        pageSize={PAGE_SIZE}
        loading={loading}
        totals={totals}
        query={query}
        fromDate={fromDate}
        toDate={toDate}
        onQueryChange={handleQueryChange}
        onDateChange={handleDateChange}
        onPageChange={handlePageChange}
      />
    </div>
  )
}
