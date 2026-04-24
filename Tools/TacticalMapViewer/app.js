const state = {
  fileIndex: new Map(),
  objectUrls: new Map(),
  spawnImages: new Map(),
  insertOverlayImages: new Map(),
  manifest: null,
  map: null,
  grid: null,
  selectedMapId: null,
  selectedGridId: null,
  activeInsertOverlays: new Set(),
  zoom: 1,
  hover: null,
  pinned: null,
  enteredCoordinates: null,
  coordinateOffset: null,
  image: null,
  isPanning: false,
  panStartX: 0,
  panStartY: 0,
  panOffsetStartX: 0,
  panOffsetStartY: 0,
  suppressClick: false,
  lastPointerX: null,
  lastPointerY: null,
  viewOffsetX: 0,
  viewOffsetY: 0,
};

const els = {
  folderInput: document.getElementById("folderInput"),
  loadStatus: document.getElementById("loadStatus"),
  mapSelect: document.getElementById("mapSelect"),
  gridSelect: document.getElementById("gridSelect"),
  showLabels: document.getElementById("showLabels"),
  showSpawns: document.getElementById("showSpawns"),
  showInsertOverlays: document.getElementById("showInsertOverlays"),
  showGrid: document.getElementById("showGrid"),
  meta: document.getElementById("meta"),
  cellInfo: document.getElementById("cellInfo"),
  areaInfo: document.getElementById("areaInfo"),
  insertInfo: document.getElementById("insertInfo"),
  insertLayerList: document.getElementById("insertLayerList"),
  spawnInfo: document.getElementById("spawnInfo"),
  tunnelInfo: document.getElementById("tunnelInfo"),
  roofingInfo: document.getElementById("roofingInfo"),
  entityInfo: document.getElementById("entityInfo"),
  canvas: document.getElementById("mapCanvas"),
  wrap: document.getElementById("canvasWrap"),
  zoomIn: document.getElementById("zoomIn"),
  zoomOut: document.getElementById("zoomOut"),
  resetView: document.getElementById("resetView"),
  coordRefX: document.getElementById("coordRefX"),
  coordRefY: document.getElementById("coordRefY"),
  applyCoords: document.getElementById("applyCoords"),
  clearCoords: document.getElementById("clearCoords"),
  coordStatus: document.getElementById("coordStatus"),
};

const ctx = els.canvas.getContext("2d", { alpha: false });

wireEvents();

function wireEvents() {
  els.folderInput.addEventListener("change", async ev => {
    const files = Array.from(ev.target.files ?? []);
    await loadExportFolder(files);
  });

  els.mapSelect.addEventListener("change", async () => {
    await loadMap(els.mapSelect.value);
  });

  els.gridSelect.addEventListener("change", async () => {
    state.selectedGridId = els.gridSelect.value;
    state.grid = getSelectedGrid();
    state.image = await loadGridImage(state.grid);
    await loadSpawnIcons(state.grid);
    await loadInsertOverlayImages(state.grid);
    rebuildInsertOverlayList();
    resetHover();
    clearCoordinateReference(false);
    updateMeta();
    fitCanvas();
    render();
  });

  els.showLabels.addEventListener("change", render);
  els.showSpawns.addEventListener("change", render);
  els.showInsertOverlays.addEventListener("change", render);
  els.showGrid.addEventListener("change", render);
  els.zoomIn.addEventListener("click", () => zoomBy(1.25));
  els.zoomOut.addEventListener("click", () => zoomBy(0.8));
  els.resetView.addEventListener("click", () => {
    fitCanvas();
    render();
  });
  els.applyCoords.addEventListener("click", applyCoordinateReference);
  els.clearCoords.addEventListener("click", clearCoordinateReference);

  els.canvas.addEventListener("mousemove", ev => {
    if (!state.grid) {
      return;
    }

    state.lastPointerX = ev.clientX;
    state.lastPointerY = ev.clientY;

    if (state.isPanning) {
      return;
    }

    state.hover = eventToIndices(ev);
    updateInfoPanels();
    render();
  });

  els.canvas.addEventListener("mouseleave", () => {
    state.hover = null;
    updateInfoPanels();
    render();
  });

  els.canvas.addEventListener("click", ev => {
    if (!state.grid) {
      return;
    }

    if (state.suppressClick) {
      state.suppressClick = false;
      return;
    }

    state.pinned = eventToIndices(ev);
    updateInfoPanels();
    render();
  });

  window.addEventListener("resize", () => {
    if (!state.grid) {
      return;
    }

    applyCanvasSize();
    clampViewOffset();
    render();
  });

  els.wrap.addEventListener("wheel", ev => {
    if (!state.grid) {
      return;
    }

    ev.preventDefault();
    zoomBy(ev.deltaY < 0 ? 1.15 : 1 / 1.15, ev.clientX, ev.clientY);
  }, { passive: false });

  els.wrap.addEventListener("mousedown", ev => {
    if (ev.button !== 0 || !state.grid) {
      return;
    }

    ev.preventDefault();
    state.isPanning = true;
    state.panStartX = ev.clientX;
    state.panStartY = ev.clientY;
    state.panOffsetStartX = state.viewOffsetX;
    state.panOffsetStartY = state.viewOffsetY;
    els.wrap.classList.add("panning");
  });

  window.addEventListener("mousemove", ev => {
    if (!state.isPanning) {
      return;
    }

    const dx = ev.clientX - state.panStartX;
    const dy = ev.clientY - state.panStartY;

    if (Math.abs(dx) > 3 || Math.abs(dy) > 3) {
      state.suppressClick = true;
    }

    state.viewOffsetX = state.panOffsetStartX + dx;
    state.viewOffsetY = state.panOffsetStartY + dy;
    clampViewOffset();
    render();
  });

  window.addEventListener("mouseup", () => {
    if (!state.isPanning) {
      return;
    }

    state.isPanning = false;
    els.wrap.classList.remove("panning");
  });
}

