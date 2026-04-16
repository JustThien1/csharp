// routeMap.js — wwwroot/js/routeMap.js (Leaflet version)

var map = null;
var routePolyline = null;
var routeMarkers = [];
var poiMarkers = [];

window.initRouteMap = function (lat, lng, zoom) {
    // Nếu Leaflet chưa load → đợi
    if (typeof L === 'undefined') {
        setTimeout(function () { window.initRouteMap(lat, lng, zoom); }, 200);
        return;
    }
    var el = document.getElementById('routeMap');
    if (!el) {
        setTimeout(function () { window.initRouteMap(lat, lng, zoom); }, 200);
        return;
    }
    if (map) { map.remove(); map = null; }

    map = L.map('routeMap').setView([lat, lng], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);
};

window.updateRouteMap = function (routePoints, poiPoints) {
    if (!map || typeof L === 'undefined') {
        setTimeout(function () { window.updateRouteMap(routePoints, poiPoints); }, 300);
        return;
    }

    if (routePolyline) { map.removeLayer(routePolyline); routePolyline = null; }
    routeMarkers.forEach(function (m) { map.removeLayer(m); }); routeMarkers = [];
    poiMarkers.forEach(function (m) { map.removeLayer(m); }); poiMarkers = [];

    var bounds = [];

    // POI markers — đỏ
    if (poiPoints && poiPoints.length > 0) {
        poiPoints.forEach(function (poi) {
            var icon = L.divIcon({
                className: '',
                html: '<div style="width:13px;height:13px;background:#E53935;border:2px solid #fff;border-radius:50%;box-shadow:0 2px 4px rgba(0,0,0,.5);"></div>',
                iconSize: [13, 13], iconAnchor: [6, 6]
            });
            var m = L.marker([poi.lat, poi.lng], { icon: icon })
                .addTo(map)
                .bindPopup('<b style="color:#E53935">🏛️ ' + poi.name + '</b>');
            poiMarkers.push(m);
            bounds.push([poi.lat, poi.lng]);
        });
    }

    // Tuyến đường + markers
    if (routePoints && routePoints.length > 0) {
        var path = routePoints.map(function (p) { return [p.lat, p.lng]; });

        routePolyline = L.polyline(path, {
            color: '#1976D2', weight: 4, opacity: 0.85, lineJoin: 'round'
        }).addTo(map);

        routePoints.forEach(function (pt, i) {
            var isFirst = i === 0;
            var isLast = i === routePoints.length - 1;
            var color = (isFirst || isLast) ? '#FF6F00' : '#1976D2';
            var size = (isFirst || isLast) ? 16 : 9;
            var label = isFirst ? '🟢 Xuất phát' : isLast ? '🔴 Kết thúc' : 'Điểm ' + (i + 1);

            var icon = L.divIcon({
                className: '',
                html: '<div style="width:' + size + 'px;height:' + size + 'px;background:' + color + ';border:2px solid #fff;border-radius:50%;box-shadow:0 2px 4px rgba(0,0,0,.5);"></div>',
                iconSize: [size, size], iconAnchor: [size / 2, size / 2]
            });
            var m = L.marker([pt.lat, pt.lng], { icon: icon, zIndexOffset: isFirst || isLast ? 1000 : 0 })
                .addTo(map)
                .bindPopup('<b>' + label + '</b>');
            routeMarkers.push(m);
            bounds.push([pt.lat, pt.lng]);
        });
    }

    if (bounds.length > 0) {
        map.fitBounds(bounds, { padding: [40, 40] });
    }
};

window.zoomRouteMap = function (lat, lng, zoom) {
    if (!map) return;
    map.setView([lat, lng], zoom || 17);
};