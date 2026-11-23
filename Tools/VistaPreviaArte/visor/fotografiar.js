// Renderiza las escenas exportadas con Chromium sin ventana:
//   node fotografiar.js <carpeta-visor> <salida.png> <escena> [ancho] [alto] [parámetros extra del visor, p. ej. "&pelusa=capas"]
const { chromium } = require('playwright');
const http = require('http');
const fs = require('fs');
const path = require('path');
const [raiz, salida, escena, ancho = '1600', alto = '900', extra = ''] = process.argv.slice(2);
const tipos = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.png': 'image/png' };
const servidor = http.createServer((req, res) => {
  const ruta = path.join(raiz, decodeURIComponent(req.url.split('?')[0]));
  fs.readFile(ruta, (err, data) => {
    if (err) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Content-Type': tipos[path.extname(ruta)] || 'application/octet-stream' });
    res.end(data);
  });
}).listen(0, '127.0.0.1', async () => {
  const puerto = servidor.address().port;
  const navegador = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const pagina = await navegador.newPage({ viewport: { width: +ancho, height: +alto } });
  pagina.on('console', m => { if (m.type() === 'error') console.error('consola:', m.text()); });
  pagina.on('pageerror', e => console.error('error:', e.message));
  await pagina.goto(`http://127.0.0.1:${puerto}/visor.html?escena=${escena}&w=${ancho}&h=${alto}${extra}`);
  await pagina.waitForFunction(() => window.__listo === true, null, { timeout: 240000 });
  await pagina.screenshot({ path: salida });
  await navegador.close();
  servidor.close();
  console.log('Captura: ' + salida);
});
