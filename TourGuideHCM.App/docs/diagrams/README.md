# TourGuideHCM — 19 sơ đồ UML/flow

Thư mục này chứa toàn bộ sơ đồ của 4 module **App / Saler / API / Admin** dưới dạng Mermaid, kèm công cụ xuất PNG nhanh gọn.

## Cấu trúc

```
docs/diagrams/
├── renderer.html         ← mở file này trong trình duyệt để xuất PNG
├── README.md
└── src/
    ├── 01_context.mmd
    ├── 02_erd.mmd
    ├── 03_usecase_tourist.mmd
    ├── 04_usecase_saler.mmd
    ├── 05_usecase_api.mmd
    ├── 06_usecase_admin.mmd
    ├── 07_sequence_map_geofence.mmd
    ├── 08_sequence_saler_poi.mmd
    ├── 09_sequence_api_auth_poi.mmd
    ├── 10_sequence_admin_approval.mmd
    ├── 11_activity_app.mmd
    ├── 12_activity_saler.mmd
    ├── 13_activity_api.mmd
    ├── 14_activity_admin.mmd
    ├── 15_class_app.mmd
    ├── 16_class_saler.mmd
    ├── 17_class_api.mmd
    ├── 18_class_admin.mmd
    └── 19_review_lifecycle.mmd
```

## Cách xuất 19 PNG nhanh nhất

1. Nhấp đúp mở **`renderer.html`** bằng Chrome / Edge / Firefox (cần internet để tải `mermaid.js` và `jszip` từ cdnjs).
2. Đợi vài giây — thanh đếm trên cùng sẽ chuyển thành `19 / 19 rendered`.
3. Bấm **⬇ Download All as PNG (ZIP)** → trình duyệt tải về `TourGuideHCM_diagrams.zip` gồm 19 PNG, độ phân giải 2× (sắc nét in ấn).
4. Giải nén ZIP vào cùng thư mục này là xong.

Tuỳ chọn khác:

- Bấm **⬇ Save 19 PNG riêng lẻ** để tải từng PNG vào `Downloads` (không đóng ZIP).
- Bấm nút **⬇ PNG** / **⬇ SVG** ở từng sơ đồ để lấy 1 file duy nhất.

## Cách xuất bằng CLI (nếu cần)

Nếu sau này bạn có máy cài được `mermaid-cli`:

```bash
npm i -g @mermaid-js/mermaid-cli
for f in src/*.mmd; do
  mmdc -i "$f" -o "${f%.mmd}.png" -s 2 -b white
done
```

Tất cả PNG sẽ được sinh ngay trong `src/` với cùng tên file.

## Ghi chú

- Các file `.mmd` là source gốc — sửa ở đây rồi mở lại `renderer.html` là thấy cập nhật ngay.
- Scale export mặc định là 2× trong `renderer.html`. Muốn to/nhỏ hơn, sửa hằng `PNG_SCALE` trong file đó.
