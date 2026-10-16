"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

import { BranchRuntimeContext } from "@/types/domain/domain.types"
import { useAllBranches, useBranchContext, useBranchTerminals } from "@/hooks/use-branches"
import { configureBranchRuntimeAction, saveBranchCredentialsAction } from "@/actions/branches.actions"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

const NONE = "__none__"

export function BranchRuntimeCard() {
  const t = useTranslations("BranchesAdmin")
  const { data, isPending } = useBranchContext()

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("runtimeTitle")}</CardTitle>
        <CardDescription>{t("runtimeDescription")}</CardDescription>
      </CardHeader>
      <CardContent>
        {isPending ? (
          <div className="flex h-24 items-center justify-center">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <RuntimeForm
            key={`${data?.branch?.id ?? "none"}-${data?.terminal?.id ?? "none"}-${data?.nodeRole ?? "none"}`}
            context={data ?? null}
          />
        )}
      </CardContent>
    </Card>
  )
}

function RuntimeForm({ context }: { context: BranchRuntimeContext | null }) {
  const t = useTranslations("BranchesAdmin")
  const resolveError = useApiError()
  const branches = useAllBranches(false)

  const [nodeRole, setNodeRole] = useState(context?.nodeRole === "central" ? "central" : "branch")
  const [branchId, setBranchId] = useState(context?.branch?.id ?? "")
  const [terminalId, setTerminalId] = useState(context?.terminal?.id ?? "")
  const [centralBaseUrl, setCentralBaseUrl] = useState(context?.centralBaseUrl ?? "")
  const [centralToken, setCentralToken] = useState("")
  const [centralBranchKey, setCentralBranchKey] = useState("")
  const [isSaving, setIsSaving] = useState(false)
  const [isSavingSecrets, setIsSavingSecrets] = useState(false)

  const branchOptions = branches.data ?? []
  const terminals = useBranchTerminals(branchId)

  const handleSaveRuntime = async () => {
    setIsSaving(true)
    try {
      await configureBranchRuntimeAction({
        branchId,
        terminalId,
        nodeRole: nodeRole as "branch" | "central",
        centralBaseUrl: centralBaseUrl.trim() || null,
      })
      toast.success(t("runtimeSaved"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  const handleSaveSecrets = async () => {
    setIsSavingSecrets(true)
    try {
      await saveBranchCredentialsAction({
        centralToken: centralToken.trim() || null,
        centralBranchKey: centralBranchKey.trim() || null,
      })
      setCentralToken("")
      setCentralBranchKey("")
      toast.success(t("credentialsSaved"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSavingSecrets(false)
    }
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("nodeRole")}</label>
          <Select value={nodeRole} onValueChange={(value) => setNodeRole(value ?? "branch")}>
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="branch">{t("roleBranch")}</SelectItem>
              <SelectItem value="central">{t("roleCentral")}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium">{t("branch")}</label>
          <Select value={branchId || NONE} onValueChange={(value) => setBranchId(!value || value === NONE ? "" : value)}>
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={NONE}>{t("none")}</SelectItem>
              {branchOptions.map((branch) => (
                <SelectItem key={branch.id} value={branch.id}>
                  {branch.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium">{t("terminal")}</label>
          <Input
            value={terminalId}
            onChange={(e) => setTerminalId(e.target.value)}
            placeholder={t("terminalIdPlaceholder")}
            list="runtime-terminals"
            dir="ltr"
          />
          <datalist id="runtime-terminals">
            {(terminals.data ?? []).map((terminal) => (
              <option key={terminal.id} value={terminal.id}>
                {terminal.name}
              </option>
            ))}
          </datalist>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium">{t("centralBaseUrl")}</label>
          <Input
            value={centralBaseUrl}
            onChange={(e) => setCentralBaseUrl(e.target.value)}
            placeholder="https://central.example.com"
            dir="ltr"
          />
        </div>
      </div>

      <Button onClick={handleSaveRuntime} disabled={isSaving || !branchId || !terminalId}>
        {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        {t("saveRuntime")}
      </Button>

      <div className="space-y-4 border-t pt-4">
        <div>
          <p className="text-sm font-medium">{t("credentialsTitle")}</p>
          <p className="text-xs text-muted-foreground">{t("credentialsDescription")}</p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("centralToken")}</label>
            <Input
              value={centralToken}
              onChange={(e) => setCentralToken(e.target.value)}
              placeholder={t("unchanged")}
              dir="ltr"
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("centralBranchKey")}</label>
            <Input
              value={centralBranchKey}
              onChange={(e) => setCentralBranchKey(e.target.value)}
              placeholder={t("unchanged")}
              dir="ltr"
            />
          </div>
        </div>
        <Button
          variant="outline"
          onClick={handleSaveSecrets}
          disabled={isSavingSecrets || (!centralToken.trim() && !centralBranchKey.trim())}
        >
          {isSavingSecrets ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
          {t("saveCredentials")}
        </Button>
      </div>

      <div className="rounded-md border bg-muted/30 px-3 py-2 text-xs text-muted-foreground">
        {context ? (
          <span>
            {t("currentContext")}: {context.branch?.name ?? "—"} · {context.terminal?.name ?? context.terminal?.id ?? "—"} ·{" "}
            {context.nodeRole ?? "—"} · {context.status ?? "—"}
          </span>
        ) : (
          <span>{t("noContext")}</span>
        )}
      </div>
    </div>
  )
}
