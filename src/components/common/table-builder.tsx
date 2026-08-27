"use client"

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"
import { useTranslations } from "next-intl"

export interface TableColumn<T> {
  key: string
  header: React.ReactNode
  cell: (row: T) => React.ReactNode
  className?: string
  headClassName?: string
}

interface TableBuilderProps<T> {
  columns: TableColumn<T>[]
  data: T[]
  rowKey: (row: T, index: number) => string
  loading?: boolean
  emptyMessage?: React.ReactNode
  onRowClick?: (row: T) => void
  className?: string
}

const LOADING_ROWS = 5

/**
 * Pure presentational table renderer. No internal state: data comes in
 * already filtered/paginated/sorted by the server.
 */
export function TableBuilder<T>({
  columns,
  data,
  rowKey,
  loading = false,
  emptyMessage,
  onRowClick,
  className,
}: TableBuilderProps<T>) {
  const t = useTranslations("Common")

  return (
    <div className={cn("rounded-md border bg-card", className)}>
      <Table>
        <TableHeader>
          <TableRow>
            {columns.map((column) => (
              <TableHead key={column.key} className={column.headClassName}>
                {column.header}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: LOADING_ROWS }).map((_, rowIndex) => (
              <TableRow key={`loading-${rowIndex}`}>
                {columns.map((column) => (
                  <TableCell key={column.key}>
                    <Skeleton className="h-5 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : data.length > 0 ? (
            data.map((row, rowIndex) => (
              <TableRow
                key={rowKey(row, rowIndex)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={onRowClick ? "cursor-pointer" : undefined}
              >
                {columns.map((column) => (
                  <TableCell key={column.key} className={column.className}>
                    {column.cell(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                {emptyMessage ?? t("noResults")}
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  )
}
