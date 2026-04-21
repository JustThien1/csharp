// wwwroot/js/qrcode.js
// Dùng thư viện qrcode.js từ CDN

window.renderQrCode = function (elementId, text, size) {
var el = document.getElementById(elementId);
if (!el) return;
el.innerHTML = ''; // Xóa QR cũ nếu có

if (typeof QRCode === 'undefined') {
console.error('QRCode library not loaded');
return; 
}

new QRCode(el, {
text: text,
width: size || 160,
height: size || 160,
colorDark: '#000000',
colorLight: '#ffffff',
correctLevel: QRCode.CorrectLevel.M
});
};

window.downloadQrCode = function (elementId, fileName) {
var el = document.getElementById(elementId);
if (!el) return;

var canvas = el.querySelector('canvas');
if (!canvas) {
// Fallback: dùng img tag
var img = el.querySelector('img');
if (img) {
var a = document.createElement('a');
a.href = img.src;
a.download = fileName || 'qrcode.png';
a.click();
}
return;
}

var a = document.createElement('a');
a.href = canvas.toDataURL('image/png');
a.download = fileName || 'qrcode.png';
a.click();
};
