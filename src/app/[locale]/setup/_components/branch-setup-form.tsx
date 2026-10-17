"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

import { completeSetupAction } from "@/actions/branches.actions"
import { useApiError } from "@/lib/api-error"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

function makeSchema(t: (key: string) => string) {
  const required = t("fieldRequired")
  const invalidUrl = t("invalidUrl")
  return z.object({
    centralBaseUrl: z
      .string()
      .trim()
      .min(1, required)
      .refine((v) => /^https?:\/\//i.test(v), invalidUrl),
    branchId: z.string().trim().min(1, required),
    terminalId: z.string().trim().min(1, required),
    branchKey: z.string().trim(),
    syncToken: z.string().trim().min(1, required),
  })
}

type BranchSetupValues = z.infer<ReturnType<typeof makeSchema>>

export function BranchSetupForm({ onBack }: { onBack: () => void }) {
  const t = useTranslations("Setup")
  const resolveError = useApiError()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<BranchSetupValues>({
    resolver: zodResolver(makeSchema(t as (key: string) => string)),
    defaultValues: {
      centralBaseUrl: "",
      branchId: "",
      terminalId: "",
      branchKey: "",
      syncToken: "",
    },
  })

  async function onSubmit(values: BranchSetupValues) {
    try {
      await completeSetupAction({
        nodeRole: "branch",
        centralBaseUrl: values.centralBaseUrl.trim(),
        branchId: values.branchId.trim(),
        terminalId: values.terminalId.trim(),
        branchKey: values.branchKey.trim() || null,
        syncToken: values.syncToken.trim(),
      })
      toast.success(t("completeTitle"))
    } catch (e) {
      toast.error(resolveError(e) || t("setupFailed"))
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("branchTitle")}</CardTitle>
        <CardDescription>{t("branchHint")}</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <label className="text-sm font-medium">{t("centralBaseUrl")}</label>
              <Input dir="ltr" placeholder={t("centralBaseUrlPlaceholder")} {...register("centralBaseUrl")} />
              {errors.centralBaseUrl && <p className="text-sm text-destructive">{errors.centralBaseUrl.message}</p>}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t("branchId")}</label>
              <Input dir="ltr" placeholder={t("branchIdPlaceholder")} {...register("branchId")} />
              {errors.branchId && <p className="text-sm text-destructive">{errors.branchId.message}</p>}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t("terminalId")}</label>
              <Input dir="ltr" placeholder={t("terminalIdPlaceholder")} {...register("terminalId")} />
              {errors.terminalId && <p className="text-sm text-destructive">{errors.terminalId.message}</p>}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t("branchKey")}</label>
              <Input dir="ltr" placeholder={t("branchKeyPlaceholder")} {...register("branchKey")} />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t("syncToken")}</label>
              <Input dir="ltr" placeholder={t("syncTokenPlaceholder")} {...register("syncToken")} />
              {errors.syncToken && <p className="text-sm text-destructive">{errors.syncToken.message}</p>}
            </div>
          </div>

          <div className="flex gap-2">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="me-2 h-4 w-4 animate-spin" /> : null}
              {isSubmitting ? t("saving") : t("save")}
            </Button>
            <Button type="button" variant="ghost" onClick={onBack} disabled={isSubmitting}>
              {t("back")}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
