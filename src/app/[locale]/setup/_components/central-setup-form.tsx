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
  return z.object({
    centralBaseUrl: z
      .string()
      .trim()
      .refine((v) => v === "" || /^https?:\/\//i.test(v), t("invalidUrl")),
  })
}

type CentralSetupValues = z.infer<ReturnType<typeof makeSchema>>

export function CentralSetupForm({ onBack }: { onBack: () => void }) {
  const t = useTranslations("Setup")
  const resolveError = useApiError()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CentralSetupValues>({
    resolver: zodResolver(makeSchema(t as (key: string) => string)),
    defaultValues: { centralBaseUrl: "" },
  })

  async function onSubmit(values: CentralSetupValues) {
    try {
      await completeSetupAction({
        nodeRole: "central",
        centralBaseUrl: values.centralBaseUrl.trim() || null,
      })
      toast.success(t("completeTitle"))
    } catch (e) {
      toast.error(resolveError(e) || t("setupFailed"))
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("centralTitle")}</CardTitle>
        <CardDescription>{t("centralHint")}</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("centralBaseUrl")}</label>
            <Input dir="ltr" placeholder={t("centralBaseUrlPlaceholder")} {...register("centralBaseUrl")} />
            {errors.centralBaseUrl && <p className="text-sm text-destructive">{errors.centralBaseUrl.message}</p>}
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
