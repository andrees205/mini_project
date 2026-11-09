const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const BASE = process.env.BASE || 'http://localhost:5131';
const dirEvidencias = path.join(__dirname, '..', 'docs', 'evidencias');
const dirCapturas = path.join(dirEvidencias, 'capturas');

const escapar = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

function colorear(texto) {
  return escapar(texto)
    .split('\n')
    .map((l) => {
      if (/^PRUEBA|^LOG DEL SERVIDOR/.test(l)) return `<span class="t">${l}</span>`;
      if (/^### /.test(l)) return `<span class="h">${l}</span>`;
      if (/^HTTP\/1\.1 [23]/.test(l)) return `<span class="ok">${l}</span>`;
      if (/^HTTP\/1\.1 [45]/.test(l)) return `<span class="err">${l}</span>`;
      if (/ OK$/.test(l)) return `<span class="ok">${l}</span>`;
      if (/FALLA$/.test(l)) return `<span class="err">${l}</span>`;
      if (/^curl|^  -/.test(l)) return `<span class="cmd">${l}</span>`;
      return l;
    })
    .join('\n');
}

(async () => {
  fs.mkdirSync(dirCapturas, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 900, height: 400 }, deviceScaleFactor: 2 });

  const archivos = fs.readdirSync(dirEvidencias).filter((f) => f.endsWith('.txt')).sort();
  for (const archivo of archivos) {
    const texto = fs.readFileSync(path.join(dirEvidencias, archivo), 'utf8');
    await page.setContent(`<!doctype html><html><head><meta charset="utf-8"><style>
      body{margin:0;background:#1e1f24;font:13px/1.45 ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;color:#d6d8de}
      .bar{background:#2b2d34;padding:8px 14px;color:#9aa0ab;font-size:12px;border-bottom:1px solid #383b44}
      .bar b{display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:6px;background:#ff5f57}
      .bar b+b{background:#febc2e}.bar b+b+b{background:#28c840;margin-right:12px}
      pre{margin:0;padding:14px 18px;white-space:pre-wrap;word-break:break-word}
      .t{color:#fff;font-weight:700}.h{color:#8ab4f8}.ok{color:#5fd38d}.err{color:#ff7b72}.cmd{color:#e5c07b}
    </style></head><body><div class="bar"><b></b><b></b><b></b>${escapar(archivo)}</div>
    <pre>${colorear(texto)}</pre></body></html>`);
    const alto = await page.evaluate(() => document.body.scrollHeight);
    await page.setViewportSize({ width: 900, height: alto });
    await page.screenshot({ path: path.join(dirCapturas, archivo.replace(/\.txt$/, '.png')) });
    console.log('captura:', archivo.replace(/\.txt$/, '.png'));
  }

  try {
    await page.setViewportSize({ width: 1200, height: 900 });
    await page.goto(`${BASE}/swagger/index.html`, { waitUntil: 'networkidle', timeout: 8000 });
    await page.waitForSelector('.opblock', { timeout: 8000 });
    await page.screenshot({ path: path.join(dirCapturas, '00-swagger-ui.png'), fullPage: true });
    console.log('captura: 00-swagger-ui.png');
  } catch {
    console.log('(API no disponible: se omite la captura de Swagger UI)');
  }

  await browser.close();
})();
