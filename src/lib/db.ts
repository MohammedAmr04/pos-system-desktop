import { PrismaClient } from '@prisma/client'
import path from 'path'
import fs from 'fs'
import { execSync } from 'child_process'

function ensureDirForDbUrl(url: string): void {
  const match = url.match(/^file:(.+)/)
  if (match) {
    const dir = path.dirname(match[1])
    fs.mkdirSync(dir, { recursive: true })
  }
}

function resolveDbUrl(): string {
  const existing = process.env.DATABASE_URL
  if (existing && !existing.startsWith('file:./')) {
    ensureDirForDbUrl(existing)
    return existing
  }

  const appDataDir = process.env.APPDATA
    ? path.join(process.env.APPDATA, 'pos-app')
    : path.join(process.cwd(), 'data')

  fs.mkdirSync(appDataDir, { recursive: true })

  const dbPath = path.join(appDataDir, 'dev.db')
  return `file:${dbPath}`
}

process.env.DATABASE_URL = resolveDbUrl()

const SCHEMA_DIRS = [
  process.cwd(),
  path.join(process.cwd(), '.next', 'standalone'),
]

function findPrismaCli(): string | null {
  for (const dir of SCHEMA_DIRS) {
    const cli = path.join(dir, 'node_modules', 'prisma', 'build', 'index.js')
    if (fs.existsSync(cli)) return cli
  }
  return null
}

function autoMigrate(): void {
  if (process.env.NEXT_PHASE === 'phase-production-build') return
  const cli = findPrismaCli()
  if (!cli) return

  const schemaDir = SCHEMA_DIRS.find(d => cli.startsWith(d)) || process.cwd()
  try {
    execSync(`node "${cli}" db push --skip-generate`, {
      cwd: schemaDir,
      env: { ...process.env, DATABASE_URL: process.env.DATABASE_URL },
      stdio: 'pipe',
      timeout: 30000,
    })
    console.log('Database schema synchronized.')
  } catch (e) {
    console.warn('Auto-migration skipped (non-fatal):', (e as Error).message)
  }
}

autoMigrate()

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined
}

export const prisma = globalForPrisma.prisma ?? new PrismaClient()

if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = prisma
