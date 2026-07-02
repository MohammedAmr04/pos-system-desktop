import express, { Request, Response } from 'express';
import { Prisma } from '@prisma/client';
import { z } from 'zod';
import { prisma } from '../lib/db';
import { logger } from '../lib/db';
import { Invoice, CartItem } from '../types';

const router = express.Router();

// Validation schemas
const createInvoiceSchema = z.object({
  items: z.array(z.object({
    productId: z.string(),
    name: z.string(),
    buyPrice: z.number(),
    salePrice: z.number(),
    quantity: z.number().int().positive(),
    maxStock: z.number().int().positive(),
  })),
  discount: z.number().min(0).default(0),
});

// GET /api/invoices
router.get('/', async (req: Request, res: Response) => {
  try {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const invoices = await prisma.invoice.findMany({
      where: { createdAt: { gte: today } },
      include: {
        InvoiceDetail: {
          include: { product: true },
        },
      },
      orderBy: { createdAt: 'desc' },
    });
    
    res.json(invoices);
  } catch (error) {
    logger.error(`Failed to fetch invoices: ${error}`);
    res.status(500).json({ error: 'Failed to fetch invoices' });
  }
});

// GET /api/invoices/filter
router.get('/filter', async (req: Request, res: Response) => {
  try {
    const { from, to } = req.query;
    const where: Record<string, unknown> = {};

    if (from) {
      where.createdAt = { ...(where.createdAt as object || {}), gte: new Date(from as string) };
    }
    if (to) {
      const toDate = new Date(to as string);
      toDate.setHours(23, 59, 59, 999);
      where.createdAt = { ...(where.createdAt as object || {}), lte: toDate };
    }
    if (!from && !to) {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      where.createdAt = { gte: today };
    }

    const invoices = await prisma.invoice.findMany({
      where,
      include: {
        InvoiceDetail: {
          include: { product: true },
        },
      },
      orderBy: { createdAt: 'desc' },
    });
    
    res.json(invoices);
  } catch (error) {
    logger.error(`Failed to fetch filtered invoices: ${error}`);
    res.status(500).json({ error: 'Failed to fetch filtered invoices' });
  }
});

// POST /api/invoices
router.post('/', async (req: Request, res: Response) => {
  try {
    const data = createInvoiceSchema.parse(req.body);
    const { items, discount } = data;

    const subtotal = items.reduce((acc, item) => acc + (item.salePrice * item.quantity), 0);
    const totalAmount = Math.max(0, subtotal - discount);

    const invoice = await prisma.$transaction(async (tx: Prisma.TransactionClient) => {
      const newInvoice = await tx.invoice.create({
        data: {
          totalAmount,
          discount,
          InvoiceDetail: {
            create: items.map(item => ({
              productId: item.productId,
              quantity: item.quantity,
              buyPrice: item.buyPrice,
              salePrice: item.salePrice,
            })),
          },
        },
        include: { InvoiceDetail: true },
      });

      for (const item of items) {
        await tx.product.update({
          where: { id: item.productId },
          data: {
            stockQuantity: {
              decrement: item.quantity,
            },
          },
        });
      }

      return newInvoice;
    });

    logger.info(`Created invoice: ${invoice.id} (total: ${invoice.totalAmount})`);
    res.json(invoice);
  } catch (error) {
    if (error instanceof z.ZodError) {
      return res.status(400).json({ error: 'Invalid invoice data', details: error.errors });
    }
    logger.error(`Failed to create invoice: ${error}`);
    res.status(500).json({ error: 'Failed to create invoice' });
  }
});

export default router;