"use client"

import { usePathname } from "@/i18n/navigation"
import { Package, ShoppingCart, FileText, LayoutDashboard, AlertTriangle, LogOut, Settings, Users, Shield, SlidersHorizontal, KeyRound, FolderTree, Tags, Ruler, Truck, UsersRound, Boxes, Wallet, Undo2, Timer, Receipt, BarChart3, Printer, ClipboardList, Bell, History, Contact } from "lucide-react"
import { useTranslations } from "next-intl"
import { Link } from "@/i18n/navigation"
import { useAuth } from "@/components/common/auth-context"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarTrigger,
} from "@/components/ui/sidebar"
import { Separator } from "@/components/ui/separator"

interface SidebarNavItem {
  key: string
  href: string
  icon: typeof LayoutDashboard
  permission?: string
  feature?: string
  section?: "main" | "reports" | "settings"
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
    key: "categories",
    href: "/categories/",
    icon: FolderTree,
    permission: PERMISSIONS.CATEGORIES_VIEW,
    feature: FEATURES.CATEGORIES,
    section: "main",
  },
  {
    key: "brands",
    href: "/brands/",
    icon: Tags,
    permission: PERMISSIONS.BRANDS_VIEW,
    feature: FEATURES.BRANDS,
    section: "main",
  },
  {
    key: "unitsMaster",
    href: "/units/",
    icon: Ruler,
    permission: PERMISSIONS.UNITS_VIEW,
    section: "main",
  },
  {
    key: "suppliers",
    href: "/suppliers/",
    icon: Truck,
    permission: PERMISSIONS.SUPPLIERS_VIEW,
    section: "main",
  },
  {
    key: "clients",
    href: "/clients/",
    icon: UsersRound,
    permission: PERMISSIONS.CLIENTS_VIEW,
    section: "main",
  },
  {
    key: "employees",
    href: "/employees/",
    icon: Contact,
    permission: PERMISSIONS.EMPLOYEES_VIEW,
    section: "main",
  },
  {
    key: "purchases",
    href: "/purchases/",
    icon: Boxes,
    permission: PERMISSIONS.PURCHASES_VIEW,
    section: "main",
  },
  {
    key: "payments",
    href: "/payments/",
    icon: Wallet,
    permission: PERMISSIONS.PAYMENTS_VIEW,
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
    key: "returns",
    href: "/returns/",
    icon: Undo2,
    permission: PERMISSIONS.INVOICES_VIEW,
    section: "main",
  },
  {
    key: "shifts",
    href: "/shifts/",
    icon: Timer,
    permission: PERMISSIONS.SHIFTS_VIEW,
    section: "main",
  },
  {
    key: "expenses",
    href: "/expenses/",
    icon: Receipt,
    permission: PERMISSIONS.EXPENSES_VIEW,
    section: "main",
  },
  { key: "inventoryAdjustments", href: "/inventory-adjustments/", icon: ClipboardList, permission: PERMISSIONS.INVENTORY_ADJUSTMENTS_VIEW, section: "main" },
  { key: "alerts", href: "/alerts/", icon: Bell, permission: PERMISSIONS.ALERTS_VIEW, section: "main" },
  { key: "auditLogs", href: "/audit-logs/", icon: History, permission: PERMISSIONS.AUDIT_VIEW, section: "settings" },
  {
    key: "lowStock",
    href: "/low-stock/",
    icon: AlertTriangle,
    permission: PERMISSIONS.REPORTS_VIEW,
    feature: FEATURES.LOW_STOCK_REPORT,
    section: "reports",
  },
  {
    key: "reportsHub",
    href: "/reports/",
    icon: BarChart3,
    permission: PERMISSIONS.REPORTS_VIEW,
    section: "reports",
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
  {
    key: "settingsPrinting",
    href: "/settings/printing/",
    icon: Printer,
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

  const mainItems = visibleItems.filter((item) => item.section === "main")
  const reportItems = visibleItems.filter((item) => item.section === "reports")
  const settingsItems = visibleItems.filter((item) => item.section === "settings")

  return (
    <SidebarProvider>
      <Sidebar side="right">
        <SidebarHeader>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton size="lg" render={<Link href="/" />}>
                <div className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                  <Package className="size-4" />
                </div>
                <div className="grid flex-1 text-start text-sm leading-tight">
                  <span className="truncate font-semibold">{appT("name")}</span>
                </div>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupContent>
              <SidebarMenu>
                {mainItems.map((item) => (
                  <SidebarMenuItem key={item.href}>
                    <SidebarMenuButton
                      render={<Link href={item.href} />}
                      isActive={pathname === item.href}
                      tooltip={t(item.key)}
                    >
                      <item.icon />
                      <span>{t(item.key)}</span>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
          {reportItems.length > 0 && (
            <SidebarGroup>
              <SidebarGroupLabel>
                <BarChart3 />
                <span>{t("reports")}</span>
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {reportItems.map((item) => (
                    <SidebarMenuItem key={item.href}>
                      <SidebarMenuButton
                        render={<Link href={item.href} />}
                        isActive={pathname === item.href}
                        tooltip={t(item.key)}
                      >
                        <item.icon />
                        <span>{t(item.key)}</span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          )}
          {settingsItems.length > 0 && (
            <SidebarGroup>
              <SidebarGroupLabel>
                <Settings />
                <span>{t("settings")}</span>
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {settingsItems.map((item) => (
                    <SidebarMenuItem key={item.href}>
                      <SidebarMenuButton
                        render={<Link href={item.href} />}
                        isActive={pathname === item.href}
                        tooltip={t(item.key)}
                      >
                        <item.icon />
                        <span>{t(item.key)}</span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          )}
        </SidebarContent>
        <SidebarFooter>
          <div className="flex items-center justify-between gap-2 px-2 py-1.5 text-sm group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0">
            <div className="min-w-0 flex-1 group-data-[collapsible=icon]:hidden">
              <p className="truncate font-medium">{session?.user?.name}</p>
              <p className="truncate text-xs text-muted-foreground">
                {session?.roles?.join(", ")}
              </p>
            </div>
            <TooltipIconButton label={t("logout")} onClick={logout} size="icon-sm">
              <LogOut className="h-4 w-4" />
            </TooltipIconButton>
          </div>
        </SidebarFooter>
        <SidebarRail />
      </Sidebar>
      <SidebarInset>
        <header className="flex h-14 shrink-0 items-center gap-2 border-b bg-background px-4">
          <SidebarTrigger className="-ms-1" />
          <Separator orientation="vertical" className="!h-4" />
        </header>
        <main className="flex flex-1 flex-col gap-4 p-4 lg:gap-6 lg:p-6">
          {children}
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}
