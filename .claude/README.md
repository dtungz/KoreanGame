# InBento: quy ước làm việc (bạn + Claude)

> Đọc file này trước khi làm bất cứ việc gì trong dự án. Mỗi khi đổi cách làm thì cập nhật lại file.

## 1. Cái gì nằm ở đâu

| Nơi | Chứa gì | Ai sửa |
|---|---|---|
| **Notion** | Mọi thứ **chưa chốt**: brainstorm, trend report, ý tưởng thô, ghi chú playtest, câu hỏi còn mở | Cả hai, thoải mái |
| **Google Sheet** | **Danh sách cơ chế** dạng bảng: mechanic, thẻ, hộp, topping, kế hoạch level. Mỗi dòng có trạng thái | Cả hai, theo luật ở mục 4 |
| **Dự án này (`.claude/`)** | Chỉ những thứ **đã chốt**: GDD chính + các file md quy ước | Chỉ sửa khi đã chốt |
| **Code (`Assets/_Project/`)** | Code theo cấu trúc ở [UnityProjectStructure.md](UnityProjectStructure.md) | |

**Luật vàng:** chưa chốt thì không vào repo. Ý tưởng chỉ được đưa vào [GDD.md](GDD.md) khi trên Sheet đã là `Chốt`.

### File trong `.claude/`

| File | Mục đích |
|---|---|
| `README.md` | File này: quy ước làm việc, link Notion và Sheet |
| `GDD.md` | GDD chính, **chỉ chứa nội dung đã chốt** |
| `UnityProjectStructure.md` | Mẫu cấu trúc thư mục và kiến trúc code Unity, dùng cho dự án này |
| `CodingConventions.md` | **Quy ước code bắt buộc**: DOTween luôn `Guard` + `KillTweens`, console không được có warning, luật UI màn chơi. Đọc trước khi sửa code |
| `game-trend-profile.md` | Hồ sơ ràng buộc (đội, engine, nền tảng, ngân sách) cho bộ skill game-trend-skills |
| `ChoDuaLenNotion/` (chỉ khi cần) | Hàng chờ tạm khi Notion không kết nối được. Đẩy lên Notion xong thì xóa |

## 2. Link

| Tên | Link |
|---|---|
| Notion: trang chính `[XGAME] - PerfectBento` | https://app.notion.com/p/410d428f8e5581328002e3aaae72107f |
| Notion: database Brainstorm | https://app.notion.com/p/2154ad462fad4a0b97327cd1237a0a9b |
| Notion: GDD (bản sao của `GDD.md`, cập nhật cùng lúc) | https://app.notion.com/p/410d428f8e5581579ccadd614d667966 |
| Sheet: tab **Questions** (câu cần bạn xác nhận) | cùng file Sheet bên dưới, tab đầu tiên |
| Google Sheet: `[XGAME] - PerfectBento` | https://docs.google.com/spreadsheets/d/1S7YWt0HrZJ6MHPS2-JPrQTU58BiwYm4WpKoz7BjhAGQ/edit |
| Figma: `InBento · UI màn chơi` (biến màu, kiểu chữ, component, các màn) | https://www.figma.com/design/KzgFtUzgEBq376Gmi1dXGl |

**Thứ tự làm từ đây:** thiết kế UI trên Figma → bạn chốt → gen asset từ Figma → mới bắt đầu code Unity.

> Tên `[XGAME] - PerfectBento` là tên tạm, sẽ đổi sau. Đổi tên không làm đổi link.
>
> **Ghi chú kết nối (2026-11-06):** Claude Code có connector Notion, Google Drive và Google Sheets, nên sửa thẳng được trên Sheet. Claude **không xem được ảnh chèn trong Sheet**: ảnh tham khảo thì để ở Notion > Tham khảo hoặc gửi trong chat.

## 3. Notion: cấu trúc đề xuất

```text
InBento (trang chính)
├── 💡 Brainstorm          Database: mỗi ý tưởng 1 trang
├── 📈 Trend Reports       Mỗi lần quét thị trường 1 trang (YYYY-MM-DD)
├── 🧪 Playtest            Mỗi buổi test 1 trang: ai test, build nào, kết quả, quyết định
├── ❓ Câu hỏi mở          Thứ cần quyết, kèm người quyết và hạn
└── 📚 Tham khảo           Game tham khảo, clip, ảnh, link bài phân tích
```

**Thuộc tính của database Brainstorm:**

