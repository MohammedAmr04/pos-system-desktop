import express, { Request, Response } from 'express';
import { ThermalPrinter, PrinterTypes, CharacterSet } from 'node-thermal-printer';
import { logger } from '../lib/db';

const router = express.Router();

// ============================================================
// CONFIGURATION
// Set this to your Xprinter's Windows printer name.
// Find it in: Windows Settings → Printers & scanners
// Example: 'XP-58IIH', 'XP-80', 'Xprinter XP-80', etc.
// ============================================================
const PRINTER_NAME = process.env.PRINTER_NAME || 'Xprinter';

interface ReceiptItem {
  name: string;
  quantity: number;
  salePrice: number;
}

const LINE_WIDTH = 42;

// ---- PRINTER HELPERS ----

function createPrinter(): ThermalPrinter {
  return new ThermalPrinter({
    type: PrinterTypes.EPSON,
    interface: `printer:${PRINTER_NAME}`,
    width: LINE_WIDTH,
    characterSet: CharacterSet.PC858_EURO,
    removeSpecialCharacters: false,
    options: { timeout: 5000 },
  });
}

function formatDate(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleString('en-US', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
    hour12: false,
  });
}

function padCenter(text: string, width: number): string {
  const pad = Math.max(0, width - text.length);
  const left = Math.floor(pad / 2);
  return ' '.repeat(left) + text + ' '.repeat(width - text.length - left);
}

function padRight(text: string, width: number): string {
  const pad = Math.max(0, width - text.length);
  return text + ' '.repeat(pad);
}

function buildReceipt(invoice: Record<string, unknown>): ThermalPrinter {
  const printer = createPrinter();
  const items = (invoice.items || invoice.InvoiceDetail || []) as Array<Record<string, unknown>>;
  const totalAmount = (invoice.totalAmount || 0) as number;
  const discount = (invoice.discount || 0) as number;

  printer.alignCenter();
  printer.bold(true);
  printer.setTextSize(1, 2);
  printer.println('POS SYSTEM');
  printer.setTextNormal();
  printer.bold(false);
  printer.println('SALES RECEIPT');
  printer.drawLine('-');

  printer.alignLeft();
  printer.println(`Date: ${formatDate(invoice.createdAt as string)}`);
  printer.println(`Invoice #: ${((invoice.invoiceNumber || invoice.id) as string || '').toString().padStart(6, '0')}`);
  printer.drawLine('-');

  printer.bold(true);
  printer.tableCustom([
    { text: 'ITEM', align: 'LEFT', width: 0.5 },
    { text: 'QTY', align: 'CENTER', width: 0.15 },
    { text: 'PRICE', align: 'RIGHT', width: 0.35 },
  ]);
  printer.bold(false);
  printer.drawLine('-');

  for (const item of items) {
    const product = item.product as Record<string, unknown> | undefined;
    const name = (product?.name || item.name || 'Item') as string;
    const qty = (item.quantity || 1) as number;
    const price = (item.salePrice || item.price || 0) as number;
    const lineTotal = qty * price;

    printer.tableCustom([
      { text: name.substring(0, 20), align: 'LEFT', width: 0.5 },
      { text: qty.toString(), align: 'CENTER', width: 0.15 },
      { text: `$${lineTotal.toFixed(2)}`, align: 'RIGHT', width: 0.35 },
    ]);
  }

  printer.drawLine('-');

  if (discount > 0) {
    const subtotal = totalAmount + discount;
    printer.tableCustom([
      { text: 'SUBTOTAL', align: 'LEFT', width: 0.5 },
      { text: '', align: 'CENTER', width: 0.15 },
      { text: `$${subtotal.toFixed(2)}`, align: 'RIGHT', width: 0.35 },
    ]);
    printer.tableCustom([
      { text: 'DISCOUNT', align: 'LEFT', width: 0.5 },
      { text: '', align: 'CENTER', width: 0.15 },
      { text: `-$${discount.toFixed(2)}`, align: 'RIGHT', width: 0.35 },
    ]);
    printer.drawLine('-');
  }

  printer.bold(true);
  printer.setTextSize(1, 2);
  printer.tableCustom([
    { text: 'TOTAL', align: 'LEFT', width: 0.5 },
    { text: '', align: 'CENTER', width: 0.15 },
    { text: `$${totalAmount.toFixed(2)}`, align: 'RIGHT', width: 0.35 },
  ]);
  printer.setTextNormal();
  printer.bold(false);

  printer.alignCenter();
  printer.newLine();
  printer.println('THANK YOU!');
  printer.newLine();
  printer.newLine();
  printer.cut();

  return printer;
}

function buildBarcodeLabel(barcode: string, name?: string, price?: number, count = 1): ThermalPrinter {
  const printer = createPrinter();

  for (let i = 0; i < count; i++) {
    if (i > 0) {
      printer.newLine();
    }

    printer.alignCenter();

    if (name) {
      printer.bold(true);
      printer.setTextSize(1, 2);
      printer.println(name.substring(0, 20));
      printer.setTextNormal();
      printer.bold(false);
      printer.newLine();
    }

    printer.code128(barcode, {
      width: 'LARGE',
      height: 80,
      text: 1,
    });

    if (price !== undefined) {
      printer.newLine();
      printer.setTextSize(2, 3);
      printer.println(`$${price.toFixed(2)}`);
      printer.setTextNormal();
    }

    printer.newLine();
    printer.cut();
  }

  return printer;
}

// ---- ENDPOINTS ----