async function loadExportFolder(files) {
  clearObjectUrls();
  state.fileIndex = buildFileIndex(files);

  try {
    state.manifest = await readJsonFile("manifest.json");
    state.manifest.maps.sort((a, b) => a.name.localeCompare(b.name));
    setLoadStatus(`Loaded export with ${state.manifest.maps.length} maps.`);
    els.mapSelect.disabled = false;
    els.gridSelect.disabled = false;
    populateMapSelect();

    if (state.manifest.maps.length > 0) {
      await loadMap(state.manifest.maps[0].id);
    }
  } catch (error) {
    console.error(error);
    setLoadStatus(error.message, true);
    clearLoadedState();
  }
}

function buildFileIndex(files) {
  const index = new Map();

  for (const file of files) {
    const relative = normalizeRelativePath(file.webkitRelativePath || file.name);
    index.set(relative, file);
  }

  return index;
}

function normalizeRelativePath(path) {
  const normalized = path.replaceAll("\\", "/");
  const slash = normalized.indexOf("/");
  return slash >= 0 ? normalized.slice(slash + 1) : normalized;
}

function setLoadStatus(message, isError = false) {
  els.loadStatus.className = isError ? "info" : "info";
  if (!isError) {
    els.loadStatus.classList.remove("empty");
  }
  els.loadStatus.textContent = message;
}

function clearLoadedState() {
  state.manifest = null;
  state.map = null;
  state.grid = null;
  state.image = null;
  state.spawnImages.clear();
  state.insertOverlayImages.clear();
  state.activeInsertOverlays.clear();
  els.mapSelect.innerHTML = "";
  els.gridSelect.innerHTML = "";
  els.mapSelect.disabled = true;
  els.gridSelect.disabled = true;
  resetHover();
  updateMeta();
  render();
}

async function readJsonFile(path) {
  const file = state.fileIndex.get(path);
  if (!file) {
    throw new Error(`Missing required file: ${path}`);
  }

  const text = await file.text();
  return JSON.parse(text);
}

function populateMapSelect() {
  els.mapSelect.innerHTML = "";

  for (const map of state.manifest.maps) {
    const opt = document.createElement("option");
    opt.value = map.id;
    opt.textContent = map.name;
    els.mapSelect.appendChild(opt);
  }
}

async function loadMap(mapId) {
  if (!state.manifest) {
    return;
  }

  const mapEntry = state.manifest.maps.find(m => m.id === mapId);
  if (!mapEntry) {
    return;
  }

  state.map = await readJsonFile(mapEntry.file);
  state.selectedMapId = mapId;
  state.selectedGridId = state.map.grids[0]?.gridId ?? null;
  state.grid = getSelectedGrid();
  state.image = await loadGridImage(state.grid);
  await loadSpawnIcons(state.grid);
  await loadInsertOverlayImages(state.grid);
  rebuildInsertOverlayList();
  els.mapSelect.value = mapId;

  populateGridSelect();
  resetHover();
  clearCoordinateReference(false);
  updateMeta();
  fitCanvas();
  render();
}

function populateGridSelect() {
  els.gridSelect.innerHTML = "";

  for (const grid of state.map.grids) {
    const opt = document.createElement("option");
    opt.value = grid.gridId;
    opt.textContent = grid.gridId;
    els.gridSelect.appendChild(opt);
  }

  if (state.selectedGridId) {
    els.gridSelect.value = state.selectedGridId;
  }
}

function getSelectedGrid() {
  if (!state.map) {
    return null;
  }

  return state.map.grids.find(g => g.gridId === state.selectedGridId) ?? state.map.grids[0] ?? null;
}

function getInsertOverlayEntries(grid) {
  const entries = [];
  for (const cell of grid?.inserts ?? []) {
    for (const insert of cell.inserts ?? []) {
      for (let i = 0; i < (insert.variations?.length ?? 0); i++) {
        const variation = insert.variations[i];
        if (!variation.overlay) {
          continue;
        }

        entries.push({
          key: variation.overlay,
          label: `${insert.name || insert.prototypeId || "Insert"} @ ${cell.x},${cell.y}`,
          detail: `Variation ${i + 1} | ${formatInsertVariation(variation)}`,
          variation,
        });
      }
    }
  }

  entries.sort((a, b) => a.label.localeCompare(b.label) || a.detail.localeCompare(b.detail));
  return entries;
}

function rebuildInsertOverlayList() {
  const entries = getInsertOverlayEntries(state.grid);
  const previousActive = new Set(state.activeInsertOverlays);
  state.activeInsertOverlays.clear();

  if (entries.length === 0) {
    els.insertLayerList.className = "info empty";
    els.insertLayerList.textContent = "No insert overlays for this grid.";
    return;
  }

  els.insertLayerList.className = "info";
  els.insertLayerList.innerHTML = "";
  const list = document.createElement("div");
  list.className = "choiceList";

  for (const entry of entries) {
    const item = document.createElement("div");
    item.className = "choiceItem";

    if (previousActive.has(entry.key)) {
      state.activeInsertOverlays.add(entry.key);
    }

    const label = document.createElement("label");
    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = state.activeInsertOverlays.has(entry.key);
    checkbox.addEventListener("change", () => {
      if (checkbox.checked) {
        state.activeInsertOverlays.add(entry.key);
      } else {
        state.activeInsertOverlays.delete(entry.key);
      }
      render();
    });

    const text = document.createElement("span");
    text.textContent = entry.label;
    label.append(checkbox, text);

    const meta = document.createElement("div");
    meta.className = "choiceMeta";
    const spawnCount = countSpawns(entry.variation.spawns);
    meta.textContent = `${entry.detail} | overlay=${entry.variation.overlay ? "Yes" : "No"} | spawns=${spawnCount}`;

    item.append(label, meta);
    list.appendChild(item);
  }

  els.insertLayerList.appendChild(list);
}

