const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');

canvas.width = 800;
canvas.height = 600;

// Game State
const gameState = {
    selectedTile: null,
    turn: 'player',
    isProcessingTurn: false,
    gameOver: false,
    winner: null,
};

// --- Constants and Classes ---
const HEX_SIZE = 30;
const HEX_WIDTH = Math.sqrt(3) * HEX_SIZE;
const HEX_HEIGHT = 2 * HEX_SIZE;
const GRID_WIDTH = 10;
const GRID_HEIGHT = 10;
const START_ZONE_ROWS = 3;
const CONTROL_POINTS = [{q: 5, r: 2}, {q: 5, r: 8}];

const TILE_CLASSES = {
    PLAYER_TANK: { name: 'Tank', color: 'white', owner: 'player', damage: 25 },
    PLAYER_MAGE: { name: 'Mage', color: 'blue', owner: 'player', damage: 40 },
    PLAYER_HEALER: { name: 'Healer', color: 'green', owner: 'player', damage: 15 },
    PLAYER_DD: { name: 'DD', color: 'red', owner: 'player', damage: 34 },
    AI_DD: { name: 'DD', color: '#ff6666', owner: 'ai', damage: 34 },
};

class Tile {
    constructor(type, hex) {
        this.type = type;
        this.hex = hex;
        this.health = 100;
        this.maxHealth = 100;
        if (hex) {
            hex.tile = this;
        }
    }
    takeDamage(amount) { this.health -= amount; if (this.health <= 0) { this.health = 0; this.hex.tile = null; } }
}

const grid = new Map();

function Hex(q, r) {
    const isControlPoint = CONTROL_POINTS.some(p => p.q === q && p.r === r);
    return { q, r, tile: null, isControlPoint };
}

for (let r = 0; r < GRID_HEIGHT; r++) {
    for (let q = 0; q < GRID_WIDTH - Math.floor(r/2); q++) {
        const hex = Hex(q, r);
        grid.set(`${q},${r}`, hex);
    }
}

new Tile(TILE_CLASSES.PLAYER_TANK, grid.get('1,1'));
new Tile(TILE_CLASSES.PLAYER_MAGE, grid.get('2,2'));
new Tile(TILE_CLASSES.PLAYER_DD, grid.get('2,1'));
new Tile(TILE_CLASSES.AI_DD, grid.get('7,7'));
new Tile(TILE_CLASSES.AI_DD, grid.get('6,6'));

// --- AI and Game Logic ---
function checkWinConditions() {
    // Check for elimination victory
    const playerTiles = Array.from(grid.values()).filter(h => h.tile && h.tile.type.owner === 'player');
    const aiTiles = Array.from(grid.values()).filter(h => h.tile && h.tile.type.owner === 'ai');
    if (aiTiles.length === 0) { gameState.gameOver = true; gameState.winner = 'Player'; return; }
    if (playerTiles.length === 0) { gameState.gameOver = true; gameState.winner = 'AI'; return; }

    // Check for control point victory
    const controlPointHexes = CONTROL_POINTS.map(p => grid.get(`${p.q},${p.r}`)).filter(Boolean);
    const playerControlledPoints = controlPointHexes.filter(h => h.tile && h.tile.type.owner === 'player').length;
    const aiControlledPoints = controlPointHexes.filter(h => h.tile && h.tile.type.owner === 'ai').length;

    if (playerControlledPoints === controlPointHexes.length) { gameState.gameOver = true; gameState.winner = 'Player'; }
    if (aiControlledPoints === controlPointHexes.length) { gameState.gameOver = true; gameState.winner = 'AI'; }
}