router.post('/print', async (req: Request, res: Response) => {
  try {
    const { invoice } = req.body as { invoice?: Record<string, unknown> };

    if (!invoice) {
      return res.status(400).json({ success: false, message: 'Missing invoice data' });
    }

    const printer = buildReceipt(invoice);
    await printer.execute({ waitForResponse: true });

    logger.info(`Receipt printed for invoice: ${invoice.id}`);
    res.json({ success: true, message: 'Receipt printed successfully' });
  } catch (error) {
    logger.error(`Print receipt failed: ${error}`);
    res.status(500).json({
      success: false,
      message: 'Print failed',
      detail: (error as Error).message,
    });
  }
});

router.post('/print-barcode', async (req: Request, res: Response) => {
  try {
    const { barcode, name, price, count } = req.body as {
      barcode?: string;
      name?: string;
      price?: number;
      count?: number;
    };

    if (!barcode) {
      return res.status(400).json({ success: false, message: 'Missing barcode' });
    }

    const printer = buildBarcodeLabel(barcode, name, price, count || 1);
    await printer.execute({ waitForResponse: true });

    logger.info(`Barcode label(s) printed for: ${barcode}`);
    res.json({ success: true, message: 'Barcode label(s) printed successfully' });
  } catch (error) {
    logger.error(`Barcode print failed: ${error}`);
    res.status(500).json({
      success: false,
      message: 'Barcode print failed',
      detail: (error as Error).message,
    });
  }
});

// Legacy: generate receipt text without printing
router.post('/receipt', async (req: Request, res: Response) => {
  try {
    const { items, total, discount, arabic } = req.body as {
      items?: ReceiptItem[];
      total?: number;
      discount?: number;
      arabic?: boolean;
    };

    if (!items || !Array.isArray(items) || items.length === 0) {
      return res.status(400).json({ error: 'No items provided' });
    }

    let receipt: string;
    if (arabic) {
      receipt = formatArabicReceipt(items, total || 0, discount || 0);
    } else {
      receipt = formatEnglishReceipt(items, total || 0, discount || 0);
    }

    logger.info(`Receipt text generated: ${items.length} items, total: ${total}`);
    res.json({ success: true, receipt });
  } catch (error) {
    logger.error(`Generate receipt text failed: ${error}`);
    res.status(500).json({ error: 'Failed to generate receipt' });
  }
});

// Legacy: generate barcode text without printing
router.post('/barcode', async (req: Request, res: Response) => {
  try {
    const { barcode, productName } = req.body as {
      barcode?: string;
      productName?: string;
    };

    if (!barcode) {
      return res.status(400).json({ error: 'Barcode is required' });
    }

    const label = `
      ${productName || ''}
      ${barcode}
    `;

    logger.info(`Barcode text generated: ${barcode}`);
    res.json({ success: true, label: label.trim() });
  } catch (error) {
    logger.error(`Generate barcode text failed: ${error}`);
    res.status(500).json({ error: 'Failed to generate barcode label' });
  }
});

function formatEnglishReceipt(items: ReceiptItem[], total: number, discount: number): string {
  const line = '='.repeat(40);
  let receipt = `\n${line}\n`;
  receipt += '     POINT OF SALE RECEIPT\n';
  receipt += `${line}\n\n`;
  receipt += `${'Item'.padEnd(20)} ${'Qty'.padStart(4)} ${'Price'.padStart(8)}\n`;
  receipt += '-'.repeat(40) + '\n';

  for (const item of items) {
    const lineTotal = item.salePrice * item.quantity;
    receipt += `${item.name.substring(0, 18).padEnd(20)} `;
    receipt += `${item.quantity.toString().padStart(4)} `;
    receipt += `${lineTotal.toFixed(2).padStart(8)}\n`;
  }

  receipt += '-'.repeat(40) + '\n';
  receipt += `${'Subtotal:'.padEnd(24)} ${total.toFixed(2).padStart(12)}\n`;

  if (discount > 0) {
    receipt += `${'Discount:'.padEnd(24)} ${discount.toFixed(2).padStart(12)}\n`;
    const finalTotal = Math.max(0, total - discount);
    receipt += `${'Total:'.padEnd(24)} ${finalTotal.toFixed(2).padStart(12)}\n`;
  }

  receipt += `\n${line}\n`;
  receipt += '       Thank you for your purchase!\n';
  receipt += `${line}\n`;
  return receipt;
}

function formatArabicReceipt(items: ReceiptItem[], total: number, discount: number): string {
  const line = '='.repeat(40);
  let receipt = `\n${line}\n`;
  receipt += '     إيصال المبيعات\n';
  receipt += `${line}\n\n`;
  receipt += `${'الكمية'.padStart(8)} ${'السعر'.padStart(10)} ${'الصنف'.padEnd(20)}\n`;
  receipt += '-'.repeat(40) + '\n';

  for (const item of items) {
    const lineTotal = item.salePrice * item.quantity;
    receipt += `${item.quantity.toString().padStart(8)} `;
    receipt += `${lineTotal.toFixed(2).padStart(10)} `;
    receipt += `${item.name.substring(0, 18).padEnd(20)}\n`;
  }

  receipt += '-'.repeat(40) + '\n';
  receipt += `${'المجموع:'.padStart(8)} ${total.toFixed(2).padStart(28)}\n`;

  if (discount > 0) {
    receipt += `${'الخصم:'.padStart(8)} ${discount.toFixed(2).padStart(28)}\n`;
    const finalTotal = Math.max(0, total - discount);
    receipt += `${'الإجمالي:'.padStart(8)} ${finalTotal.toFixed(2).padStart(28)}\n`;
  }

  receipt += `\n${line}\n`;
  receipt += '       شكراً لشرائك!\n';
  receipt += `${line}\n`;
  return receipt;
}

export default router;