function resetHover() {
  state.hover = null;
  state.pinned = null;
  updateInfoPanels();
}

function updateMeta() {
  if (!state.map || !state.grid) {
    els.meta.textContent = "";
    return;
  }

  const width = getGridWidth(state.grid);
  const height = getGridHeight(state.grid);
  els.meta.innerHTML = [
    row("Map", state.map.name),
    row("Map Key", state.map.id),
    row("Grid", state.grid.gridId),
    row("Bounds", `${state.grid.bounds.minX}, ${state.grid.bounds.minY} -> ${state.grid.bounds.maxX}, ${state.grid.bounds.maxY}`),
    row("Render Bounds", `${getGridBounds(state.grid).minX}, ${getGridBounds(state.grid).minY} -> ${getGridBounds(state.grid).maxX}, ${getGridBounds(state.grid).maxY}`),
    row("Extent", `${width} x ${height}`),
    row("Image", state.grid.image ? `${state.grid.image.width} x ${state.grid.image.height}` : "None"),
    row("Insert Tiles", (state.grid.inserts?.length ?? 0).toString()),
    row("Spawn Tiles", (state.grid.spawns?.length ?? 0).toString()),
    row("Tunnel Tiles", (state.grid.tunnels?.length ?? 0).toString()),
    row("Roofing Tiles", (state.grid.roofing?.length ?? 0).toString()),
    row("Entity Tiles", (state.grid.entities?.length ?? 0).toString()),
  ].join("");
}

function updateInfoPanels() {
  const indices = state.hover ?? state.pinned;
  if (!state.grid || !indices) {
    els.cellInfo.className = "info empty";
    els.cellInfo.textContent = "Move the cursor over the map.";
    els.areaInfo.className = "info empty";
    els.areaInfo.textContent = "No area selected.";
    els.insertInfo.className = "info empty";
    els.insertInfo.textContent = "No inserts on this tile.";
    els.spawnInfo.className = "info empty";
    els.spawnInfo.textContent = "No spawns on this tile.";
    els.tunnelInfo.className = "info empty";
    els.tunnelInfo.textContent = "No tunnels on this tile.";
    els.roofingInfo.className = "info empty";
    els.roofingInfo.textContent = "No roofing source on this tile.";
    els.entityInfo.className = "info empty";
    els.entityInfo.textContent = "No entities on this tile.";
    updateCoordinateStatus();
    return;
  }

  const areaCell = findAreaCell(state.grid, indices.x, indices.y);
  const areaLabel = findLabel(state.grid.labels, indices.x, indices.y);
  const insertCell = findInsertCell(state.grid, indices.x, indices.y);
  const spawnCell = findEffectiveSpawnCell(state.grid, indices.x, indices.y);
  const tunnelCell = findTunnelCell(state.grid, indices.x, indices.y);
  const roofingCell = findRoofingCell(state.grid, indices.x, indices.y);
  const entityCell = findEntityCell(state.grid, indices.x, indices.y);
  const calculatedCoordinates = getCalculatedCoordinates(indices);

  els.cellInfo.className = "info";
  els.cellInfo.innerHTML = [
    row("Indices", `${indices.x}, ${indices.y}`),
    row("Coordinates", calculatedCoordinates ? formatCoordinates(calculatedCoordinates) : "Not set"),
    row("Area Label", areaLabel?.t ?? "None"),
    row("Pinned", state.pinned ? "Yes" : "No"),
  ].join("");

  if (!areaCell) {
    els.areaInfo.className = "info empty";
    els.areaInfo.textContent = "No area at this cell.";
    updateInsertInfo(insertCell);
    updateSpawnInfo(spawnCell);
    updateTunnelInfo(tunnelCell);
    updateRoofingInfo(roofingCell);
    updateEntityInfo(entityCell);
    updateCoordinateStatus();
    return;
  }

  const areaId = state.grid.areaIds[areaCell.a] ?? null;
  const info = state.grid.areaInfo.find(a => a.id === areaId) ?? null;
  if (!info) {
    els.areaInfo.className = "info";
    els.areaInfo.innerHTML = row("Area Id", areaId ?? "Unknown");
    updateInsertInfo(insertCell);
    updateSpawnInfo(spawnCell);
    updateTunnelInfo(tunnelCell);
    updateRoofingInfo(roofingCell);
    updateEntityInfo(entityCell);
    updateCoordinateStatus();
    return;
  }

  const flags = areaFlags(info);
  const restrictions = areaRestrictions(info);
  els.areaInfo.className = "info";
  els.areaInfo.innerHTML = [
    row("Name", info.name || info.id),
    row("Id", info.id),
    row("Minimap Color", formatPackedColor(info.minimapColor)),
    row("Power Net", info.powerNet || "None"),
    row("Linked LZ", info.linkedLz || "None"),
    row("Z Level", info.zLevel),
    row("Weather Enabled", boolText(info.weatherEnabled)),
    row("Always Powered", boolText(info.alwaysPowered)),
    row("Avoid Bioscan", boolText(info.avoidBioscan)),
    row("No Tunnel", boolText(info.noTunnel)),
    row("Unweedable", boolText(info.unweedable)),
    row("Build Special", boolText(info.buildSpecial)),
    row("Resin Allowed", boolText(info.resinAllowed)),
    row("Resin Construction", boolText(info.resinConstructionAllowed)),
    row("Weed Killing", boolText(info.weedKilling)),
    row("Retrieve Objective", boolText(info.retrieveItemObjective)),
    row("Buildable Tiles", info.buildableTiles),
    row("Resin Count", info.resinConstructCount),
    row("Hijack Evac Area", boolText(info.hijackEvacuationArea)),
    row("Hijack Evac Type", info.hijackEvacuationType || "None"),
    row("Hijack Evac Weight", info.hijackEvacuationWeight),
    row("Tacmap Excluded", boolText(info.excludeFromTacMapRender)),
    flags.length > 0 ? `<div class="infoRow"><span class="infoKey">Flags:</span><div>${flags.map(flag => `<span class="tag">${escapeHtml(flag)}</span>`).join("")}</div></div>` : row("Flags", "None"),
    restrictions.length > 0 ? `<div class="infoRow"><span class="infoKey">Restrictions:</span><div>${restrictions.map(flag => `<span class="tag">${escapeHtml(flag)}</span>`).join("")}</div></div>` : row("Restrictions", "None"),
  ].join("");

  updateInsertInfo(insertCell);
  updateSpawnInfo(spawnCell);
  updateTunnelInfo(tunnelCell);
  updateRoofingInfo(roofingCell);
  updateEntityInfo(entityCell);
  updateCoordinateStatus();
}

