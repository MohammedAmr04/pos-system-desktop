import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Package, ShoppingCart, DollarSign } from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { prisma } from "@/lib/db"
import { getTranslations } from "next-intl/server"

export default async function DashboardPage() {
  const t = await getTranslations("Dashboard")
  const productsCount = await prisma.product.count()
  
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const invoices = await prisma.invoice.findMany({
    where: { createdAt: { gte: today } }
  })
  const revenue = invoices.reduce((acc, inv) => acc + inv.totalAmount, 0)
  const discountGiven = invoices.reduce((acc, inv) => acc + inv.discount, 0)

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
            <div className="text-2xl font-bold">+{invoices.length}</div>
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
            <Link href="/pos">
              <Button size="lg" className="h-16">
                <ShoppingCart className="mr-2 h-5 w-5" /> {t("openPOS")}
              </Button>
            </Link>
          </div>
        </Card>
      </div>
    </div>
  )
}
