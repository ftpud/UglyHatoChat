const http = require('node:http');

function createApp() {
  const items = new Map();
  let nextId = 1;
  const startedAt = Date.now();

  function send(res, status, body) {
    const payload = body === undefined ? '' : JSON.stringify(body);
    res.writeHead(status, {
      'Content-Type': 'application/json',
      'Content-Length': Buffer.byteLength(payload),
    });
    res.end(payload);
  }

  function readJson(req) {
    return new Promise((resolve, reject) => {
      let data = '';
      req.on('data', (chunk) => {
        data += chunk;
        if (data.length > 1e6) {
          reject(Object.assign(new Error('Payload too large'), { status: 413 }));
          req.destroy();
        }
      });
      req.on('end', () => {
        if (!data) return resolve({});
        try {
          resolve(JSON.parse(data));
        } catch {
          reject(Object.assign(new Error('Invalid JSON'), { status: 400 }));
        }
      });
      req.on('error', reject);
    });
  }

  async function handle(req, res) {
    const url = new URL(req.url, 'http://localhost');
    const { pathname } = url;
    const method = req.method;

    if (pathname === '/health' && method === 'GET') {
      return send(res, 200, { status: 'ok', uptime: (Date.now() - startedAt) / 1000 });
    }

    if (pathname === '/echo' && method === 'POST') {
      return send(res, 200, await readJson(req));
    }

    if (pathname === '/items') {
      if (method === 'GET') return send(res, 200, [...items.values()]);
      if (method === 'POST') {
        const body = await readJson(req);
        if (typeof body.name !== 'string' || !body.name.trim()) {
          return send(res, 400, { error: 'name is required' });
        }
        const item = { id: nextId++, name: body.name.trim() };
        items.set(item.id, item);
        return send(res, 201, item);
      }
    }

    const match = pathname.match(/^\/items\/(\d+)$/);
    if (match) {
      const id = Number(match[1]);
      const item = items.get(id);
      if (!item) return send(res, 404, { error: 'not found' });
      if (method === 'GET') return send(res, 200, item);
      if (method === 'DELETE') {
        items.delete(id);
        return send(res, 204);
      }
    }

    return send(res, 404, { error: 'not found' });
  }

  return http.createServer((req, res) => {
    handle(req, res).catch((err) => {
      send(res, err.status || 500, { error: err.status ? err.message : 'internal error' });
    });
  });
}

module.exports = { createApp };