function fitCanvas() {
  if (!state.grid) {
    return;
  }

  resizeCanvasViewport();

  const width = getCanvasBaseWidth(state.grid);
  const height = getCanvasBaseHeight(state.grid);
  const availableWidth = Math.max(1, els.canvas.width);
  const availableHeight = Math.max(1, els.canvas.height);
  const fitZoom = Math.min(availableWidth / width, availableHeight / height);

  state.zoom = Math.max(0.05, fitZoom || 1);
  const contentWidth = width * state.zoom;
  const contentHeight = height * state.zoom;
  state.viewOffsetX = Math.round((availableWidth - contentWidth) / 2);
  state.viewOffsetY = Math.round((availableHeight - contentHeight) / 2);
  clampViewOffset();
  applyCanvasSize();
}

function zoomBy(factor, clientX = null, clientY = null) {
  const previousZoom = state.zoom;
  const nextZoom = Math.max(0.05, Math.min(8, state.zoom * factor));
  if (nextZoom === previousZoom) {
    return;
  }

  const wrapRect = els.wrap.getBoundingClientRect();
  const anchorClientX = clientX ?? state.lastPointerX ?? (wrapRect.left + wrapRect.width / 2);
  const anchorClientY = clientY ?? state.lastPointerY ?? (wrapRect.top + wrapRect.height / 2);
  const anchorOffsetX = anchorClientX - wrapRect.left;
  const anchorOffsetY = anchorClientY - wrapRect.top;
  const worldX = (anchorOffsetX - state.viewOffsetX) / previousZoom;
  const worldY = (anchorOffsetY - state.viewOffsetY) / previousZoom;

  state.zoom = nextZoom;
  applyCanvasSize();
  state.viewOffsetX = anchorOffsetX - worldX * nextZoom;
  state.viewOffsetY = anchorOffsetY - worldY * nextZoom;
  clampViewOffset();
  render();
}

function applyCanvasSize() {
  if (!state.grid) {
    return;
  }

  resizeCanvasViewport();
}

function render() {
  if (!state.grid) {
    ctx.clearRect(0, 0, els.canvas.width, els.canvas.height);
    return;
  }

  const bounds = getGridBounds(state.grid);
  const width = getGridWidth(state.grid);
  const height = getGridHeight(state.grid);
  const tileScale = getTilePixelScale();
  const drawOffsetX = state.viewOffsetX;
  const drawOffsetY = state.viewOffsetY;

  ctx.fillStyle = "#0b0f14";
  ctx.fillRect(0, 0, els.canvas.width, els.canvas.height);

  if (state.grid.image && state.image) {
    ctx.drawImage(
      state.image,
      drawOffsetX,
      drawOffsetY,
      state.grid.image.width * state.zoom,
      state.grid.image.height * state.zoom);
  }

  if (els.showInsertOverlays.checked) {
    drawInsertOverlays(drawOffsetX, drawOffsetY);
  }

  if (els.showGrid.checked && tileScale >= 6) {
    ctx.strokeStyle = "rgba(255,255,255,0.08)";
    ctx.lineWidth = 1;

    for (let x = 0; x <= width; x++) {
      const px = Math.round(drawOffsetX + x * tileScale) + 0.5;
      ctx.beginPath();
      ctx.moveTo(px, drawOffsetY);
      ctx.lineTo(px, drawOffsetY + height * tileScale);
      ctx.stroke();
    }

    for (let y = 0; y <= height; y++) {
      const py = Math.round(drawOffsetY + y * tileScale) + 0.5;
      ctx.beginPath();
      ctx.moveTo(drawOffsetX, py);
      ctx.lineTo(drawOffsetX + width * tileScale, py);
      ctx.stroke();
    }
  }

  if (els.showLabels.checked && tileScale >= 8 && state.grid.labels) {
    ctx.font = `${Math.max(10, Math.floor(tileScale * 0.45))}px Consolas, monospace`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillStyle = "#ffffff";
    ctx.strokeStyle = "rgba(0, 0, 0, 0.75)";
    ctx.lineWidth = Math.max(2, tileScale * 0.10);

    for (const label of state.grid.labels) {
      const p = toDrawPosition(bounds, label.x, label.y);
      const cx = drawOffsetX + (p.x + 0.5) * tileScale;
      const cy = drawOffsetY + (p.y + 0.5) * tileScale;
      ctx.strokeText(label.t, cx, cy);
      ctx.fillText(label.t, cx, cy);
    }
  }

  if (els.showSpawns.checked && state.grid.spawns) {
    drawSpawnOverlay(bounds, tileScale, drawOffsetX, drawOffsetY);
  }

  const active = state.hover ?? state.pinned;
  if (active && isWithinBounds(bounds, active.x, active.y)) {
    const p = toDrawPosition(bounds, active.x, active.y);
    ctx.strokeStyle = state.hover ? "#4fb3ff" : "#7ee787";
    ctx.lineWidth = Math.max(2, tileScale * 0.08);
    ctx.strokeRect(drawOffsetX + p.x * tileScale, drawOffsetY + p.y * tileScale, tileScale, tileScale);
  }
}

