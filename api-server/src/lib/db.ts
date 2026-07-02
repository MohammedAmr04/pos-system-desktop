// Environment setup
import dotenv from 'dotenv';
dotenv.config();

import fs from 'fs';
import path from 'path';

// Custom logging setup
const logger = {
  info: (msg: string) => console.log(`[API] ${msg}`),
  error: (msg: string) => console.error(`[API ERR] ${msg}`),
  warn: (msg: string) => console.warn(`[API WARN] ${msg}`),
};

function ensureDirForDbUrl(url: string): void {
  const match = url.match(/^file:(.+)/);
  if (match) {
    const dir = path.dirname(match[1]);
    fs.mkdirSync(dir, { recursive: true });
  }
}

function resolveDbUrl(): string {
  const existing = process.env.DATABASE_URL;
  if (existing && !existing.startsWith('file:./')) {
    ensureDirForDbUrl(existing);
    return existing;
  }

  const appDataDir = process.env.APPDATA
    ? path.join(process.env.APPDATA, 'pos-app')
    : path.join(process.cwd(), 'data');

  fs.mkdirSync(appDataDir, { recursive: true });

  const dbPath = path.join(appDataDir, 'dev.db');
  return `file:${dbPath}`;
}

process.env.DATABASE_URL = resolveDbUrl();

import { PrismaClient } from '@prisma/client';

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined;
};

const prisma = globalForPrisma.prisma ?? new PrismaClient({
  log: [
    { emit: 'stdout', level: 'query' },
    { emit: 'stdout', level: 'info' },
    { emit: 'stdout', level: 'warn' },
    { emit: 'stdout', level: 'error' },
  ],
});

if (process.env.NODE_ENV !== 'production') {
  globalForPrisma.prisma = prisma;
}

async function applyPendingMigrations(): Promise<void> {
  const migrationsDir = path.join(process.cwd(), 'prisma', 'migrations');
  if (!fs.existsSync(migrationsDir)) {
    logger.info('No migrations directory found');
    return;
  }

  try {
    await prisma.$executeRawUnsafe(
      `CREATE TABLE IF NOT EXISTS _applied_migrations (id TEXT PRIMARY KEY, applied_at DATETIME DEFAULT CURRENT_TIMESTAMP)`
    );
  } catch (e) {
    logger.warn(`Failed to create applied_migrations table: ${e}`);
  }

  interface MigrationRow { id: string }
  const applied = new Set<string>();
  try {
    const rows = await prisma.$queryRawUnsafe('SELECT id FROM _applied_migrations') as MigrationRow[];
    for (const r of rows) applied.add(r.id);
    logger.info(`Loaded ${applied.size} applied migrations`);
  } catch (e) {
    logger.warn(`Failed to load applied migrations: ${e}`);
  }

  const migrationDirs = fs.readdirSync(migrationsDir, { withFileTypes: true })
    .filter(d => d.isDirectory())
    .map(d => d.name)
    .sort();

  logger.info(`Found ${migrationDirs.length} migration directories`);

  for (const dir of migrationDirs) {
    if (applied.has(dir)) {
      logger.info(`Skipping already applied migration: ${dir}`);
      continue;
    }

    const sqlFile = path.join(migrationsDir, dir, 'migration.sql');
    if (!fs.existsSync(sqlFile)) {
      logger.warn(`Migration file not found: ${sqlFile}`);
      continue;
    }

    try {
      const sql = fs.readFileSync(sqlFile, 'utf8');
      const stmts: string[] = [];
      let current = '';
      for (const line of sql.split('\n')) {
        const trimmed = line.trim();
        if (trimmed.startsWith('--') || trimmed.startsWith('#')) continue;
        current += line + '\n';
        if (trimmed.endsWith(';')) {
          stmts.push(current.trim());
          current = '';
        }
      }
      if (current.trim().length > 0) stmts.push(current.trim());

      let success = true;
      for (const stmt of stmts) {
        try {
          await prisma.$executeRawUnsafe(stmt);
        } catch (e) {
          logger.error(`Migration statement failed in '${dir}': ${e}`);
          success = false;
          break;
        }
      }

      if (success) {
        await prisma.$executeRawUnsafe('INSERT INTO _applied_migrations (id) VALUES (?)', dir);
        logger.info(`Successfully applied migration: ${dir}`);
      } else {
        logger.error(`Failed to apply migration: ${dir}`);
      }
    } catch (e) {
      logger.error(`Error processing migration ${dir}: ${e}`);
    }
  }
}

applyPendingMigrations();

export { prisma, logger };