"use client"

import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { useTranslations } from "next-intl"

import { Button } from "@/components/ui/button"
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
} from "@/components/ui/pagination"
import { cn } from "@/lib/utils"

interface DataPaginationProps {
  page: number
  pageSize: number
  total: number
  onPageChange: (page: number) => void
  disabled?: boolean
  className?: string
}

/** Always RTL: "next" moves leftward, "previous" rightward. */
function pageWindow(current: number, totalPages: number): (number | "ellipsis")[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1)
  const pages = new Set<number>([1, totalPages, current - 1, current, current + 1])
  const sorted = [...pages].filter((p) => p >= 1 && p <= totalPages).sort((a, b) => a - b)
  const result: (number | "ellipsis")[] = []
  let previous = 0
  for (const p of sorted) {
    if (previous && p - previous > 1) result.push("ellipsis")
    result.push(p)
    previous = p
  }
  return result
}

/**
 * Server-driven pagination control: `total` comes from the API response and
 * every click re-fetches that page from the backend — never slices local data.
 */
export function DataPagination({
  page,
  pageSize,
  total,
  onPageChange,
  disabled = false,
  className,
}: DataPaginationProps) {
  const t = useTranslations("Common")
  const totalPages = Math.max(1, Math.ceil(total / Math.max(1, pageSize)))
  const canPrevious = page > 1
  const canNext = page < totalPages

  if (total === 0) return null

  return (
    <Pagination className={cn("justify-end py-2", className)}>
      <PaginationContent>
        <PaginationItem>
          <Button
            variant="outline"
            size="sm"
            disabled={disabled || !canPrevious}
            onClick={() => onPageChange(page - 1)}
            aria-label={t("previous")}
          >
            <ChevronRightIcon />
            <span className="hidden sm:block">{t("previous")}</span>
          </Button>
        </PaginationItem>

        {pageWindow(page, totalPages).map((entry, index) =>
          entry === "ellipsis" ? (
            <PaginationItem key={`ellipsis-${index}`}>
              <PaginationEllipsis />
            </PaginationItem>
          ) : (
            <PaginationItem key={entry}>
              <Button
                variant={entry === page ? "outline" : "ghost"}
                size="icon-sm"
                disabled={disabled}
                onClick={() => onPageChange(entry)}
                aria-current={entry === page ? "page" : undefined}
                aria-label={`${t("page")} ${entry}`}
              >
                {entry}
              </Button>
            </PaginationItem>
          )
        )}

        <PaginationItem>
          <Button
            variant="outline"
            size="sm"
            disabled={disabled || !canNext}
            onClick={() => onPageChange(page + 1)}
            aria-label={t("next")}
          >
            <span className="hidden sm:block">{t("next")}</span>
            <ChevronLeftIcon />
          </Button>
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  )
}
