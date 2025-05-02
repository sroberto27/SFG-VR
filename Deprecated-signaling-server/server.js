// Import WebSocket library (install it via npm install ws)
const WebSocket = require('ws');

// Create a new WebSocket server running on port 8080
const wss = new WebSocket.Server({ port: 8080 });
console.log('🚀 Signaling server started on ws://localhost:8080');

// Client tracking
let peers = {};      // id → ws
let masters = {};    // id → ws
let slaves = {};     // id → ws
let roles = {};      // id → 'master' | 'slave'

// Connection handler
wss.on('connection', function connection(ws) {
    const id = generateId();
    peers[id] = ws;
    logWithTime(`✅ New client connected: [${id}]`);

    // Send client its ID
    ws.send(JSON.stringify({ type: 'id', id }));

    // On message from client
    ws.on('message', function incoming(message) {
        let data;

        try {
            data = JSON.parse(message);
        } catch (e) {
            logWithTime(`❌ Invalid JSON from ${id}:`, message);
            return;
        }

        if (Object.keys(data).length === 0) {
            logWithTime(`⚠️ Ignored empty message from ${id}`);
            return;
        }

        // Registration message
        if (data.type === 'register') {
            const role = data.role;
            roles[id] = role;

            if (role === 'master') {
                masters[id] = ws;
                logWithTime(`🟢 [MASTER] ${id} registered`);
                // Notify all slaves
                Object.keys(slaves).forEach(slaveId => {
                    slaves[slaveId].send(JSON.stringify({
                        type: 'remote_connected',
                        id: id,
                        role: 'master'
                    }));
                });
            } else if (role === 'slave') {
                slaves[id] = ws;
                logWithTime(`🔵 [SLAVE] ${id} registered`);
                // Notify all masters
                Object.keys(masters).forEach(masterId => {
                    masters[masterId].send(JSON.stringify({
                        type: 'remote_connected',
                        id: id,
                        role: 'slave'
                    }));
                });
            } else {
                logWithTime(`⚠️ Unknown role: ${role} from ${id}`);
            }

            return;
        }

        // Relay message
        const { type, target, payload } = data;
        if (!type || !target || !payload) {
            logWithTime(`⚠️ Malformed message from ${id}`);
            logWithTime(`🔍 Message content:`, JSON.stringify(data, null, 2));
            return;
        }

        if (peers[target]) {
            const relay = {
                from: id,
                type,
                payload
            };
            peers[target].send(JSON.stringify(relay));

            const fromRole = roles[id] || 'unknown';
            const toRole = roles[target] || 'unknown';

            // Collapsed real-time log
            console.log(`🔄 ${type.toUpperCase()} | ${fromRole.toUpperCase()} [${id}] → ${toRole.toUpperCase()} [${target}]`);

            // Optional: brief payload description
            if (payload.pos && payload.rot) {
                console.log('   🧭 CameraPose sent');
            } else if (payload.width && payload.height) {
                console.log('   📐 Partition data sent');
            } else if (type === 'ice') {
                console.log('   ❄️ ICE candidate');
            } else if (type === 'offer') {
                console.log('   📡 SDP Offer');
            } else if (type === 'answer') {
                console.log('   📡 SDP Answer');
            }
        } else {
            logWithTime(`⚠️ Target peer '${target}' not found for message from ${id} (${type})`);
        }
    });

    // On disconnect
    ws.on('close', () => {
        logWithTime(`❌ Client disconnected: ${id}`);

        delete peers[id];
        delete masters[id];
        delete slaves[id];
        delete roles[id];

        // Notify all remaining peers
        Object.keys(peers).forEach(peerId => {
            peers[peerId].send(JSON.stringify({
                type: 'peer_disconnected',
                id: id
            }));
        });
    });
});

// Utility: generate unique 9-character ID
function generateId() {
    return Math.random().toString(36).substr(2, 9);
}

// Utility: timestamped logging
function logWithTime(...args) {
    const now = new Date().toISOString().split("T")[1].split(".")[0];
    console.log(`[${now}]`, ...args);
}
