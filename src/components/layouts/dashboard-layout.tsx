"use client"

import { usePathname } from "@/i18n/navigation"
import { cn } from "@/lib/utils"
import { Package, ShoppingCart, FileText, LayoutDashboard, AlertTriangle, LogOut, Settings, Users, Shield, SlidersHorizontal, KeyRound } from "lucide-react"
import { useTranslations } from "next-intl"
import { Link } from "@/i18n/navigation"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { Button } from "@/components/ui/button"

interface SidebarNavItem {
  key: string
  href: string
  icon: typeof LayoutDashboard
  permission?: string
  feature?: string
  section?: "main" | "settings"
}

const sidebarNavItems: SidebarNavItem[] = [
  {
    key: "dashboard",
    href: "/",
    icon: LayoutDashboard,
    section: "main",
  },
  {
    key: "posCheckout",
    href: "/pos/",
    icon: ShoppingCart,
    section: "main",
  },
  {
    key: "products",
    href: "/products/",
    icon: Package,
    permission: PERMISSIONS.PRODUCTS_VIEW,
    section: "main",
  },
  {
    key: "invoices",
    href: "/invoices/",
    icon: FileText,
    permission: PERMISSIONS.INVOICES_VIEW,
    section: "main",
  },
  {
    key: "lowStock",
    href: "/low-stock/",
    icon: AlertTriangle,
    permission: PERMISSIONS.REPORTS_VIEW,
    feature: FEATURES.LOW_STOCK_REPORT,
    section: "main",
  },
  {
    key: "settingsUsers",
    href: "/settings/users/",
    icon: Users,
    permission: PERMISSIONS.USERS_MANAGE,
    section: "settings",
  },
  {
    key: "settingsRoles",
    href: "/settings/roles/",
    icon: Shield,
    permission: PERMISSIONS.ROLES_MANAGE,
    section: "settings",
  },
  {
    key: "settingsPermissions",
    href: "/settings/permissions/",
    icon: KeyRound,
    permission: PERMISSIONS.ROLES_MANAGE,
    section: "settings",
  },
  {
    key: "settingsFeatures",
    href: "/settings/features/",
    icon: SlidersHorizontal,
    permission: PERMISSIONS.SETTINGS_VIEW,
    section: "settings",
  },
]

export function DashboardLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  const t = useTranslations("Sidebar")
  const appT = useTranslations("App")
  const { session, hasAccess, logout } = useAuth()

  if (pathname === '/pos/') {
    return <>{children}</>
  }

  const visibleItems = sidebarNavItems.filter((item) => {
    if (!item.permission) return true
    return hasAccess(item.permission, item.feature)
  })

  const mainItems = visibleItems.filter((item) => item.section !== "settings")
  const settingsItems = visibleItems.filter((item) => item.section === "settings")

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
              {mainItems.map((item) => {
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
              {settingsItems.length > 0 && (
                <>
                  <div className="flex items-center gap-2 px-3 pt-4 pb-1 text-xs text-muted-foreground">
                    <Settings className="h-4 w-4" />
                    <span>{t("settings")}</span>
                  </div>
                  {settingsItems.map((item) => {
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
                </>
              )}
            </nav>
          </div>
          <div className="flex items-center justify-between gap-2 border-t p-3 text-sm">
            <div className="min-w-0">
              <p className="truncate font-medium">{session?.user?.name}</p>
              <p className="truncate text-xs text-muted-foreground">
                {session?.roles?.join(", ")}
              </p>
            </div>
            <Button variant="ghost" size="icon" onClick={logout} title={t("logout")}>
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </aside>
      <main className="flex flex-1 flex-col gap-4 p-4 lg:gap-6 lg:p-6">
        {children}
      </main>
    </div>
  )
}
