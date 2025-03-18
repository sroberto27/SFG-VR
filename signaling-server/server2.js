// =========================
// STEP 1: Node.js Signaling Server (WebSocket)
// =========================

// Create a folder for your signaling server
// Inside, create a file called server.js

const WebSocket = require('ws');

// Create a WebSocket server on port 8080
const wss = new WebSocket.Server({ port: 8080 });
console.log('Signaling server is running on ws://localhost:8080');

let peers = {}; // Stores connected peers

wss.on('connection', (ws) => {
    const id = generateId();
    peers[id] = ws;
    console.log(`Client connected: ${id}`);

    // Send back their ID
    ws.send(JSON.stringify({ type: 'id', id: id }));

    ws.on('message', (message) => {
        let data;
        try {
            data = JSON.parse(message);
        } catch (err) {
            console.error('Invalid JSON', err);
            return;
        }

        const targetId = data.target;
        if (targetId && peers[targetId]) {
            peers[targetId].send(JSON.stringify({
                from: id,
                type: data.type,
                payload: data.payload
            }));
        }
    });

    ws.on('close', () => {
        console.log(`Client disconnected: ${id}`);
        delete peers[id];
    });
});

function generateId() {
    return Math.random().toString(36).substr(2, 9);
}