function eventToIndices(ev) {
  const rect = els.canvas.getBoundingClientRect();
  const bounds = getGridBounds(state.grid);
  const worldX = (ev.clientX - rect.left - state.viewOffsetX) / state.zoom;
  const worldY = (ev.clientY - rect.top - state.viewOffsetY) / state.zoom;
  const pixelsPerTile = getPixelsPerTile();
  return {
    x: bounds.minX + Math.floor(worldX / pixelsPerTile),
    y: bounds.maxY - Math.floor(worldY / pixelsPerTile),
  };
}

function getGridWidth(grid) {
  const bounds = getGridBounds(grid);
  return Math.max(1, bounds.maxX - bounds.minX + 1);
}

function getGridHeight(grid) {
  const bounds = getGridBounds(grid);
  return Math.max(1, bounds.maxY - bounds.minY + 1);
}

function getGridBounds(grid) {
  return grid.image ? grid.renderBounds : grid.bounds;
}

function getPixelsPerTile() {
  return state.grid?.image?.pixelsPerTile ?? 1;
}

function getTilePixelScale() {
  return getPixelsPerTile() * state.zoom;
}

function getCanvasBaseWidth(grid) {
  return grid.image ? grid.image.width : getGridWidth(grid);
}

function getCanvasBaseHeight(grid) {
  return grid.image ? grid.image.height : getGridHeight(grid);
}

function toDrawPosition(bounds, x, y) {
  return {
    x: x - bounds.minX,
    y: bounds.maxY - y,
  };
}

function isWithinBounds(bounds, x, y) {
  return x >= bounds.minX && x <= bounds.maxX && y >= bounds.minY && y <= bounds.maxY;
}

