// routeMap.js — wwwroot/js/routeMap.js (Leaflet version)
// Vẽ polyline nối các điểm đã đi (theo thứ tự nhận từ Blazor)
// + markers xuất phát/kết thúc/trung gian
// + POI markers đỏ

var map = null;
var routePolyline = null;
var routePolylineOutline = null;   // viền trắng ở dưới để polyline nổi trên map
var routeMarkers = [];
var poiMarkers = [];

window.initRouteMap = function (lat, lng, zoom) {
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

    // Xóa layer cũ
    if (routePolylineOutline) { map.removeLayer(routePolylineOutline); routePolylineOutline = null; }
    if (routePolyline) { map.removeLayer(routePolyline); routePolyline = null; }
    routeMarkers.forEach(function (m) { map.removeLayer(m); }); routeMarkers = [];
    poiMarkers.forEach(function (m) { map.removeLayer(m); }); poiMarkers = [];

    console.log('🗺️ updateRouteMap — routePoints:', routePoints ? routePoints.length : 0,
        ', poiPoints:', poiPoints ? poiPoints.length : 0);

    var bounds = [];

    // POI markers — đỏ (vẽ trước, dưới polyline)
    if (poiPoints && poiPoints.length > 0) {
        poiPoints.forEach(function (poi) {
            if (!poi || typeof poi.lat !== 'number' || typeof poi.lng !== 'number') return;
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

    // ====================== TUYẾN ĐƯỜNG (polyline nổi bật) ======================
    if (routePoints && routePoints.length > 0) {
        // Filter điểm hợp lệ (tránh [0,0] hoặc NaN)
        var validPts = routePoints.filter(function (p) {
            return p && typeof p.lat === 'number' && typeof p.lng === 'number'
                && !isNaN(p.lat) && !isNaN(p.lng)
                && (p.lat !== 0 || p.lng !== 0);
        });

        if (validPts.length >= 2) {
            var path = validPts.map(function (p) { return [p.lat, p.lng]; });

            // Layer 1: VIỀN TRẮNG dày ở dưới — giúp polyline nổi trên nền map
            routePolylineOutline = L.polyline(path, {
                color: '#ffffff',
                weight: 9,
                opacity: 1,
                lineJoin: 'round',
                lineCap: 'round'
            }).addTo(map);

            // Layer 2: ĐƯỜNG XANH ĐẬM chính
            routePolyline = L.polyline(path, {
                color: '#1976D2',
                weight: 5,
                opacity: 1,
                lineJoin: 'round',
                lineCap: 'round'
            }).addTo(map);
        }

        // Markers từng điểm
        routePoints.forEach(function (pt, i) {
            if (!pt || typeof pt.lat !== 'number' || typeof pt.lng !== 'number') return;
            if (pt.lat === 0 && pt.lng === 0) return;

            var isFirst = i === 0;
            var isLast = i === routePoints.length - 1;

            var color, size, label, inner;
            if (isFirst) {
                color = '#4CAF50'; size = 20; label = '🟢 Xuất phát'; inner = '▶';
            } else if (isLast) {
                color = '#FF6F00'; size = 20; label = '🔴 Kết thúc'; inner = '■';
            } else {
                color = '#1976D2'; size = 10; label = 'Điểm ' + (i + 1); inner = '';
            }

            var icon = L.divIcon({
                className: '',
                html: '<div style="' +
                    'width:' + size + 'px;height:' + size + 'px;' +
                    'background:' + color + ';' +
                    'border:2px solid #fff;' +
                    'border-radius:50%;' +
                    'box-shadow:0 2px 4px rgba(0,0,0,.5);' +
                    'display:flex;align-items:center;justify-content:center;' +
                    'color:white;font-size:' + (size > 14 ? '10' : '0') + 'px;font-weight:bold;">' +
                    inner +
                    '</div>',
                iconSize: [size, size], iconAnchor: [size / 2, size / 2]
            });

            var m = L.marker([pt.lat, pt.lng], {
                icon: icon,
                zIndexOffset: (isFirst || isLast) ? 1000 : 500
            })
                .addTo(map)
                .bindPopup('<b>' + label + '</b>');
            routeMarkers.push(m);
            bounds.push([pt.lat, pt.lng]);
        });
    }

    if (bounds.length > 0) {
        map.fitBounds(bounds, { padding: [50, 50] });
    }
};

window.zoomRouteMap = function (lat, lng, zoom) {
    if (!map) return;
    map.setView([lat, lng], zoom || 17);
};