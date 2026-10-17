"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { Building2, MonitorSmartphone } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { CentralSetupForm } from "./_components/central-setup-form"
import { BranchSetupForm } from "./_components/branch-setup-form"

type RoleChoice = "central" | "branch" | null

export function SetupClient() {
  const t = useTranslations("Setup")
  const [role, setRole] = useState<RoleChoice>(null)

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-2xl space-y-6">
        <div className="space-y-1 text-center">
          <h1 className="text-2xl font-semibold">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">{t("subtitle")}</p>
        </div>

        {role === null && (
          <Card>
            <CardHeader>
              <CardTitle>{t("roleTitle")}</CardTitle>
              <CardDescription>{t("roleDescription")}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2">
              <button
                type="button"
                onClick={() => setRole("central")}
                className="flex flex-col items-start gap-2 rounded-lg border p-4 text-start transition-colors hover:bg-accent"
              >
                <Building2 className="h-6 w-6 text-primary" />
                <span className="font-medium">{t("chooseCentral")}</span>
                <span className="text-xs text-muted-foreground">{t("centralDescription")}</span>
              </button>
              <button
                type="button"
                onClick={() => setRole("branch")}
                className="flex flex-col items-start gap-2 rounded-lg border p-4 text-start transition-colors hover:bg-accent"
              >
                <MonitorSmartphone className="h-6 w-6 text-primary" />
                <span className="font-medium">{t("chooseBranch")}</span>
                <span className="text-xs text-muted-foreground">{t("branchDescription")}</span>
              </button>
            </CardContent>
          </Card>
        )}

        {role === "central" && <CentralSetupForm onBack={() => setRole(null)} />}
        {role === "branch" && <BranchSetupForm onBack={() => setRole(null)} />}
      </div>
    </div>
  )
}