function findAreaCell(grid, x, y) {
  return grid.areas?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function findLabel(labels, x, y) {
  return labels?.find(label => label.x === x && label.y === y) ?? null;
}

function findEntityCell(grid, x, y) {
  return grid.entities?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function findInsertCell(grid, x, y) {
  return grid.inserts?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function findSpawnCell(grid, x, y) {
  return grid.spawns?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function findTunnelCell(grid, x, y) {
  return grid.tunnels?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function findRoofingCell(grid, x, y) {
  return grid.roofing?.find(cell => cell.x === x && cell.y === y) ?? null;
}

function updateEntityInfo(entityCell) {
  if (!entityCell || !entityCell.entities || entityCell.entities.length === 0) {
    els.entityInfo.className = "info empty";
    els.entityInfo.textContent = "No entities on this tile.";
    return;
  }

  els.entityInfo.className = "info";
  els.entityInfo.innerHTML = entityCell.entities.map(entity =>
    row(entity.name, entity.prototypeId || "No prototype")).join("");
}

function updateInsertInfo(insertCell) {
  if (!insertCell || !insertCell.inserts || insertCell.inserts.length === 0) {
    els.insertInfo.className = "info empty";
    els.insertInfo.textContent = "No inserts on this tile.";
    return;
  }

  const parts = [];
  for (const insert of insertCell.inserts) {
    parts.push(row("Insert", insert.name || insert.prototypeId || "Unknown"));
    parts.push(row("Prototype", insert.prototypeId || "No prototype"));
    parts.push(row("Flags", insertFlags(insert).join(", ") || "None"));

    if (!insert.variations || insert.variations.length === 0) {
      parts.push(row("Variations", "None"));
    } else {
      for (let i = 0; i < insert.variations.length; i++) {
        const variation = insert.variations[i];
        parts.push(row(`Variation ${i + 1}`, formatInsertVariation(variation)));
        parts.push(row("Overlay", variation.overlay || "None"));
        parts.push(row("Variation Spawns", String(countSpawns(variation.spawns))));
      }
    }
  }

  els.insertInfo.className = "info";
  els.insertInfo.innerHTML = parts.join("");
}

function updateSpawnInfo(spawnCell) {
  if (!spawnCell || !spawnCell.spawns || spawnCell.spawns.length === 0) {
    els.spawnInfo.className = "info empty";
    els.spawnInfo.textContent = "No spawns on this tile.";
    return;
  }

  const parts = [];
  for (const spawn of spawnCell.spawns) {
    parts.push(row("Spawn", spawn.name || spawn.prototypeId || "Unknown"));
    parts.push(row("Kind", formatSpawnKind(spawn.kind)));
    parts.push(row("Origin", spawn.origin || "runtime"));
    parts.push(row("Prototype", spawn.prototypeId || "No prototype"));
    parts.push(row("Spawn Type", spawn.spawnType || "None"));
    parts.push(row("Job", spawn.jobId || "None"));
    parts.push(row("Intel Type", spawn.intelType || "None"));
    if (spawn.chance != null) {
      parts.push(row("Chance", formatPercent(spawn.chance)));
    }
    if (spawn.rareChance != null) {
      parts.push(row("Rare Chance", formatPercent(spawn.rareChance)));
    }
    if (spawn.minCount != null || spawn.maxCount != null) {
      parts.push(row("Count", formatRange(spawn.minCount, spawn.maxCount)));
    }
    if (spawn.quota != null) {
      parts.push(row("Quota", String(spawn.quota)));
    }
    if (spawn.ratio != null) {
      parts.push(row("Ratio", String(spawn.ratio)));
    }
    if (spawn.deleteAfterSpawn != null) {
      parts.push(row("Deletes", boolText(spawn.deleteAfterSpawn)));
    }
    if (spawn.targetId) {
      parts.push(row("Target", spawn.targetId));
    }
    if (spawn.groupId) {
      parts.push(row("Group", spawn.groupId));
    }
    if (spawn.spawnPath) {
      parts.push(row("Spawn Path", spawn.spawnPath));
    }
    if (spawn.targets && spawn.targets.length > 0) {
      parts.push(row("Targets", spawn.targets.join(", ")));
    }
    if (spawn.rareTargets && spawn.rareTargets.length > 0) {
      parts.push(row("Rare Targets", spawn.rareTargets.join(", ")));
    }
  }

  els.spawnInfo.className = "info";
  els.spawnInfo.innerHTML = parts.join("");
}

function updateTunnelInfo(tunnelCell) {
  if (!tunnelCell || !tunnelCell.tunnels || tunnelCell.tunnels.length === 0) {
    els.tunnelInfo.className = "info empty";
    els.tunnelInfo.textContent = "No tunnels on this tile.";
    return;
  }

  const parts = [];
  for (const tunnel of tunnelCell.tunnels) {
    parts.push(row("Tunnel", tunnel.name || tunnel.prototypeId || "Unknown"));
    parts.push(row("Prototype", tunnel.prototypeId || "No prototype"));
    parts.push(row("Max Mobs", tunnel.maxMobs));
    parts.push(row("Enter Delay", formatTunnelDelaySet(tunnel.smallXenoEnterDelay, tunnel.standardXenoEnterDelay, tunnel.largeXenoEnterDelay)));
    parts.push(row("Move Delay", formatTunnelDelaySet(tunnel.smallXenoMoveDelay, tunnel.standardXenoMoveDelay, tunnel.largeXenoMoveDelay)));
  }

  els.tunnelInfo.className = "info";
  els.tunnelInfo.innerHTML = parts.join("");
}

function updateRoofingInfo(roofingCell) {
  if (!roofingCell || !roofingCell.roofing || roofingCell.roofing.length === 0) {
    els.roofingInfo.className = "info empty";
    els.roofingInfo.textContent = "No roofing source on this tile.";
    return;
  }

  const parts = [];
  for (const roof of roofingCell.roofing) {
    parts.push(row("Source", roof.name || roof.prototypeId || "Unknown"));
    parts.push(row("Prototype", roof.prototypeId || "No prototype"));
    parts.push(row("Range", roof.range));
    parts.push(row("Allows", roofingFlags(roof).join(", ") || "None"));
    parts.push(row("Blocks", blockedRoofingFlags(roof).join(", ") || "None"));
  }

  els.roofingInfo.className = "info";
  els.roofingInfo.innerHTML = parts.join("");
}

function applyCoordinateReference() {
  const indices = state.pinned ?? state.hover;
  if (!state.grid || !indices) {
    updateCoordinateStatus("Select or pin a tile before applying a coordinate reference.");
    return;
  }

  const x = Number.parseInt(els.coordRefX.value, 10);
  const y = Number.parseInt(els.coordRefY.value, 10);
  if (!Number.isInteger(x) || !Number.isInteger(y)) {
    updateCoordinateStatus("Enter valid integer X and Y coordinates.");
    return;
  }

  state.enteredCoordinates = { x, y };
  state.coordinateOffset = {
    x: x - indices.x,
    y: y - indices.y,
  };

  updateCoordinateStatus();
  updateInfoPanels();
  render();
}

function clearCoordinateReference(clearInputs = true) {
  state.enteredCoordinates = null;
  state.coordinateOffset = null;

  if (clearInputs) {
    els.coordRefX.value = "";
    els.coordRefY.value = "";
  }

  updateCoordinateStatus();
}

function getCalculatedCoordinates(indices) {
  if (!state.coordinateOffset) {
    return null;
  }

  return {
    x: indices.x + state.coordinateOffset.x,
    y: indices.y + state.coordinateOffset.y,
  };
}

function updateCoordinateStatus(message) {
  if (message) {
    els.coordStatus.className = "info";
    els.coordStatus.textContent = message;
    return;
  }

  if (!state.coordinateOffset || !state.enteredCoordinates) {
    els.coordStatus.className = "info empty";
    els.coordStatus.textContent = "Select or pin a tile, enter its coordinates, then apply the reference.";
    return;
  }

  const indices = state.pinned ?? state.hover;
  const calculated = indices ? getCalculatedCoordinates(indices) : null;
  const rows = [row("Reference", formatCoordinates(state.enteredCoordinates))];

  if (indices) {
    rows.push(row("Current Tile", `${indices.x}, ${indices.y}`));
  }

  if (calculated) {
    rows.push(row("Calculated", formatCoordinates(calculated)));
  }

  els.coordStatus.className = "info";
  els.coordStatus.innerHTML = rows.join("");
}

function formatCoordinates(coordinates) {
  return `${coordinates.x}, ${coordinates.y}`;
}

async function loadGridImage(grid) {
  if (!grid?.image?.file) {
    return null;
  }

  return await loadImageFile(grid.image.file);
}

async function loadSpawnIcons(grid) {
  if (!grid) {
    return;
  }

  const iconPaths = new Set();
  for (const cell of getMergedSpawnCells(grid)) {
    for (const spawn of cell.spawns ?? []) {
      if (spawn.icon) {
        iconPaths.add(spawn.icon);
      }
    }
  }

  await Promise.all(Array.from(iconPaths, async path => {
    if (state.spawnImages.has(path)) {
      return;
    }

    try {
      const image = await loadImageFile(path);
      state.spawnImages.set(path, image);
    } catch (error) {
      console.error(error);
      state.spawnImages.set(path, null);
    }
  }));
}

async function loadInsertOverlayImages(grid) {
  state.insertOverlayImages.clear();

  if (!grid) {
    return;
  }

  const overlayPaths = new Set(getInsertOverlayEntries(grid).map(entry => entry.key));
  await Promise.all(Array.from(overlayPaths, async path => {
    try {
      const image = await loadImageFile(path);
      state.insertOverlayImages.set(path, image);
    } catch (error) {
      console.error(error);
      state.insertOverlayImages.set(path, null);
    }
  }));
}

async function loadImageFile(path) {
  const file = state.fileIndex.get(path);
  if (!file) {
    throw new Error(`Missing image file: ${path}`);
  }

  let url = state.objectUrls.get(path);
  if (!url) {
    url = URL.createObjectURL(file);
    state.objectUrls.set(path, url);
  }

  return await new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error(`Failed to load image: ${path}`));
    image.src = url;
  });
}

function clearObjectUrls() {
  for (const url of state.objectUrls.values()) {
    URL.revokeObjectURL(url);
  }

  state.objectUrls.clear();
  state.spawnImages.clear();
  state.insertOverlayImages.clear();
}

function resizeCanvasViewport() {
  const width = Math.max(320, els.wrap.clientWidth);
  const height = Math.max(240, els.wrap.clientHeight);

  if (els.canvas.width !== width) {
    els.canvas.width = width;
  }

  if (els.canvas.height !== height) {
    els.canvas.height = height;
  }
}

function clampViewOffset() {
  if (!state.grid) {
    return;
  }

  const viewportWidth = els.canvas.width;
  const viewportHeight = els.canvas.height;
  const contentWidth = getCanvasBaseWidth(state.grid) * state.zoom;
  const contentHeight = getCanvasBaseHeight(state.grid) * state.zoom;

  state.viewOffsetX = clampAxis(state.viewOffsetX, viewportWidth, contentWidth);
  state.viewOffsetY = clampAxis(state.viewOffsetY, viewportHeight, contentHeight);
}

function clampAxis(offset, viewportSize, contentSize) {
  const padding = Math.max(240, Math.round(viewportSize * 0.4));
  const minOffset = viewportSize - contentSize - padding;
  const maxOffset = padding;
  return Math.max(minOffset, Math.min(maxOffset, offset));
}

function areaFlags(info) {
  const flags = [];
  if (info.cas) flags.push("CAS");
  if (info.mortarFire) flags.push("Mortar Fire");
  if (info.mortarPlacement) flags.push("Mortar Placement");
  if (info.lasing) flags.push("Lasing");
  if (info.medevac) flags.push("Medevac");
  if (info.paradropping) flags.push("Paradropping");
  if (info.orbitalBombard) flags.push("OB");
  if (info.supplyDrop) flags.push("Supply Drop");
  if (info.fulton) flags.push("Fulton");
  if (info.landingZone) flags.push("Landing Zone");
  return flags;
}

function areaRestrictions(info) {
  const flags = [];
  if (info.noTunnel) flags.push("No Tunnel");
  if (info.unweedable) flags.push("Unweedable");
  if (!info.resinAllowed) flags.push("No Resin");
  if (!info.resinConstructionAllowed) flags.push("No Resin Construction");
  if (!info.weatherEnabled) flags.push("Weather Disabled");
  if (info.avoidBioscan) flags.push("Avoid Bioscan");
  if (info.weedKilling) flags.push("Weed Killing");
  if (info.buildSpecial) flags.push("Build Special");
  if (info.retrieveItemObjective) flags.push("Retrieve Objective");
  if (info.excludeFromTacMapRender) flags.push("Tacmap Hidden");
  return flags;
}

function insertFlags(insert) {
  const flags = [];
  if (insert.clearEntities) flags.push("Clear Entities");
  if (insert.clearDecals) flags.push("Clear Decals");
  if (insert.replaceAreas) flags.push("Replace Areas");
  return flags;
}

function roofingFlags(roof) {
  const flags = [];
  if (roof.cas) flags.push("CAS");
  if (roof.mortarFire) flags.push("Mortar Fire");
  if (roof.mortarPlacement) flags.push("Mortar Placement");
  if (roof.lasing) flags.push("Lasing");
  if (roof.medevac) flags.push("Medevac");
  if (roof.paradropping) flags.push("Paradropping");
  if (roof.orbitalBombard) flags.push("OB");
  if (roof.supplyDrop) flags.push("Supply Drop");
  if (roof.fulton) flags.push("Fulton");
  return flags;
}

function blockedRoofingFlags(roof) {
  const flags = [];
  if (!roof.cas) flags.push("CAS");
  if (!roof.mortarFire) flags.push("Mortar Fire");
  if (!roof.mortarPlacement) flags.push("Mortar Placement");
  if (!roof.lasing) flags.push("Lasing");
  if (!roof.medevac) flags.push("Medevac");
  if (!roof.paradropping) flags.push("Paradropping");
  if (!roof.orbitalBombard) flags.push("OB");
  if (!roof.supplyDrop) flags.push("Supply Drop");
  if (!roof.fulton) flags.push("Fulton");
  return flags;
}

function formatInsertVariation(variation) {
  const parts = [variation.spawn];

  if (variation.probability !== undefined) {
    parts.push(`p=${variation.probability}`);
  }

  if (variation.nightmareScenario) {
    parts.push(`scenario=${variation.nightmareScenario}`);
  }

  if ((variation.offsetX ?? 0) !== 0 || (variation.offsetY ?? 0) !== 0) {
    parts.push(`offset=${variation.offsetX},${variation.offsetY}`);
  }

  return parts.join(" | ");
}

function formatTunnelDelaySet(small, standard, large) {
  return `S=${small}s | M=${standard}s | L=${large}s`;
}

function drawSpawnOverlay(bounds, tileScale, drawOffsetX, drawOffsetY) {
  const baseSize = Math.max(12, Math.min(24, tileScale * 0.9));

  for (const cell of getMergedSpawnCells(state.grid)) {
    if (!cell.spawns || cell.spawns.length === 0) {
      continue;
    }

    const p = toDrawPosition(bounds, cell.x, cell.y);
    const centerX = drawOffsetX + (p.x + 0.5) * tileScale;
    const centerY = drawOffsetY + (p.y + 0.5) * tileScale;
    const iconSize = cell.spawns.length === 1 ? baseSize : Math.max(10, Math.floor(baseSize * 0.72));
    const spacing = Math.max(1, Math.floor(iconSize * 0.1));
    const totalWidth = cell.spawns.length * iconSize + Math.max(0, cell.spawns.length - 1) * spacing;
    let startX = centerX - totalWidth / 2;

    for (const spawn of cell.spawns) {
      const icon = spawn.icon ? state.spawnImages.get(spawn.icon) : null;
      const x = Math.round(startX);
      const y = Math.round(centerY - iconSize / 2);

      if (icon) {
        ctx.drawImage(icon, x, y, iconSize, iconSize);
      } else {
        ctx.fillStyle = "#ffca57";
        ctx.fillRect(x, y, iconSize, iconSize);
        ctx.strokeStyle = "#1b1b1b";
        ctx.lineWidth = 1;
        ctx.strokeRect(x, y, iconSize, iconSize);
      }

      startX += iconSize + spacing;
    }
  }
}

function drawInsertOverlays(drawOffsetX, drawOffsetY) {
  if (!state.grid?.image) {
    return;
  }

  for (const entry of getInsertOverlayEntries(state.grid)) {
    if (!state.activeInsertOverlays.has(entry.key)) {
      continue;
    }

    const image = state.insertOverlayImages.get(entry.key);
    if (!image) {
      continue;
    }

    ctx.drawImage(
      image,
      drawOffsetX,
      drawOffsetY,
      state.grid.image.width * state.zoom,
      state.grid.image.height * state.zoom);
  }
}

function getMergedSpawnCells(grid) {
  return mergeSpawnCells(grid?.spawns ?? [], getActiveInsertSpawnCells(grid));
}

function getActiveInsertSpawnCells(grid) {
  if (!grid) {
    return [];
  }

  const cells = [];
  for (const entry of getInsertOverlayEntries(grid)) {
    if (!state.activeInsertOverlays.has(entry.key)) {
      continue;
    }

    for (const cell of entry.variation.spawns ?? []) {
      cells.push(cell);
    }
  }

  return cells;
}

function mergeSpawnCells(primaryCells, secondaryCells) {
  const merged = new Map();

  for (const cell of [...primaryCells, ...secondaryCells]) {
    const key = `${cell.x},${cell.y}`;
    const existing = merged.get(key);
    if (existing) {
      existing.spawns.push(...(cell.spawns ?? []));
      continue;
    }

    merged.set(key, {
      x: cell.x,
      y: cell.y,
      spawns: [...(cell.spawns ?? [])],
    });
  }

  return Array.from(merged.values()).sort((a, b) => (a.x - b.x) || (a.y - b.y));
}

function findEffectiveSpawnCell(grid, x, y) {
  return getMergedSpawnCells(grid).find(cell => cell.x === x && cell.y === y) ?? null;
}

function countSpawns(cells) {
  let count = 0;
  for (const cell of cells ?? []) {
    count += cell.spawns?.length ?? 0;
  }

  return count;
}

function formatSpawnKind(kind) {
  switch (kind) {
    case "job":
      return "Job";
    case "latejoin":
      return "Latejoin";
    case "observer":
      return "Observer";
    case "xeno":
      return "Xeno";
    case "xenoLeader":
      return "Xeno Leader";
    case "intel":
      return "Intel";
    case "gunSpawner":
      return "Gun Spawner";
    case "randomSpawner":
      return "Random Spawner";
    case "uniqueRandomSpawner":
      return "Unique Random Spawner";
    case "conditionalSpawner":
      return "Conditional Spawner";
    case "entityTableSpawner":
      return "Entity Table Spawner";
    case "itemPoolSpawner":
      return "Item Pool Spawner";
    case "corpseSpawner":
      return "Corpse Spawner";
    case "aegisSpawner":
      return "Aegis Spawner";
    case "aegisCorpseSpawner":
      return "Aegis Corpse Spawner";
    case "proportionalSpawner":
      return "Proportional Spawner";
    case "gridSpawner":
      return "Grid Spawner";
    case "randomHumanoidSpawner":
      return "Random Humanoid Spawner";
    case "randomAnchoredSpawner":
      return "Random Anchored Spawner";
    case "ghostRoleSpawner":
      return "Ghost Role Spawner";
    case "communicationsTowerSpawner":
      return "Communications Tower Spawner";
    case "squadSpawner":
      return "Squad Spawner";
    case "deliverySpawner":
      return "Delivery Spawner";
    case "timedSpawner":
      return "Timed Spawner";
    case "randomCloneSpawner":
      return "Random Clone Spawner";
    case "randomPatronFigurineSpawner":
      return "Random Patron Figurine Spawner";
    default:
      return kind || "Unknown";
  }
}

function formatPercent(value) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    return "Unknown";
  }

  return `${(number * 100).toFixed(number === 0 || number === 1 ? 0 : 1)}%`;
}

function formatRange(min, max) {
  if (min == null && max == null) {
    return "Unknown";
  }
  if (min == null) {
    return String(max);
  }
  if (max == null) {
    return String(min);
  }
  if (min === max) {
    return String(min);
  }

  return `${min}-${max}`;
}

function formatPackedColor(value) {
  const color = Number(value) >>> 0;
  return `#${color.toString(16).padStart(8, "0")}`;
}

function boolText(value) {
  return value ? "Yes" : "No";
}

function row(key, value) {
  return `<div class="infoRow"><span class="infoKey">${escapeHtml(key)}:</span> ${escapeHtml(value)}</div>`;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
