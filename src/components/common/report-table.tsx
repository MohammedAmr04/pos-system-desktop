"use client"

import { useTranslations } from "next-intl"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

export function ReportTable({
  headers,
  children,
  empty,
}: {
  headers: string[]
  children: React.ReactNode
  empty?: string
}) {
  const tc = useTranslations("Common")
  let body = children
  const rows = Array.isArray(children) ? children : [children]
  const isEmpty = rows.every(
    (r) => r === null || r === undefined || r === false || (Array.isArray(r) && r.length === 0),
  )
  if (isEmpty) {
    body = (
      <TableRow>
        <TableCell colSpan={headers.length} className="h-20 text-center text-muted-foreground">
          {empty ?? tc("noResults")}
        </TableCell>
      </TableRow>
    )
  }
  return (
    <div className="rounded-md border bg-card">
      <Table>
        <TableHeader>
          <TableRow>
            {headers.map((h) => (
              <TableHead key={h}>{h}</TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>{body}</TableBody>
      </Table>
    </div>
  )
}
