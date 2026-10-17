#!/usr/bin/env python3
"""Đưa ảnh chụp thật của nhà trường vào wwwroot/images/viu/.

Ảnh nguồn nằm ở ~/Documents — thư mục này KHÔNG bị script đụng tới, chỉ đọc.
Mỗi ảnh được đặt lại tên theo nội dung, resize về rộng tối đa 1600px, và sinh
hai bản WebP theo đúng quy ước của Html.Img():
    <tên>.jpg  /  <tên>.webp  (1600px)  /  <tên>@sm.webp  (640px)

Cách chạy:
    pip install Pillow
    python3 tools/import-images.py

Thêm ảnh mới: bỏ tệp vào ~/Documents rồi thêm một dòng vào bảng WANTED.
Tuyệt đối không dùng ảnh của cơ sở đào tạo khác — xem README mục 9.
"""
import io
import os
from PIL import Image

SRC = os.path.expanduser("~/Documents")
DST = os.path.expanduser("~/Downloads/VIUUniversity/wwwroot/images/viu")

# tên đích -> tệp nguồn trong ~/Documents
WANTED = {
    # ---- Cơ sở đào tạo ----
    "viu-aerial.jpg":        "VIU_2_1511100833.jpg",              # toàn cảnh cơ sở từ trên cao
    "viu-main-building.jpg": "truong-dai-hoc-cong-nghiep-viet-hung.jpg",
    "viu-highrise.jpg":      "images.jpeg",                      # toà nhà cao tầng
    "viu-sontay-gate.jpg":   "cong_cssontay.jpg",                 # cổng cơ sở Sơn Tây
    "viu-garden.jpg":        "khuonvien.jpg",                     # khuôn viên + bảng tên trường
    "viu-yard-sign.jpg":     "anh_san.jpg",                       # sân trường, bảng tên
    "viu-yard-trees.jpg":    "anh_san_tt.jpg",                    # sân trường góc cây xanh
    "viu-thachthat.jpg":     "cs_thachthat.jpg",                  # cơ sở Thạch Thất
    "viu-sports-field.jpg":  "anhviutrencao.jpg",                 # sân thể thao nhìn từ trên cao
    "viu-plaza.jpg":         "baner2.jpg",                        # sân trường, đài phun nước
    "viu-yard-court.jpg":    "banner3.jpg",                       # sân trường, bóng đá
    "viu-hall.jpg":          "moi_truong.jpg",                    # hội trường / rạp

    # ---- Sự kiện ----
    "viu-opening.jpg":       "khai_giang.jpg",                    # lễ khai giảng
    "viu-signing.jpg":       "ky_ket.jpg",                        # lễ ký kết hợp tác toàn diện
    "viu-partnership.jpg":   "hop_tac.jpg",                       # lễ ký kết trên sân khấu
    "viu-anniversary.jpg":   "ky_niem.jpg",                       # lễ kỷ niệm
    "viu-stage.jpg":         "van_nghe.webp",                     # chương trình trên sân khấu đỏ

    # ---- Sinh viên ----
    "viu-students-open-day.jpg": "sinhvientinhnguyen.jpg",        # tập thể trước bảng tên trường
    "viu-students-cheer.jpg":    "tuyen_sinh.jpg",                # đội mũ "Việt Hưng"
    "viu-students-ao-dai.jpg":   "plus2931-20230418015407-e.jpg", # sinh viên nữ áo dài
    "viu-students-campus.jpg":   "muathu_viu.jpg",                # sinh viên dưới gốc cây
    "viu-students-group.jpg":    "images (1).jpeg",                # nhóm sinh viên Bộ Công Thương
    "viu-football-cup.jpg":      "the_thao.jpg",                  # Cúp bóng đá trường
    "viu-clb-ngoai-khoa.jpg":    "anh_ngoai_khoa.jpg",            # CLB Ngoại khoa
    "viu-clb-vo.jpg":            "clb_vo.jpeg",                    # CLB Võ
    "viu-hoi-tnvn.jpg":          "hoitn.jpg",                     # Hội Thanh niên Việt

    # ---- Khoa ----
    "viu-khoa-cntt.jpg":    "khoa_cntt.jpg",
    "viu-khoa-dien.jpg":    "khoa_dien.jpeg",
    "viu-khoa-kinhte.jpg":  "khoa_kinhte.jpg",
    "viu-cntt.jpg":         "cntt.jpg",                          # sự kiện khoa CNTT
}

# Những tện nguồn này cố ý không dùng:
#   305-tb-bgd-24-*.jpg, aac16a91-*.jpeg  -> poster/giấy tờ chữ, không phải ảnh cơ sở
#   vhd-2026*.jpg, banner1.jpeg          -> ảnh đồ hoạ dạng banner, dùng ở Admin/Banner


def emit(src_path, out_name):
    im = Image.open(src_path).convert("RGB")

    # Giới hạn bản gốc ở 1600px. Nhiều ảnh trong ~/Documents chụp ở 5760px —
    # để nguyên làm trang nặng thêm vài MB mà không màn hình nào hiện nổi.
    im.thumbnail((1600, 1600 * 4), Image.LANCZOS)
    im.save(os.path.join(DST, out_name), "JPEG", quality=84,
            optimize=True, progressive=True)

    stem = os.path.join(DST, os.path.splitext(out_name)[0])
    for suffix, box, q in ((".webp", 1600, 80), ("@sm.webp", 640, 78)):
        c = im.copy()
        c.thumbnail((box, box * 4), Image.LANCZOS)
        c.save(stem + suffix, "WEBP", quality=q, method=5)

    return im.size, os.path.getsize(os.path.join(DST, out_name))


def main():
    os.makedirs(DST, exist_ok=True)
    for out_name, src_name in WANTED.items():
        src = os.path.join(SRC, src_name)
        if not os.path.exists(src):
            print("THIEU:", src_name)
            continue
        size, nbytes = emit(src, out_name)
        print(f"OK  {out_name:30s} {size[0]}x{size[1]}  {nbytes//1024}KB")
    print(f"\n{len(WANTED)} anh -> {DST}")


if __name__ == "__main__":
    main()