| Thuộc tính | Kiểu | Giá trị |
|---|---|---|
| Tên | Title | |
| Trạng thái | Select | `Ý tưởng` · `Đang xem` · `Đưa lên Sheet` · `Bỏ` |
| Loại | Select | `Core loop` · `Thẻ/Hộp` · `Meta` · `Kiếm tiền` · `Art/Feel` · `LiveOps` |
| Điểm /35 | Number | Theo rubric của game-trend-skills |
| Người đề xuất | Select | `Bạn` · `Claude` |
| Ngày | Date | |
| Nguồn | URL / Text | Mọi số liệu thị trường phải có nguồn và tháng |

## 4. Google Sheet: các tab và cột

### Luật chung cho mọi tab
- Mỗi dòng có **ID** cố định (vd `CUT-01`, `FN-03`), không bao giờ đổi hay dùng lại ID.
- **Không xóa dòng.** Muốn bỏ thì đổi `Trạng thái` thành `Bỏ` và ghi lý do.
- Cột `Trạng thái`: `Ý tưởng` → `Đang thử` → `Chốt` (hoặc `Bỏ`).
- Cột `Sửa lần cuối` ghi `Bạn` hoặc `Claude` + ngày. Claude chỉ đổi trạng thái sang `Chốt` khi bạn đồng ý.
- Ô nào chưa biết thì để `?`, không đoán.

### Tab `Mechanics` (cơ chế lõi)
| ID | Tên | Mô tả 1 câu | Thao tác người chơi | Điều kiện thắng/thua | Phụ thuộc (ID) | Trạng thái | Ghi chú | Sửa lần cuối |
|---|---|---|---|---|---|---|---|---|

### Tab `Cards` (thẻ cắt + thẻ chức năng)
| ID | Tên | Nhóm (`Cắt` / `Chức năng` / `Đặc biệt`) | Hình/Tác dụng | Kích thước ô | Xoay được? | Xuất hiện từ level | Dùng làm booster? | Trạng thái | Ghi chú | Sửa lần cuối |
|---|---|---|---|---|---|---|---|---|---|---|

Gợi ý ID: `CUT-xx` cho thẻ cắt, `FN-xx` cho thẻ chức năng, `SP-xx` cho thẻ đặc biệt (Joker...).

### Tab `Boxes` (biến thể hộp)
| ID | Tên | Cách mở | Chứa gì | Thấy trước? | Dùng ở chế độ | Trạng thái | Ghi chú | Sửa lần cuối |
|---|---|---|---|---|---|---|---|---|

### Tab `Toppings`
| ID | Tên | Số ô | Ràng buộc trong khay | Art có chưa? | Trạng thái | Sửa lần cuối |
|---|---|---|---|---|---|---|

### Tab `LevelPlan` (kế hoạch nhịp độ)
| Chapter | Level | Thẻ mới giới thiệu (ID) | Thẻ có trong tay (ID) | Số nhát cắt | Độ khó mục tiêu (1–5) | Sinh tự động / Làm tay | Trạng thái |
|---|---|---|---|---|---|---|---|

### Tab `Changelog`
| Ngày | ID | Thay đổi | Lý do | Ai |
|---|---|---|---|---|

## 5. Luồng từ ý tưởng tới GDD

```text
Notion: Brainstorm (Ý tưởng)
   │  đáng làm → tạo dòng trên Sheet, Notion đổi sang "Đưa lên Sheet"
   ▼
Sheet: Ý tưởng → Đang thử (prototype / playtest, ghi kết quả vào Notion > Playtest)
   │  bạn đồng ý chốt
   ▼
Sheet: Chốt  →  Claude cập nhật GDD.md (ghi ID Sheet + ngày)  →  thêm dòng Changelog
```

- Bỏ một thứ đã chốt: đổi Sheet thành `Bỏ`, xóa khỏi GDD và ghi vào mục "Lịch sử thay đổi" của GDD.
- Claude không tự chốt. Claude đề xuất, bạn quyết.

## 6. Quy ước khi Claude làm việc
- Số liệu thị trường luôn kèm `(Nguồn, Tháng YYYY)`. Số tự tính ghi **ước tính**.
- Kết quả brainstorm hay trend report: đưa lên Notion, không lưu trong repo (nếu chưa đẩy được thì để tạm ở `ChoDuaLenNotion/`).
- Thêm hay sửa cơ chế: sửa trên Sheet, đúng tab và đúng cột ở trên.
- Code: theo [UnityProjectStructure.md](UnityProjectStructure.md), thay `<Game>` bằng `InBento`.
