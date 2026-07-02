const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

const appDataDir = process.env.APPDATA
  ? path.join(process.env.APPDATA, 'pos-app')
  : path.join(__dirname, 'data');

fs.mkdirSync(appDataDir, { recursive: true });

const dbPath = path.join(appDataDir, 'dev.db');
process.env.DATABASE_URL = `file:${dbPath}`;
process.env.PORT = '3000';

// Start unified API server
const apiServerPath = path.join(__dirname, 'api-server', 'dist', 'server.js');
try {
  const apiProcess = spawn('node', [apiServerPath], {
    stdio: 'pipe',
    windowsHide: true,
    env: { ...process.env, API_PORT: '3001' },
  });
  apiProcess.stdout.on('data', (data) => process.stdout.write(`[API] ${data}`));
  apiProcess.stderr.on('data', (data) => process.stderr.write(`[API] ${data}`));
  apiProcess.on('error', (err) => console.warn('[API] Failed to start:', err.message));
  apiProcess.on('exit', (code) => console.warn(`[API] Exited with code ${code}`));
} catch (e) {
  console.warn('[API] Could not start:', e.message);
}

// Start Next.js main server
require('./pos-app/server.js');
