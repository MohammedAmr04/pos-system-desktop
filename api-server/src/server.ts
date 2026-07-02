import express from 'express';
import cors from 'cors';
import { productsRouter, invoicesRouter, licenseRouter, printingRouter } from './routes';
import { prisma, logger } from './lib/db';

const app = express();
const PORT = parseInt(process.env.API_PORT || '3001', 10);

app.use(cors({ origin: true, credentials: true }));
app.use(express.json());

app.use('/api/products', productsRouter);
app.use('/api/invoices', invoicesRouter);
app.use('/api/license', licenseRouter);
app.use('/api/printing', printingRouter);

app.get('/health', (_req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.listen(PORT, () => {
  logger.info(`API server running on http://localhost:${PORT}`);
});

process.on('SIGTERM', async () => {
  logger.info('SIGTERM received, shutting down...');
  await prisma.$disconnect();
  process.exit(0);
});

process.on('SIGINT', async () => {
  logger.info('SIGINT received, shutting down...');
  await prisma.$disconnect();
  process.exit(0);
});

export default app;
