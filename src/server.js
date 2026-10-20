const { createApp } = require('./app');

const port = Number(process.env.PORT) || 8080;
const server = createApp();

server.listen(port, () => console.log(`Listening on http://localhost:${port}`));

process.on('SIGINT', () => server.close(() => process.exit(0)));
process.on('SIGTERM', () => server.close(() => process.exit(0)));
