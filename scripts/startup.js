const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

const appDataDir = process.env.APPDATA
  ? path.join(process.env.APPDATA, 'pos-app')
  : path.join(__dirname, 'data');

fs.mkdirSync(appDataDir, { recursive: true });

const dbPath = path.join(appDataDir, 'dev.db');
process.env.DATABASE_URL = `file:${dbPath}`;

const printerApiPath = path.join(__dirname, 'printer-api', 'server.js');
try {
  const printerProcess = spawn('node', [printerApiPath], {
    stdio: 'pipe',
    windowsHide: true,
  });
  printerProcess.stdout.on('data', (data) => process.stdout.write(`[Printer] ${data}`));
  printerProcess.stderr.on('data', (data) => process.stderr.write(`[Printer] ${data}`));
  printerProcess.on('error', (err) => console.warn('Printer API failed to start:', err.message));
  printerProcess.on('exit', (code) => console.warn(`Printer API exited with code ${code}`));
} catch (e) {
  console.warn('Could not start Printer API:', e.message);
}

require('./server.js');
