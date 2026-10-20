const { test, before, after } = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');

let server;
let base;

before(async () => {
  server = createApp();
  await new Promise((resolve) => server.listen(0, resolve));
  base = `http://localhost:${server.address().port}`;
});

after(() => server.close());

const json = (path, method, body) =>
  fetch(base + path, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

test('GET /health', async () => {
  const res = await fetch(base + '/health');
  assert.strictEqual(res.status, 200);
  assert.strictEqual((await res.json()).status, 'ok');
});

test('POST /echo returns the body', async () => {
  const res = await json('/echo', 'POST', { a: 1 });
  assert.deepStrictEqual(await res.json(), { a: 1 });
});

test('POST /echo rejects invalid JSON', async () => {
  const res = await fetch(base + '/echo', { method: 'POST', body: '{nope' });
  assert.strictEqual(res.status, 400);
});

test('items CRUD', async () => {
  let res = await json('/items', 'POST', { name: 'widget' });
  assert.strictEqual(res.status, 201);
  const item = await res.json();
  assert.strictEqual(item.name, 'widget');

  res = await fetch(`${base}/items/${item.id}`);
  assert.deepStrictEqual(await res.json(), item);

  res = await fetch(base + '/items');
  assert.ok((await res.json()).some((i) => i.id === item.id));

  res = await fetch(`${base}/items/${item.id}`, { method: 'DELETE' });
  assert.strictEqual(res.status, 204);

  res = await fetch(`${base}/items/${item.id}`);
  assert.strictEqual(res.status, 404);
});

test('POST /items validates name', async () => {
  const res = await json('/items', 'POST', {});
  assert.strictEqual(res.status, 400);
});

test('unknown route returns 404', async () => {
  const res = await fetch(base + '/nope');
  assert.strictEqual(res.status, 404);
});
