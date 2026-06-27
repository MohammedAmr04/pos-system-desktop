const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const appDataDir = process.env.APPDATA
  ? path.join(process.env.APPDATA, 'pos-app')
  : path.join(__dirname, 'data');

fs.mkdirSync(appDataDir, { recursive: true });

const dbPath = path.join(appDataDir, 'dev.db');
process.env.DATABASE_URL = `file:${dbPath}`;

const prismaCli = path.join(__dirname, 'node_modules', 'prisma', 'build', 'index.js');
if (fs.existsSync(prismaCli)) {
  try {
    execSync(`node "${prismaCli}" db push --skip-generate`, {
      cwd: __dirname,
      env: { ...process.env, DATABASE_URL: process.env.DATABASE_URL },
      stdio: 'pipe',
      timeout: 30000,
    });
    console.log('Database schema synchronized.');
  } catch (e) {
    console.error('Database migration skipped (non-fatal):', e.message);
  }
} else {
  console.warn('Prisma CLI not found, skipping auto-migration.');
}

require('./server.js');