function hexDistance(a,b){const aq=a.q,ar=a.r,bq=b.q,br=b.r;return(Math.abs(aq-bq)+Math.abs(ar-br)+Math.abs((-aq-ar)-(-bq-br)))/2}
function aiTurn() {
    gameState.isProcessingTurn = true;
    setTimeout(() => {
        const aiTiles = Array.from(grid.values()).filter(h => h.tile && h.tile.type.owner === 'ai').map(h => h.tile);
        if (aiTiles.length === 0) { gameState.isProcessingTurn = false; return; }
        const aiTile = aiTiles[Math.floor(Math.random() * aiTiles.length)];
        const neighbors = getNeighbors(aiTile.hex);
        const adjacentPlayerTiles = neighbors.filter(n => n.tile && n.tile.type.owner === 'player');
        if (adjacentPlayerTiles.length > 0) {
            adjacentPlayerTiles[0].tile.takeDamage(aiTile.type.damage);
        } else {
            // Logic to move towards player or control point
            const playerTiles = Array.from(grid.values()).filter(h => h.tile && h.tile.type.owner === 'player').map(h => h.tile);
            const unoccupiedControlPoints = CONTROL_POINTS.map(p=>grid.get(`${p.q},${p.r}`)).filter(h=>h&&!h.tile);
            let targets = playerTiles.map(t=>t.hex).concat(unoccupiedControlPoints);
            if(targets.length === 0) { gameState.isProcessingTurn = false; return; }
            let closestTarget = targets[0];
            let minDistance = hexDistance(aiTile.hex, closestTarget);
            for(let i=1; i<targets.length; i++) {
                const d = hexDistance(aiTile.hex, targets[i]);
                if(d < minDistance) { minDistance = d; closestTarget = targets[i]; }
            }
            let bestMoveHex = null, bestMoveDistance = minDistance;
            for (const neighbor of neighbors) {
                if (!neighbor.tile) {
                    const d = hexDistance(neighbor, closestTarget);
                    if (d < bestMoveDistance) { bestMoveDistance = d; bestMoveHex = neighbor; }
                }
            }
            if (bestMoveHex) { aiTile.hex.tile = null; bestMoveHex.tile = aiTile; aiTile.hex = bestMoveHex; }
        }
        checkWinConditions();
        gameState.turn = 'player';
        gameState.isProcessingTurn = false;
    }, 500);
}

