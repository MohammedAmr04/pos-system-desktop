"use client"

import { useQuery } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Package, ShoppingCart, DollarSign } from "lucide-react"
import { Link } from "@/i18n/navigation"
import { Button } from "@/components/ui/button"
import { countProducts } from "@/api/products"
import { listInvoicesPaged } from "@/api/invoices"
import { productsKeys } from "@/hooks/use-products"
import { format } from "date-fns"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"

export default function DashboardPage() {
  const t = useTranslations("Dashboard")
  const { hasPermission, hasAccess } = useAuth()
  const canViewProducts = hasPermission(PERMISSIONS.PRODUCTS_VIEW)
  const canViewInvoices = hasPermission(PERMISSIONS.INVOICES_VIEW)
  const canUsePOS = hasAccess(PERMISSIONS.INVOICES_CREATE) && hasPermission(PERMISSIONS.PRODUCTS_VIEW)

  const { data: productsCount = 0 } = useQuery({
    queryKey: productsKeys.count(),
    queryFn: countProducts,
    enabled: canViewProducts,
  })

  // Aggregate today's stats on the backend; pageSize=1 keeps the payload tiny.
  const { data: todayInvoices } = useQuery({
    queryKey: ["invoices", "paged", 1, 1, { from: format(new Date(), "yyyy-MM-dd") }],
    queryFn: () => listInvoicesPaged(1, 1, { from: format(new Date(), "yyyy-MM-dd") }),
    enabled: canViewInvoices,
  })

  const revenue = todayInvoices?.totals.revenue ?? 0
  const salesCount = todayInvoices?.total ?? 0
  const discountGiven = todayInvoices?.totals.discounts ?? 0

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("todayRevenue")}</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{revenue.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("salesCount")}</CardTitle>
            <ShoppingCart className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">+{salesCount}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("discountsGiven")}</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{discountGiven.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("productsInDb")}</CardTitle>
            <Package className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{productsCount}</div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7 mt-8">
        <Card className="col-span-4 flex flex-col justify-center items-center py-10">
          <h3 className="text-lg font-semibold mb-4">{t("quickActions")}</h3>
          <div className="flex gap-4">
            {canUsePOS && (
              <Link href="/pos">
                <Button size="lg" className="h-16">
                  <ShoppingCart className="mr-2 h-5 w-5" /> {t("openPOS")}
                </Button>
              </Link>
            )}
          </div>
        </Card>
      </div>
    </div>
  )
}
