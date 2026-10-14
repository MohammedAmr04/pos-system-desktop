-- Branch -> central push completion: the 042 outbox triggers enqueued Payment, Shift,
-- Expense and StockMovement rows with no payload, so the agent pushed empty operations and
-- the central ApplyReceived had nothing to persist. Re-declare them with real JSON payloads.
-- They also now fire only on a 'branch' node so applying pulled data centrally does not echo
-- received rows back out through the outbox.

DROP TRIGGER IF EXISTS "trg_payment_sync_insert";
DROP TRIGGER IF EXISTS "trg_shift_sync_insert";
DROP TRIGGER IF EXISTS "trg_expense_sync_insert";
DROP TRIGGER IF EXISTS "trg_stock_movement_sync_insert";

CREATE TRIGGER "trg_payment_sync_insert"
AFTER INSERT ON "Payment"
WHEN (SELECT "nodeRole" FROM "BranchRuntimeConfig" WHERE "id"=1) = 'branch'
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Payment',NEW.id,
        json_object('id',NEW.id,'amount',NEW.amount,'paymentMethod',NEW.paymentMethod,'date',NEW.date,
            'invoiceId',NEW.invoiceId,'clientId',NEW.clientId,'supplierId',NEW.supplierId,'reference',NEW.reference,
            'notes',NEW.notes,'createdBy',NEW.createdBy,'shiftId',NEW.shiftId,'createdAt',NEW.createdAt),
        COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER "trg_shift_sync_insert"
AFTER INSERT ON "Shift"
WHEN (SELECT "nodeRole" FROM "BranchRuntimeConfig" WHERE "id"=1) = 'branch'
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Shift',NEW.id,
        json_object('id',NEW.id,'number',NEW.number,'openedBy',NEW.openedBy,'openingCash',NEW.openingCash,
            'openedAt',NEW.openedAt,'closedAt',NEW.closedAt,'countedCash',NEW.countedCash,
            'expectedCash',NEW.expectedCash,'difference',NEW.difference,'notes',NEW.notes,'status',NEW.status),
        COALESCE(NEW.openedAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER "trg_expense_sync_insert"
AFTER INSERT ON "Expense"
WHEN (SELECT "nodeRole" FROM "BranchRuntimeConfig" WHERE "id"=1) = 'branch'
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Expense',NEW.id,
        json_object('id',NEW.id,'categoryId',NEW.categoryId,'amount',NEW.amount,'paymentMethod',NEW.paymentMethod,
            'date',NEW.date,'description',NEW.description,'reference',NEW.reference,'shiftId',NEW.shiftId,
            'createdBy',NEW.createdBy,'createdAt',NEW.createdAt),
        COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER "trg_stock_movement_sync_insert"
AFTER INSERT ON "StockMovement"
WHEN (SELECT "nodeRole" FROM "BranchRuntimeConfig" WHERE "id"=1) = 'branch'
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','StockMovement',NEW.id,
        json_object('id',NEW.id,'productId',NEW.productId,'quantity',NEW.quantity,'type',NEW.type,
            'referenceId',NEW.referenceId,'referenceNumber',NEW.referenceNumber,'createdAt',NEW.createdAt),
        COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;