// --- Coordinate Conversion and Drawing ---
function hexToPixel(h){return{x:HEX_SIZE*(Math.sqrt(3)*h.q+Math.sqrt(3)/2*h.r),y:HEX_SIZE*(3/2*h.r)}}
function pixelToHex(x,y){const cX=canvas.width/2-(GRID_WIDTH*HEX_WIDTH/4),cY=canvas.height/2-(GRID_HEIGHT*HEX_HEIGHT/4);const iX=x-cX,iY=y-cY;const caX=(iX/0.866+iY/0.5)/2,caY=(iY/0.5-iX/0.866)/2;const qf=(Math.sqrt(3)/3*caX-1/3*caY)/HEX_SIZE,rf=(2/3*caY)/HEX_SIZE;return hexRound(qf,rf)}
function hexRound(qf,rf){const sf=-qf-rf;let q=Math.round(qf),r=Math.round(rf),s=Math.round(sf);const qd=Math.abs(q-qf),rd=Math.abs(r-rf),sd=Math.abs(s-sf);if(qd>rd&&qd>sd)q=-r-s;else if(rd>sd)r=-q-s;return Hex(q,r)}
const axialDirections=[Hex(1,0),Hex(0,1),Hex(-1,1),Hex(-1,0),Hex(0,-1),Hex(1,-1)];
function getNeighbors(h){const n=[];for(let d of axialDirections){const nq=h.q+d.q,nr=h.r+d.r;if(grid.has(`${nq},${nr}`))n.push(grid.get(`${nq},${nr}`))}return n}
function getIsoPixel(h){let p=hexToPixel(h);let ip={x:(p.x-p.y)*0.866,y:(p.x+p.y)*0.5};ip.x+=canvas.width/2-(GRID_WIDTH*HEX_WIDTH/4);ip.y+=canvas.height/2-(GRID_HEIGHT*HEX_HEIGHT/4);return ip}
function drawHexShape(p){ctx.beginPath();for(let i=0;i<6;i++){const a=2*Math.PI/6*i;const xi=p.x+HEX_SIZE*Math.cos(a),yi=p.y+HEX_SIZE*Math.sin(a);i===0?ctx.moveTo(xi,yi):ctx.lineTo(xi,yi)}ctx.closePath()}
function drawHexAndTile(h,ip){if(h.isControlPoint){drawHexShape(ip);ctx.fillStyle="rgba(255,223,0,0.2)";ctx.fill()}if(h.r<START_ZONE_ROWS){drawHexShape(ip);ctx.fillStyle="rgba(0,100,200,0.1)";ctx.fill()}drawHexShape(ip);ctx.strokeStyle=h.isControlPoint?'gold':'white';ctx.lineWidth=h.isControlPoint?2:1;ctx.stroke();if(h.tile){const t=h.tile,hp=t.health/t.maxHealth;ctx.save();drawHexShape(ip);ctx.clip();ctx.fillStyle='#444';ctx.fill();ctx.fillStyle=t.type.color;const fh=HEX_HEIGHT*0.85*hp;ctx.fillRect(ip.x-HEX_SIZE,ip.y-HEX_SIZE/2+(HEX_HEIGHT*0.85-fh),HEX_WIDTH,fh);ctx.restore();ctx.strokeStyle=h.isControlPoint?'gold':'white';ctx.stroke();}}
function drawGrid(){const nh=gameState.selectedTile?getNeighbors(gameState.selectedTile.hex):[];grid.forEach(h=>{const ip=getIsoPixel(h);drawHexAndTile(h,ip);const isN=nh.includes(h);if(isN&&!h.tile){drawHexShape(ip);ctx.fillStyle="rgba(255,255,0,0.3)";ctx.fill()}else if(isN&&h.tile&&h.tile.type.owner==='ai'){drawHexShape(ip);ctx.fillStyle="rgba(255,0,0,0.3)";ctx.fill()}});if(gameState.selectedTile){const ip=getIsoPixel(gameState.selectedTile.hex);drawHexShape(ip);ctx.strokeStyle='yellow';ctx.lineWidth=3;ctx.stroke()}}

// --- Event Listener ---
canvas.addEventListener('click', (event) => {
    if (gameState.gameOver || gameState.turn !== 'player' || gameState.isProcessingTurn) return;
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left, y = event.clientY - rect.top;
    const clickedHex = grid.get(`${pixelToHex(x, y).q},${pixelToHex(x, y).r}`);
    if (clickedHex) {
        if (gameState.selectedTile) {
            const neighbors = getNeighbors(gameState.selectedTile.hex);
            if (neighbors.includes(clickedHex)) {
                if (!clickedHex.tile) {
                    const tile = gameState.selectedTile;
                    tile.hex.tile = null;
                    tile.hex = clickedHex;
                    clickedHex.tile = tile;
                } else if (clickedHex.tile.type.owner === 'ai') {
                    clickedHex.tile.takeDamage(gameState.selectedTile.type.damage);
                }
                gameState.selectedTile = null;
                checkWinConditions();
                if(!gameState.gameOver) { gameState.turn = 'ai'; aiTurn(); }
            } else { gameState.selectedTile = null; }
        } else if (clickedHex.tile && clickedHex.tile.type.owner === 'player') {
             gameState.selectedTile = clickedHex.tile;
        }
    }
});

// --- Game Loop ---
function gameLoop() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (gameState.gameOver) {
        ctx.fillStyle = 'black';
        ctx.globalAlpha = 0.7;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.globalAlpha = 1.0;
        ctx.fillStyle = 'white';
        ctx.font = '48px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(`${gameState.winner} Wins!`, canvas.width / 2, canvas.height / 2);
    } else {
        drawGrid();
    }
    requestAnimationFrame(gameLoop);
}
gameLoop();
