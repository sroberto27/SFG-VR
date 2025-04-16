// Import WebSocket library (install it via npm install ws)
const WebSocket = require('ws');

// Create a new WebSocket server running on port 8080
const wss = new WebSocket.Server({ port: 8080 });
console.log('🚀 Signaling server started on ws://localhost:8080');

// A simple object to store connected clients
let peers = {};
let masters = {};
let slaves = {};

// Fires when a new client connects
wss.on('connection', function connection(ws) {
    // Generate a unique ID for the connecting client
    const id = generateId();

    // Store the client's WebSocket connection in our peers list
    peers[id] = ws;
    console.log(`✅ New client connected with ID: ${id}`);

    // Send the ID back to the client so they know their identifier
    ws.send(JSON.stringify({ type: 'id', id: id }));

    // Fires whenever a client sends a message to the server
    ws.on('message', function incoming(message) {
        let data;

        // Try parsing the message as JSON
        try {
            data = JSON.parse(message);
        } catch (e) {
            console.error('❌ Invalid JSON received:', e);
            return;
        }

        // Handle registration message
        if (data.type === 'register') {
            if (data.role === 'master') {
                masters[id] = ws;
                logWithTime(`🟢 [MASTER] ${id} connected`);
                console.log(`🟢 Client ${id} registered as master`);

                // Notify all slaves about the new master
                Object.keys(slaves).forEach(slaveId => {
                    slaves[slaveId].send(JSON.stringify({
                        type: 'remote_connected',
                        id: id,
                        role: 'master'
                    }));
                });

            } else if (data.role === 'slave') {
                slaves[id] = ws;
                logWithTime(`🔵 [SLAVE] ${id} connected`);
                console.log(`🔵 Client ${id} registered as slave`);

                // Notify all masters about the new slave
                Object.keys(masters).forEach(masterId => {
                    masters[masterId].send(JSON.stringify({
                        type: 'remote_connected',
                        id: id,
                        role: 'slave'
                    }));
                });
            }

            return; // Registration handled, exit here
        }

        // Relay messages between peers (offer, answer, ice)
        const targetId = data.target;

        if (targetId && peers[targetId]) {
            const relayPayload = {
                from: id,                  // sender ID
                type: data.type,          // message type: offer, answer, ice, etc.
                payload: data.payload     // actual data being sent
            };

            peers[targetId].send(JSON.stringify(relayPayload));

            console.log(`➡️ Relayed ${data.type} from ${id} to ${targetId}`);
            console.log('📦 Payload content:\n', JSON.stringify(data.payload, null, 2)); // pretty print
        } else {
            console.warn(`⚠️ Target peer ${targetId} not found for ${data.type}`);
        }
    });

    // Fires when a client disconnects
    ws.on('close', () => {
        console.log(`❌ Client disconnected: ${id}`);

        // Clean up peers
        delete peers[id];
        delete masters[id];
        delete slaves[id];

        // Optionally, notify other peers about disconnection
        Object.keys(peers).forEach(peerId => {
            peers[peerId].send(JSON.stringify({
                type: 'peer_disconnected',
                id: id
            }));
        });
    });
});

// Generates a unique 9-character alphanumeric ID
function generateId() {
    return Math.random().toString(36).substr(2, 9);
}
function logWithTime(...args) {
    const now = new Date().toISOString().split("T")[1].split(".")[0]; // HH:MM:SS
    console.log(`[${now}]`, ...args);
}

