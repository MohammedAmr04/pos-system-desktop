import express, { Request, Response, NextFunction } from 'express';
import { z } from 'zod';
import { prisma } from '../lib/db';
import { logger } from '../lib/db';
import { Product, CreateProductData, UpdateProductData } from '../types';

const router = express.Router();

// Validation schemas
const createProductSchema = z.object({
  name: z.string().min(1, 'Product name is required'),
  barcode: z.string().optional(),
  buyPrice: z.number().min(0, 'Buy price must be positive'),
  salePrice: z.number().min(0, 'Sale price must be positive'),
  stockQuantity: z.number().int().min(0, 'Stock quantity must be non-negative'),
  notes: z.string().optional(),
});

const updateProductSchema = createProductSchema.partial().extend({
  id: z.string().min(1, 'Product ID is required'),
});

// Helper function to generate unique barcode
async function generateUniqueBarcode(): Promise<string> {
  let barcode: string;
  let attempts = 0;
  do {
    barcode = Math.floor(Math.random() * 1e12).toString().padStart(12, '0');
    attempts++;
    if (attempts > 10) {
      throw new Error('Failed to generate unique barcode after multiple attempts');
    }
  } while (await prisma.product.findUnique({ where: { barcode } }));
  return barcode;
}

// GET /api/products
router.get('/', async (req: Request, res: Response) => {
  try {
    const products = await prisma.product.findMany({
      orderBy: { createdAt: 'desc' },
    });
    res.json(products);
  } catch (error) {
    logger.error(`Failed to fetch products: ${error}`);
    res.status(500).json({ error: 'Failed to fetch products' });
  }
});

// POST /api/products
router.post('/', async (req: Request, res: Response) => {
  try {
    const data = createProductSchema.parse(req.body) as CreateProductData;
    
    let barcode = data.barcode;
    if (!barcode) {
      barcode = await generateUniqueBarcode();
    } else {
      const existing = await prisma.product.findUnique({ where: { barcode } });
      if (existing) {
        return res.status(400).json({ error: 'Barcode already in use' });
      }
    }

    const product = await prisma.product.create({
      data: { ...data, barcode },
    });
    
    logger.info(`Created product: ${product.id} (${product.name})`);
    res.json(product);
  } catch (error) {
    if (error instanceof z.ZodError) {
      return res.status(400).json({ error: 'Invalid product data', details: error.errors });
    }
    logger.error(`Failed to create product: ${error}`);
    res.status(500).json({ error: 'Failed to create product' });
  }
});

// PUT /api/products/:id
router.put('/:id', async (req: Request, res: Response) => {
  try {
    const data = updateProductSchema.parse({
      ...req.body,
      id: req.params.id,
    }) as UpdateProductData;

    const product = await prisma.product.update({
      where: { id: data.id },
      data: {
        name: data.name,
        barcode: data.barcode,
        buyPrice: data.buyPrice,
        salePrice: data.salePrice,
        stockQuantity: data.stockQuantity,
        notes: data.notes,
      },
    });
    
    logger.info(`Updated product: ${product.id}`);
    res.json(product);
  } catch (error) {
    if (error instanceof z.ZodError) {
      return res.status(400).json({ error: 'Invalid product data', details: error.errors });
    }
    if (error && typeof error === 'object' && 'code' in error && error.code === 'P2025') {
      return res.status(404).json({ error: 'Product not found' });
    }
    logger.error(`Failed to update product ${req.params.id}: ${error}`);
    res.status(500).json({ error: 'Failed to update product' });
  }
});

// DELETE /api/products/:id
router.delete('/:id', async (req: Request, res: Response) => {
  try {
    await prisma.product.delete({ where: { id: req.params.id } });
    logger.info(`Deleted product: ${req.params.id}`);
    res.json({ success: true });
  } catch (error) {
    if (error && typeof error === 'object' && 'code' in error && error.code === 'P2025') {
      return res.status(404).json({ error: 'Product not found' });
    }
    logger.error(`Failed to delete product ${req.params.id}: ${error}`);
    res.status(500).json({ error: 'Failed to delete product' });
  }
});

export default router;