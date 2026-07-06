-- 1. إضافة عمود رقم الفاتورة للجدول
ALTER TABLE Invoice ADD COLUMN invoiceNumber INTEGER;

-- 2. تحديث الفواتير القديمة (لو موجودة) بحيث تأخذ أرقام متسلسلة تبدأ من 1
UPDATE Invoice 
SET invoiceNumber = (
    SELECT COUNT(*) 
    FROM Invoice AS i 
    WHERE i.createdAt <= Invoice.createdAt
);

-- 3. عمل Trigger سحري ليزيد الرقم تلقائياً (Auto Increment) مع كل فاتورة جديدة
CREATE TRIGGER IF NOT EXISTS auto_increment_invoice_number
AFTER INSERT ON Invoice
FOR EACH ROW
WHEN NEW.invoiceNumber IS NULL
BEGIN UPDATE Invoice SET invoiceNumber = COALESCE((SELECT MAX(invoiceNumber) FROM Invoice), 0) + 1 WHERE id = NEW.id; END;