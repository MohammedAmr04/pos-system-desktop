"use client"

import { useTranslations } from "next-intl"
import { ShieldX } from "lucide-react"

export function AccessDenied() {
  const t = useTranslations("Access")
  return (
    <div className="flex h-screen items-center justify-center bg-muted/20 p-8">
      <div className="max-w-md text-center space-y-4">
        <ShieldX className="mx-auto h-16 w-16 text-destructive" />
        <h1 className="text-2xl font-bold">{t("deniedTitle")}</h1>
        <p className="text-sm text-muted-foreground">{t("deniedDescription")}</p>
      </div>
    </div>
  )
}
