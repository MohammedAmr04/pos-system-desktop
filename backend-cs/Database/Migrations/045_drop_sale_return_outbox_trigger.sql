-- Sales returns now enqueue through SyncOutboxWriter with a full {saleReturn, details}
-- payload (mirroring invoices) so the detail lines travel with the header. The 042 trigger
-- only enqueued a payload-less header row, which central could not apply, so remove it to
-- avoid a duplicate empty operation.

DROP TRIGGER IF EXISTS "trg_sale_return_sync_insert";
