"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { createInventoryCount } from "@/actions/operations.actions";
import { AccessDenied } from "@/components/common/access-denied";
import { TableBuilder } from "@/components/common/table-builder";
import { Button } from "@/components/ui/button";
import { DataPagination } from "@/components/common/data-pagination";
import { useAuth } from "@/components/common/auth-context";
import { useInventoryAdjustments } from "@/hooks/use-operations";
import { PERMISSIONS } from "@/lib/constants";
import { useRouter } from "@/i18n/navigation";
import { useAdjustmentColumns } from "./_components/use-adjustment-columns";

const PAGE_SIZE = 20;

export default function InventoryAdjustmentsPage() {
  const t = useTranslations("InventoryCounts");
  const { hasPermission } = useAuth();
  const router = useRouter();
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const { data, isPending } = useInventoryAdjustments(page);
  const columns = useAdjustmentColumns((id) => router.push(`/inventory-adjustments/${id}`));
  const create = async () => {
    setCreating(true);
    try {
      const session = await createInventoryCount();
      router.push(`/inventory-adjustments/${session.id}`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveFailed"));
    } finally {
      setCreating(false);
    }
  };
  if (!hasPermission(PERMISSIONS.INVENTORY_ADJUSTMENTS_VIEW))
    return <AccessDenied />;
  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-3xl font-bold">{t("title")}</h2>
          <p className="text-muted-foreground">{t("description")}</p>
        </div>
        {hasPermission(PERMISSIONS.INVENTORY_ADJUSTMENTS_CREATE) && (
          <Button disabled={creating} onClick={() => void create()}>
            <Plus className="ml-2 size-4" />
            {creating ? t("creating") : t("create")}
          </Button>
        )}
      </div>
      <TableBuilder
        columns={columns}
        data={data?.items ?? []}
        rowKey={(item) => item.id}
        loading={isPending}
        emptyMessage={t("empty")}
      />
      <DataPagination
        page={page}
        pageSize={PAGE_SIZE}
        total={data?.total ?? 0}
        onPageChange={setPage}
      />
    </div>
  );
}
