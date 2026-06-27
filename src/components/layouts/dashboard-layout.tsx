"use client"

import { usePathname } from "next/navigation"
import { cn } from "@/lib/utils"
import { Package, ShoppingCart, FileText, LayoutDashboard } from "lucide-react"
import { useTranslations } from "next-intl"
import { Link } from "@/i18n/navigation"

const sidebarNavItems = [
  {
    key: "dashboard" as const,
    href: "/",
    icon: LayoutDashboard,
  },
  {
    key: "posCheckout" as const,
    href: "/pos",
    icon: ShoppingCart,
  },
  {
    key: "products" as const,
    href: "/products",
    icon: Package,
  },
  {
    key: "invoices" as const,
    href: "/invoices",
    icon: FileText,
  },
]

export function DashboardLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  const t = useTranslations("Sidebar")
  const appT = useTranslations("App")

  if (pathname === '/pos') {
    return <>{children}</>
  }

  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      <aside className="border-l bg-muted/40 lg:w-64">
        <div className="flex h-full max-h-screen flex-col gap-2">
          <div className="flex h-14 items-center border-b px-4 lg:h-[60px] lg:px-6">
            <Link href="/" className="flex items-center gap-2 font-semibold">
              <Package className="h-6 w-6" />
              <span>{appT("name")}</span>
            </Link>
          </div>
          <div className="flex-1">
            <nav className="grid items-start px-2 text-sm font-medium lg:px-4 mt-4 space-y-1">
              {sidebarNavItems.map((item) => {
                const Icon = item.icon
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={cn(
                      "flex items-center gap-3 rounded-lg px-3 py-2 transition-all hover:text-primary",
                      pathname === item.href
                        ? "bg-muted text-primary"
                        : "text-muted-foreground"
                    )}
                  >
                    <Icon className="h-4 w-4" />
                    {t(item.key)}
                  </Link>
                )
              })}
            </nav>
          </div>
        </div>
      </aside>
      <main className="flex flex-1 flex-col gap-4 p-4 lg:gap-6 lg:p-6">
        {children}
      </main>
    </div>
  )
}
