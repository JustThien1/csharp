// heatMap.js — wwwroot/js/heatMap.js

var heatMap = null;
var heatLayer = null;
var heatPoiMarkers = [];

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
};

window.updateHeatMap = function (heatPoints, poiPoints) {
    if (!heatMap || typeof L === 'undefined') {
        setTimeout(function () { window.updateHeatMap(heatPoints, poiPoints); }, 300);
        return;
    }

    // Xóa layer cũ
    if (heatLayer) { heatMap.removeLayer(heatLayer); heatLayer = null; }
    heatPoiMarkers.forEach(function (m) { heatMap.removeLayer(m); });
    heatPoiMarkers = [];

    // POI markers — đỏ
    var poiIcon = L.divIcon({
        className: '',
        html: '<div style="width:12px;height:12px;background:#E53935;border:2px solid #fff;border-radius:50%;box-shadow:0 2px 4px rgba(0,0,0,.5);"></div>',
        iconSize: [12, 12], iconAnchor: [6, 6]
    });
    if (poiPoints && poiPoints.length > 0) {
        poiPoints.forEach(function (poi) {
            var m = L.marker([poi.lat, poi.lng], { icon: poiIcon })
                .addTo(heatMap)
                .bindPopup('<b style="color:#E53935">🏛️ ' + poi.name + '</b>');
            heatPoiMarkers.push(m);
        });
    }

    // Heatmap circles — vẽ bằng circles có opacity theo weight
    if (heatPoints && heatPoints.length > 0) {
        var maxWeight = Math.max.apply(null, heatPoints.map(function (p) { return p.weight; }));

        heatPoints.forEach(function (pt) {
            var ratio = pt.weight / maxWeight;
            var radius = 200 + ratio * 600;  // 200m → 800m
            var opacity = 0.15 + ratio * 0.45;
            var color = ratio < 0.33 ? '#3B82F6' : ratio < 0.66 ? '#8B5CF6' : '#EF4444';

            var circle = L.circle([pt.lat, pt.lng], {
                radius: radius,
                color: 'transparent',
                fillColor: color,
                fillOpacity: opacity
            }).addTo(heatMap);
            heatPoiMarkers.push(circle);
        });

        // Fit bounds
        var bounds = heatPoints.map(function (p) { return [p.lat, p.lng]; });
        if (poiPoints && poiPoints.length > 0)
            poiPoints.forEach(function (p) { bounds.push([p.lat, p.lng]); });
        if (bounds.length > 0)
            heatMap.fitBounds(bounds, { padding: [40, 40] });
    }
};
