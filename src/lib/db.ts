import { PrismaClient } from '@prisma/client'
import path from 'path'
import fs from 'fs'

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

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined
}

export const prisma = globalForPrisma.prisma ?? new PrismaClient()

async function applyPendingMigrations(): Promise<void> {
  const migrationsDir = path.join(process.cwd(), 'prisma', 'migrations')
  if (!fs.existsSync(migrationsDir)) return

  try {
    await prisma.$executeRawUnsafe(
      `CREATE TABLE IF NOT EXISTS _applied_migrations (id TEXT PRIMARY KEY, applied_at DATETIME DEFAULT CURRENT_TIMESTAMP)`
    )
  } catch { return }

  interface MigrationRow { id: string }
  const applied = new Set<string>()
  try {
    const rows = await prisma.$queryRawUnsafe<MigrationRow[]>('SELECT id FROM _applied_migrations')
    for (const r of rows) applied.add(r.id)
  } catch { return }

  const dirs = fs.readdirSync(migrationsDir, { withFileTypes: true })
    .filter(d => d.isDirectory())
    .map(d => d.name)
    .sort()

  for (const dir of dirs) {
    if (applied.has(dir)) continue
    const sqlFile = path.join(migrationsDir, dir, 'migration.sql')
    if (!fs.existsSync(sqlFile)) continue

    const sql = fs.readFileSync(sqlFile, 'utf8')
    const stmts: string[] = []
    let current = ''
    for (const line of sql.split('\n')) {
      const trimmed = line.trim()
      if (trimmed.startsWith('--')) continue
      current += line + '\n'
      if (trimmed.endsWith(';')) {
        stmts.push(current.trim())
        current = ''
      }
    }
    if (current.trim().length > 0) stmts.push(current.trim())

    let ok = true
    for (const stmt of stmts) {
      try {
        await prisma.$executeRawUnsafe(stmt)
      } catch {
        ok = false
        break
      }
    }

    if (ok) {
      await prisma.$executeRawUnsafe('INSERT INTO _applied_migrations (id) VALUES (?)', dir)
      console.log(`Migration applied: ${dir}`)
    }
  }
}

applyPendingMigrations()

if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = prisma
