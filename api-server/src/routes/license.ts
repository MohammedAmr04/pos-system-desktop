import express, { Request, Response } from 'express';
import { prisma, logger } from '../lib/db';
import crypto from 'crypto';

const router = express.Router();

function getMachineId(): string {
  const parts: string[] = [];
  try {
    const os = require('os');
    parts.push(os.hostname());
    parts.push(os.platform());
    parts.push(os.arch());
    const cpus = os.cpus();
    if (cpus && cpus.length > 0) {
      parts.push(cpus[0].model);
    }
  } catch {
    parts.push('unknown');
  }
  const hash = crypto.createHash('sha256').update(parts.join('-')).digest('hex');
  return hash.substring(0, 16);
}

router.get('/', async (_req: Request, res: Response) => {
  try {
    const machineId = getMachineId();
    let settings = await prisma.settings.findUnique({ where: { machineId } });

    if (!settings) {
      settings = await prisma.settings.create({
        data: {
          machineId,
          activatedAt: new Date(),
          lastCheckedAt: new Date(),
          unlocked: false,
        },
      });
      return res.json({ status: 'first_boot', machineId });
    }

    if (!settings.unlocked) {
      return res.json({ status: 'locked', machineId });
    }

    const daysSinceActivation = Math.floor(
      (Date.now() - settings.activatedAt.getTime()) / (1000 * 60 * 60 * 24)
    );

    return res.json({
      status: 'ok',
      machineId,
      daysSinceActivation,
    });
  } catch (error) {
    logger.error(`License check failed: ${error}`);
    res.status(500).json({ error: 'License check failed' });
  }
});

router.post('/unlock', async (req: Request, res: Response) => {
  try {
    const { machineId } = req.body;
    if (!machineId) {
      return res.status(400).json({ error: 'Machine ID required' });
    }

    const settings = await prisma.settings.upsert({
      where: { machineId },
      update: { unlocked: true, lastCheckedAt: new Date() },
      create: {
        machineId,
        activatedAt: new Date(),
        lastCheckedAt: new Date(),
        unlocked: true,
      },
    });

    logger.info(`License unlocked for machine: ${machineId}`);
    res.json({ success: true, machineId: settings.machineId });
  } catch (error) {
    logger.error(`License unlock failed: ${error}`);
    res.status(500).json({ error: 'License unlock failed' });
  }
});

export default router;
