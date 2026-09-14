"use client"

import { useTranslations } from "next-intl"
import { Card, CardContent } from "@/components/ui/card"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { usePOSStore } from "@/store/pos.store"
import { useActiveClients } from "@/hooks/use-clients"
import { useActiveEmployees } from "@/hooks/use-employees"
import { Client, Employee } from "@/types/domain/domain.types"

export function OrderDetailsPanel() {
  const t = useTranslations("POS")
  const clientId = usePOSStore((s) => s.clientId)
  const employeeId = usePOSStore((s) => s.employeeId)
  const paymentMethod = usePOSStore((s) => s.paymentMethod)
  const setClient = usePOSStore((s) => s.setClient)
  const setEmployee = usePOSStore((s) => s.setEmployee)
  const setPaymentMethod = usePOSStore((s) => s.setPaymentMethod)

  const { data: allClients = [] } = useActiveClients()
  const clients: Client[] = allClients
  const { data: allEmployees = [] } = useActiveEmployees()
  const employees: Employee[] = allEmployees
  const activeClients = clients.filter((client) => client.isActive)
  const activeEmployees = employees.filter((employee) => employee.isActive)

  return (
    <Card className="shrink-0 border-primary/15 bg-background/80 shadow-sm">
      <CardContent className="grid gap-3 sm:grid-cols-3">
        <div className="space-y-1.5">
          <label htmlFor="pos-client" className="text-sm font-medium">{t("client")}</label>
          <Select
            value={clientId ?? ""}
            onValueChange={(value) => setClient(value || null)}
            items={{
              "": t("walkIn"),
              ...Object.fromEntries(activeClients.map((client) => [client.id, client.name])),
            }}
          >
            <SelectTrigger id="pos-client" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">{t("walkIn")}</SelectItem>
              {activeClients.map((client) => (
                <SelectItem key={client.id} value={client.id}>{client.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <label htmlFor="pos-employee" className="text-sm font-medium">{t("salesperson")}</label>
          <Select
            value={employeeId ?? ""}
            onValueChange={(value) => setEmployee(value || null)}
            items={{
              "": t("noSalesperson"),
              ...Object.fromEntries(activeEmployees.map((employee) => [employee.id, employee.name])),
            }}
          >
            <SelectTrigger id="pos-employee" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">{t("noSalesperson")}</SelectItem>
              {activeEmployees.map((employee) => (
                <SelectItem key={employee.id} value={employee.id}>{employee.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <label htmlFor="pos-payment" className="text-sm font-medium">{t("paymentMethod")}</label>
          <Select
            value={paymentMethod}
            onValueChange={(value) => value && setPaymentMethod(value as "cash" | "credit" | "card" | "bank_transfer")}
            items={{
              cash: t("cash"),
              credit: t("credit"),
              card: t("card"),
              bank_transfer: t("bankTransfer"),
            }}
          >
            <SelectTrigger id="pos-payment" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="cash">{t("cash")}</SelectItem>
              <SelectItem value="credit">{t("credit")}</SelectItem>
              <SelectItem value="card">{t("card")}</SelectItem>
              <SelectItem value="bank_transfer">{t("bankTransfer")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </CardContent>
    </Card>
  )
}
