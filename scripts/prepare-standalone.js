const path = require('path');
const fs = require('fs');

const root = path.join(__dirname, '..');
const standalone = path.join(root, '.next', 'standalone');

if (!fs.existsSync(standalone)) {
  console.warn('Standalone directory not found, skipping prepare.');
  process.exit(0);
}

function copy(src, dest) {
  const srcPath = path.join(root, src);
  const destPath = path.join(standalone, dest || src);
  if (fs.existsSync(srcPath)) {
    fs.rmSync(destPath, { recursive: true, force: true });
    fs.cpSync(srcPath, destPath, { recursive: true, force: true });
    console.log(`  Copied ${src}`);
  } else {
    console.warn(`  Skipped ${src} (not found)`);
  }
}

copy('prisma');
copy('public');
copy('.next/static', '.next/static');
copy('node_modules/prisma', 'node_modules/prisma');
copy('node_modules/@prisma/engines', 'node_modules/@prisma/engines');
copy('node_modules/@prisma/engines-version', 'node_modules/@prisma/engines-version');
copy('node_modules/@prisma/debug', 'node_modules/@prisma/debug');
copy('node_modules/@prisma/fetch-engine', 'node_modules/@prisma/fetch-engine');
copy('node_modules/@prisma/get-platform', 'node_modules/@prisma/get-platform');
copy('scripts/startup.js', 'startup.js');
copy('printer-api');

console.log('Standalone preparation complete.');
