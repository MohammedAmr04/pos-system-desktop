ALTER TABLE Invoice ADD COLUMN invoiceNumber INTEGER;

UPDATE Invoice 
SET invoiceNumber = (
    SELECT COUNT(*) 
    FROM Invoice AS i 
    WHERE i.createdAt <= Invoice.createdAt
);

CREATE TRIGGER IF NOT EXISTS auto_increment_invoice_number
AFTER INSERT ON Invoice
FOR EACH ROW
WHEN NEW.invoiceNumber IS NULL
BEGIN UPDATE Invoice SET invoiceNumber = COALESCE((SELECT MAX(invoiceNumber) FROM Invoice), 0) + 1 WHERE id = NEW.id; END;