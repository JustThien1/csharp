// heatMap.js — wwwroot/js/heatMap.js
// Heatmap gradient blob (leaflet.heat) + POI markers nổi bật

var heatMap = null;
var heatLayer = null;
var heatPoiMarkers = [];

// Load plugin leaflet.heat từ CDN nếu chưa có
function ensureHeatPluginLoaded(callback) {
    if (typeof L !== 'undefined' && typeof L.heatLayer === 'function') {
        callback();
        return;
    }
    var script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/leaflet.heat@0.2.0/dist/leaflet-heat.js';
    script.onload = callback;
    script.onerror = function () {
        console.error('❌ Không tải được leaflet.heat plugin');
    };
    document.head.appendChild(script);
}

window.initHeatMap = function (lat, lng, zoom) {
    if (typeof L === 'undefined') {
        setTimeout(function () { window.initHeatMap(lat, lng, zoom); }, 200);
        return;
    }
    var el = document.getElementById('heatMap');
    if (!el) {
        setTimeout(function () { window.initHeatMap(lat, lng, zoom); }, 200);
        return;
    }
    if (heatMap) { heatMap.remove(); heatMap = null; }

    heatMap = L.map('heatMap').setView([lat, lng], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(heatMap);

    ensureHeatPluginLoaded(function () {
        console.log('✅ leaflet.heat plugin sẵn sàng');
    });
};

window.updateHeatMap = function (heatPoints, poiPoints) {
    if (!heatMap || typeof L === 'undefined') {
        setTimeout(function () { window.updateHeatMap(heatPoints, poiPoints); }, 300);
        return;
    }
    ensureHeatPluginLoaded(function () {
        renderHeatmap(heatPoints, poiPoints);
    });
};

function renderHeatmap(heatPoints, poiPoints) {
    // Xóa layer cũ
    if (heatLayer) { heatMap.removeLayer(heatLayer); heatLayer = null; }
    heatPoiMarkers.forEach(function (m) { heatMap.removeLayer(m); });
    heatPoiMarkers = [];

    // ====================== HEATMAP BLOB (vẽ TRƯỚC để marker nằm ĐÈ LÊN) ======================
    if (heatPoints && heatPoints.length > 0) {
        var maxWeight = Math.max.apply(null, heatPoints.map(function (p) { return p.weight || 1; }));
        if (maxWeight < 1) maxWeight = 1;

        var heatData = heatPoints.map(function (pt) {
            var w = pt.weight || 1;
            var intensity = 0.3 + (w / maxWeight) * 0.7;
            return [pt.lat, pt.lng, intensity];
        });

        heatLayer = L.heatLayer(heatData, {
            radius: 35,
            blur: 25,
            maxZoom: 18,
            max: 1.0,
            minOpacity: 0.5,
            gradient: {
                0.0: 'blue',
                0.3: 'cyan',
                0.5: 'lime',
                0.7: 'yellow',
                1.0: 'red'
            }
        }).addTo(heatMap);
    }

    // ====================== POI MARKERS (nổi bật trên heatmap) ======================
    // Dùng marker trắng viền đen to + chấm đỏ giữa → luôn nhìn thấy dù heatmap màu gì
    var poiIcon = L.divIcon({
        className: 'poi-marker-wrapper',
        html:
            '<div style="' +
            'width:18px;height:18px;' +
            'background:white;' +
            'border:3px solid #111;' +
            'border-radius:50%;' +
            'display:flex;align-items:center;justify-content:center;' +
            'box-shadow:0 2px 6px rgba(0,0,0,.6);' +
            'position:relative;z-index:1000;">' +
            '<div style="width:6px;height:6px;background:#E53935;border-radius:50%;"></div>' +
            '</div>',
        iconSize: [18, 18],
        iconAnchor: [9, 9]
    });

    if (poiPoints && poiPoints.length > 0) {
        poiPoints.forEach(function (poi) {
            var m = L.marker([poi.lat, poi.lng], {
                icon: poiIcon,
                zIndexOffset: 1000     // đảm bảo marker luôn nổi trên heatmap
            })
                .addTo(heatMap)
                .bindPopup('<b style="color:#111;">📍 ' + poi.name + '</b>');
            heatPoiMarkers.push(m);
        });
    }

    // Fit bounds
    var bounds = [];
    if (heatPoints) heatPoints.forEach(function (p) { bounds.push([p.lat, p.lng]); });
    if (poiPoints) poiPoints.forEach(function (p) { bounds.push([p.lat, p.lng]); });
    if (bounds.length > 0) {
        heatMap.fitBounds(bounds, { padding: [60, 60], maxZoom: 15 });
    }
}