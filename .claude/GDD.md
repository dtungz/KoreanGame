# InBento: Game Design Document

## 0. Thông tin tài liệu

| Mục | Nội dung |
|---|---|
| Phiên bản | 1.2 (2026-11-06): cập nhật theo câu trả lời bổ sung + prototype |
| Tên làm việc | InBento. Tên tạm trên Sheet/Notion: `[XGAME] - PerfectBento`. Repo: `gcc-dtung/SuperBento`. Tên chính thức: 🟡 Q-20 (chưa chốt) |
| Prototype tham chiếu | Bản mới nhất: https://dtungz.github.io/PrototypeGame/cut-card/play.html (mã: `E:\DataGame\PrototypeGame\cut-card\play.html` + `levelkit.js`). Các bản cũ (v1–v4) đã bỏ, vẫn còn trong lịch sử git |
| Sheet cơ chế | https://docs.google.com/spreadsheets/d/1S7YWt0HrZJ6MHPS2-JPrQTU58BiwYm4WpKoz7BjhAGQ/edit (tab **Questions** chứa các câu cần xác nhận) |
| Notion | Trang dự án: https://app.notion.com/p/410d428f8e5581328002e3aaae72107f · Bản GDD trên Notion: https://app.notion.com/p/410d428f8e5581579ccadd614d667966 |

**Ký hiệu trạng thái** dùng trong tài liệu:
- ✅ **Đã chốt**: bạn đã quyết (trên Sheet là `Chốt`, hoặc câu hỏi đã trả lời).
- 🧪 **Đang thử**: đã quyết hướng, cần prototype / playtest trước khi chốt chi tiết.
- 🟡 **Chờ xác nhận**: mã `Q-xx` trỏ tới dòng tương ứng trong tab Questions.
- 💡 **Ý tưởng**: chưa có trong prototype, để tham khảo.

---

## 1. Tổng quan

| Mục | Nội dung | Trạng thái |
|---|---|---|
| Thể loại | Puzzle cắt và xếp: cắt cơm trên lưới rồi ráp kín hộp cơm | ✅ |
| Một câu (high concept) | *"Cắt khối cơm bằng những lá thẻ in sẵn đường dao, rồi ráp lại cho vừa khít hộp cơm."* | ✅ Q-21 |
| Điểm khác biệt | Cắt bằng **thẻ in sẵn đường cắt** (thẳng, chữ L, khuôn, chéo), không dùng nút cắt hay dao đặt sẵn. Mỗi màn là một bài **biến hình**: cơm hình A thành khay hình B | ✅ |
| Chủ đề | Hộp cơm văn phòng (hộp sắt) với món cơm nhà: cơm, trứng, thịt, xúc xích… và có thể thêm nhiều món khác. Thay cho bento Nhật | ✅ Q-24 |
| Nền tảng | Android trước, kèm bản web để test | ✅ Q-17 |
| Đối tượng | Người chơi puzzle thư giãn, 18–45 tuổi, toàn cầu | ✅ Q-18 |
| Mô hình kinh doanh | Hybrid-casual: quảng cáo + IAP gỡ quảng cáo | ✅ Q-16 |
| Đội | 1 solo dev làm cả code, design và art | ✅ Q-22 |
| Engine | Unity 6, cấu trúc dự án theo [UnityProjectStructure.md](UnityProjectStructure.md) | ✅ |

**Bối cảnh thị trường** (chi tiết ở Notion > Trend Reports):
- Puzzle chiếm hơn 44% doanh thu casual mobile trong H1 2026 *(AppMagic, H1 2026)*.
- Block puzzle rất đông: hơn 2.000 game mới trong H1 2026 *(AppMagic, H1 2026)*.
- Ngách "cắt khối" gần như trống: đối thủ gần nhất là Block Slice Puzzle, khoảng 100 lượt cài *(AppGoblin, 11/2026, ước tính)*.

Vì vậy điểm khác biệt phải **nhìn thấy được trong clip 3 giây không tiếng**: một lá thẻ đập xuống, cơm tách đúng hình, các miếng bay vào ráp kín hộp.

---

## 2. Trụ cột thiết kế ✅ Q-21

1. **Một thao tác, đọc được ngay.** Thẻ vẽ ra chỗ dao đi qua. Không cần giải thích luật thứ hai.
2. **Thua vì mình, không vì may.** Mọi thứ đều có tính quyết định. Có xem trước trước khi cắt, thả hỏng không mất thẻ, luôn hoàn tác được.
3. **Khoảnh khắc "vừa khít".** Phần thưởng chính là lúc miếng cuối cùng lấp kín hộp: âm thanh, rung, hiệu ứng tập trung vào đó.
4. **Nội dung có solver kiểm chứng.** Mọi level đều được máy kiểm tra là giải được trước khi đưa vào game, để sinh nhiều level mà không cần thiết kế tay từng màn.

---

## 3. Vòng chơi

**Trong một màn (moment-to-moment):** nhìn hình khay → chọn thẻ → (chạm để xoay thẻ) → kéo thẻ lên cơm, xem trước → thả để cắt → kéo các miếng vào khay → lặp lại tới khi khay kín. ✅

**Giữa các màn:** thắng → popup → màn tiếp. ✅ MEC-16
- Không có chapter, không có màn chọn level. Màn hình chính chỉ có **nút Play**, bấm là vào level kế tiếp.
- **Không có sao.** ✅ Q-05
- Cứ vài level lại giới thiệu một loại thẻ hoặc một loại khay mới.

**Dài hạn:** sưu tập các khay hình bóng, mở khóa theo mốc level. ✅ Q-15

---

## 4. Luật chơi

### 4.1 Thớt và lưới ✅ MEC-01
- Thớt là lưới ô, prototype dùng 7×10.
- Mỗi ô chia thành **4 tam giác**: trên, phải, dưới, trái. Nhờ vậy cắt chéo 45° tạo ra được nửa ô.
- Cơm và khay đều nằm trên cùng một thớt.

### 4.2 Miếng cơm ✅ MEC-01
- Mỗi miếng cơm là một tập tam giác liền nhau.
- Kéo miếng cơm đi được, khi thả sẽ bám theo ô.
- Không được đặt chồng lên miếng khác. Được đặt lên khay.
- **Miếng cơm phải nằm gọn trong khay hoặc ở hẳn bên ngoài khay**, không được vắt ngang mép khay (kể cả chỉ chạm vào ô có vành khay). Thả sai thì miếng về chỗ cũ (không hiện chữ gợi ý). ✅ (bạn chốt 2026-11-08 khi playtest)
- **Không tự xoay được.** Muốn xoay một miếng thì phải dùng thẻ chức năng **FN-01 Xoay 90°**. ✅ Q-03

### 4.3 Khay ✅ MEC-11, MEC-10
- **Mỗi màn 1 khay.** ✅ Q-01. MEC-11 thay cho MEC-02 "nhiều khay" (MEC-02 đã chuyển sang `Bỏ`).
- Khay có hình bất kỳ trên lưới, kể cả nửa ô tam giác (MEC-09 ✅).
- **Khay kéo được** trên thớt. ✅ Q-02
  - Khay không được đè lên cơm, không ra ngoài thớt.
  - Cơm nằm gọn trong khay thì đi theo khay.
  - Nắm khay ở ô trống hoặc ở viền khay.

### 4.4 Vách ngăn 🧪 MEC-14
- Khay có thể có vách ngăn cố định trên cạnh ô.
- Miếng cơm nằm vắt qua vách thì chưa tính là xong. Game báo "có miếng cơm vắt qua vách ngăn!".

### 4.5 Món ăn cố định trong khay 🧪 MEC-15 (vào MVP, Q-11)
- Khay có sẵn món ăn (trứng, thịt, xúc xích…) chiếm vài ô hoặc nửa ô.
- Cơm phải lấp kín phần còn lại quanh các món đó.
- Món cố định không kéo được và không bị cắt.
- Chưa có prototype. Cần thử để chốt: món có kéo được không, có món nằm trên thớt chờ người chơi đặt vào khay không.

### 4.6 Thẻ và cách cắt ✅ MEC-03, MEC-04, MEC-08
- Mỗi màn có một **tay bài cố định**. Người chơi tự chọn thứ tự và vị trí dùng thẻ.
- **Mọi thẻ cắt là một đường cắt** đi qua các giao điểm lưới: ngang, dọc, hoặc chéo 45°.
  - Đường hở = **thẻ đường**: chia miếng cơm theo đường.
  - Đường khép kín = **thẻ khuôn**: lấy ra đúng phần bên trong.
- Một thẻ có thể cắt nhiều miếng cùng lúc nếu đường đi qua chúng.
- Chạm thẻ (không kéo) để **xoay thẻ 90°**. Xoay thẻ miễn phí, không tốn thẻ.
- **Thẻ chức năng nằm chung tay bài** với thẻ cắt, kéo lên miếng cơm để dùng, dùng xong mất thẻ. 🧪 MEC-17 · Q-10

### 4.7 Xem trước và thả ✅
- Khi kéo, đường cắt bám lưới theo vị trí ngón tay. Các miếng sắp tách ra được tô màu khác nhau.
- Thả mà **không làm miếng nào tách ra**: thẻ rung, quay về tay, **không mất thẻ**.

### 4.8 Thắng và thua
- **Thắng:** khay được phủ kín, không miếng nào vắt qua vách. ✅
- **Không bắt buộc dùng hết thẻ.** ✅ Q-06
- **Không có thua:** hết thẻ mà chưa xong thì Hoàn tác hoặc Chơi lại. ✅ Q-04
- Trong các level thiết kế, tổng diện tích cơm (cộng món cố định) luôn bằng diện tích khay.

### 4.9 Hoàn tác, gợi ý
- **Hoàn tác** ✅ MEC-06: quay lại 1 bước (cắt, xoay thẻ, dùng thẻ chức năng, dời cơm, dời khay).
  - **Miễn phí, không giới hạn.** ✅ Q-07
- **Booster:** **không có booster.** ✅ Q-08 (bạn chốt 2026-11-06: game không cần booster)
- **Sao:** không có. ✅ Q-05

---

## 5. Thẻ

Tất cả thẻ là dữ liệu, tạo / sửa / xóa được bằng tool (mục 13.3). ✅ Q-09

### 5.1 Thẻ cắt đã chốt ✅

| ID | Tên | Nhóm | Tác dụng | Xuất hiện |
|---|---|---|---|---|
| CUT-01 | Nhát thẳng (1 / 2 / 3) | Đường | Đường thẳng theo độ dài in trên thẻ | Đầu tiên |
| CUT-06 | Khuôn dập (bản đầu: 1 ô) | Khuôn | Đường khép kín, lấy ra phần bên trong | Sớm |
| CUT-02 | Chữ L | Đường | Đường gấp góc vuông (2 + 1), cắt góc | Sau thẻ thẳng |
| CUT-09 | Chéo 2 | Chéo | Chéo 45° dài 2 ô, cắt khối 2×2 thành 2 tam giác | Khi bắt đầu khay hình bóng |
| CUT-08 | Chéo 1 | Chéo | Chéo 45° qua 1 ô, tách ô thành 2 nửa | Sau Chéo 2 |

**Nhận diện nhóm thẻ:**
- Thẻ đường: nét đỏ, khung trơn.
- Thẻ khuôn: hình tô cam, khung nét đứt.
- Thẻ chéo: nét tím, nền tím nhạt.

### 5.2 Thẻ cắt thêm vào MVP 🧪 Q-09

| ID | Tên | Tác dụng |
|---|---|---|
| CUT-03 | Chữ T | Thanh ngang 2 ô + thân 1 ô ở giữa, chia miếng thành 3 phần |
| CUT-10 | Khuôn 2×2 | Đường khép kín 2×2, lấy ra đúng 1 khối 2×2 |

Các thẻ cắt khác (Dấu cộng, Khía góc, Lát mỏng, khuôn tam giác…) để sau. 💡

### 5.3 Thẻ chức năng 🧪 Q-10

Thử trong prototype trước, sau đó mới quyết thẻ nào vào MVP. Thẻ chức năng được phát trong **tay bài như thẻ cắt** (MEC-17).

| ID | Tên | Tác dụng | Ghi chú |
|---|---|---|---|
| FN-01 | Xoay 90° | Xoay 1 miếng cơm 90° | Cách duy nhất để xoay miếng cơm (Q-03) |
| FN-02 | Lật gương | Lật 1 miếng cơm | |
| FN-03 | Dán | Ghép 2 miếng cạnh nhau thành 1 | Đổi hẳn cách giải: phải cắt rồi dán lại |
| FN-04 | Nhân bản | Copy 1 miếng nhỏ (≤ 2 ô) | Phá luật "diện tích cơm = diện tích khay", level phải tính thêm |

Các thẻ FN-05 → FN-07 (topping, đẩy) và Joker (SP-01) vẫn là ý tưởng. 💡

### 5.4 Quy tắc thiết kế thẻ
- Mỗi thẻ mới chỉ thêm **một** khả năng mới.
- Mỗi màn chỉ dùng **2–3 loại thẻ**.
- Phải có màn mà **chỉ thẻ đường mới giải được**, để người chơi không dùng mỗi thẻ khuôn cho dễ.

---

## 6. Các loại khay

| Loại | Mô tả | Trạng thái |
|---|---|---|
| Biến hình | Cơm hình A → khay hình B cùng diện tích (bậc thang, lệch góc, chữ T…) | 🧪 MEC-12 |
| Khay hình bóng | Hình đặc biệt: viên kẹo ◇, cá, ngôi sao… Thường cần thẻ chéo. Là phần thưởng sưu tập, mở theo mốc level | 🧪 MEC-13 |
| Khay có ngăn | Có vách ngăn, miếng cơm không được vắt qua | 🧪 MEC-14 |
| Khay có món cố định | Trứng, thịt, xúc xích… chiếm sẵn vài ô; cơm lấp quanh. **Vào MVP** | 🧪 MEC-15 · Q-11 |

---

## 7. Level design

### 7.1 Cấu trúc ✅ MEC-16
- **Không có chapter.** Level xếp thành một dãy liên tục, người chơi chỉ bấm Play.
- **MVP có 40 level.** ✅ Q-23
- Cứ 5–8 level giới thiệu 1 loại thẻ hoặc khay mới. Ở các mốc level có một khay hình bóng để mở vào bộ sưu tập.

### 7.2 Danh sách 40 level (bản Unity, 2026-11-08) 🧪

Nguồn: `Assets/_Project/Data/Levels/LevelDatabase.asset`. Level 1–14 là màn dạy port nguyên từ play.html; 15–40 do generator sinh theo `Data/Levels/Source/gen_specs.json` (riêng 33 làm tay). Độ khó = điểm solver (số nhánh phải thử / số lời giải, cộng số thẻ). ★ = khay hình bóng (cờ `unlocksSilhouette`, dùng cho bộ sưu tập sau này).

| Level | Thẻ | Khay | Độ khó | Học gì / gợi ý |
|---|---|---|---|---|
| 1 | Thẳng 1 | | 24 | Kéo thẻ để cắt, ráp lại |
| 2 | Thẳng 3 | | 24 | Hộp bậc thang, chạm để xoay thẻ |
| 3 | Chữ L | | 29 | Đường chữ L cắt góc |
| 4 | Thẳng 1, Thẳng 1 | trứng | 38 | Món cố định |
| 5 | Thẳng 2, Thẳng 1 | vách | 28 | Vách ngăn |
| 6 | Khuôn 2×2 | xúc xích | 34 | Thẻ khuôn |
| 7 | Chữ T | | 17 | Chữ T chia 3 |
| 8 | Chéo 2 | | 34 | Thẻ chéo |
| 9 | Chéo 1 | | 22 | Chéo 1, hộp con thuyền |
| 10 | Thẳng 2, Chéo 2, Chéo 2 | ★ kẹo ◇ | 66 | Chéo hai hướng (Thẳng 2 là thẻ bẫy) |
| 11 | Xoay | | 20 | Thẻ Xoay |
| 12 | Lật | | 20 | Thẻ Lật |
| 13 | Dán, Xoay | | 56 | Thẻ Dán |
| 14 | Thẳng 3, Thẳng 2, Thẳng 1, Nhân bản | | 41 | Hàng chờ thẻ thứ 4, Nhân bản (3 thẻ thẳng là thẻ dạy hàng chờ, không bắt buộc) |
| 15 | Thẳng 2, Thẳng 1 | | 28 | |
| 16 | Chữ L, Thẳng 1 | | 34 | |
| 17 | Chữ L, Thẳng 2 | vách | 53 | |
| 18 | Chữ L, Thẳng 1 | trứng | 34 | |
| 19 | Thẳng 1, Xoay | | 31 | |
| 20 | Chéo 2, Chéo 2 | ★ | 74 | |
| 21 | Khuôn 2×2, Thẳng 1 | xúc xích | 35 | |
| 22 | Chữ L, Chữ T | | 38 | |
| 23 | Lật, Xoay | | 38 | Cơm hình L |
| 24 | Thẳng 1, Thẳng 1 | vách | 38 | |
| 25 | Chéo 1, Chéo 1 | | 42 | |
| 26 | Chữ T, Xoay | thịt | 44 | |
| 27 | Chữ L, Thẳng 2, Thẳng 1 | | 47 | |
| 28 | Khuôn 2×2, Xoay | | 49 | |
| 29 | Chéo 2, Thẳng 2, Thẳng 1 | | 72 | |
| 30 | Chéo 2, Chéo 2, Thẳng 2 | ★ | 105 | |
| 31 | Chữ T, Chữ L | vách | 66 | |
| 32 | Chữ L, Thẳng 1 | trứng | 34 | |
| 33 | Dán, Xoay | | 50 | Làm tay: 2 thanh 2×1 → khay dọc 1×4 |
| 34 | Chữ L, Chéo 1, Thẳng 1 | | 73 | |
| 35 | Khuôn 2×2, Chữ L | | 53 | |
| 36 | Xoay, Lật | | 38 | Cơm hình P |
| 37 | Thẳng 1, Nhân bản, Thẳng 1 | | 49 | |
| 38 | Chéo 2, Chữ L, Thẳng 1 | vách | 86 | |
| 39 | Chữ T, Chữ L, Xoay | | 70 | |
| 40 | Chéo 2, Chéo 2, Chéo 1, Thẳng 1 | ★ | 135 | |

Solver đã kiểm tra cả 40 level: đều giải được và đều cần thẻ. Level 15–40 không có thẻ thừa. Màn dạy 10 và 14 cố ý có thẻ không bắt buộc (giống prototype).

**Cần chỉnh khi playtest:**
- Đường cong độ khó chưa tăng đều (vd 30 → 105 rồi 32 → 34). Sửa bằng cách đổi thứ tự trong LevelDatabase hoặc Regenerate trong Level Browser.
- Generator khó tạo màn bắt buộc dùng Xoay / Lật từ khối cơm chữ nhật (xoay thẻ cắt miễn phí nên thường cắt thẳng ra đúng chiều). Các màn Xoay / Lật vì vậy dùng cơm hình L / P hoặc thanh 4×1.

**Cách chơi thẻ chức năng trong prototype:**
- **Xoay:** kéo lên miếng cơm để xoay 90°. Chạm thẻ để đổi chiều ↻ / ↺.
- **Lật:** kéo lên miếng cơm để lật. Chạm thẻ để đổi lật ngang / dọc.
- **Dán:** là một đường dài 1 ô. Kéo đặt lên chỗ nối giữa 2 miếng kề nhau để ghép thành 1 miếng. Chạm thẻ để xoay đường dán.
- **Nhân bản:** kéo lên miếng tối đa 2 ô. Bản sao xuất hiện ở chỗ trống gần nhất.
- Nếu không đủ chỗ để làm, thẻ bật về tay và không bị mất.

### 7.3 Quy tắc level
- Tổng diện tích cơm + món cố định = diện tích khay (trừ level có thẻ Nhân bản).
- Có ít nhất 1 lời giải, đã được solver kiểm tra.
- Màn dạy thẻ mới thì chỉ nên có 1 lời giải rõ ràng. Màn sau mới mở ra nhiều cách giải.

### 7.4 Sinh level tự động
- **Sinh ngược:** bắt đầu từ hình khay → chia khay thành các miếng bằng các đường cắt của bộ thẻ → ráp các miếng thành hình cơm ban đầu (thường là hình chữ nhật). Thêm thẻ thừa làm bẫy nếu cần.
- **Solver (đã có trong prototype):** thử mọi thứ tự thẻ × hướng xoay × vị trí, rồi kiểm tra các miếng có xếp kín khay không, chỉ dịch không xoay, tôn trọng vách ngăn.
  - Cần mở rộng: thẻ chức năng (xoay, lật, dán, nhân bản) và món cố định.
- **Đo độ khó:** số lời giải, số nhánh solver phải thử, số thẻ.

---

## 8. Điều khiển và cảm giác ✅

| Quy tắc |
|---|
| Kéo thẻ: đường cắt bám lưới theo ngón tay; trên cảm ứng, đường cắt hiện cao hơn ngón tay khoảng 1,3 ô để không bị che |
| Xem trước: các miếng sắp tách được tô màu; đường đỏ = hợp lệ, xám nét đứt = không cắt được gì |
| Chạm thẻ (di chuyển dưới 12 px trên cảm ứng) = xoay thẻ 90° |
| Thả thẻ hỏng: thẻ rung, quay về tay, kèm âm báo |
| Cắt thành công: vệt chém trắng mờ dần + tiếng chém ngắn |
| Miếng cơm và khay bám ô khi thả; chỗ không hợp lệ thì quay về chỗ cũ, khung xem trước xanh/đỏ khi kéo |
| Nắm khay ở ô trống hoặc viền, kể cả khi khay đầy cơm |

### 8.1 Cảm giác (game feel) trong bản Unity 🧪 (làm 2026-11-08, chờ playtest)

| Khoảnh khắc | Hình | Âm / rung |
|---|---|---|
| Kéo thẻ | Bóng thẻ thu nhỏ bay theo ngón tay (lệch lên-phải để không che đường cắt); thẻ trong tay mờ đi | |
| Cắt | Vệt dao chạy dọc đường cắt rồi mờ; các miếng dạt ra ~0,1 ô rồi về; hạt cơm bắn ở hai đầu + giữa đường cắt; bóng thẻ "đập" xuống rồi tan | Tiếng dao, rung nhẹ |
| Dán / Xoay / Lật / Nhân bản | Vệt xanh (dán) hoặc chớp trắng phần cơm vừa biến hình | Tiếng "pop", rung nhẹ |
| Chạm xoay thẻ | | Tiếng blip |
| Kéo miếng cơm | Miếng nhấc lên (to 1,06×) có bóng đổ | |
| Thả đúng | Miếng trượt nốt vào ô, hơi nảy; vào hộp thì chớp trắng | Vào hộp: tiếng "tách" + rung nhẹ; ra ngoài: tiếng gỗ |
| Thả sai | Miếng bay về chỗ cũ; vắt mép hộp thì viền hộp chớp đỏ 2 lần | Tiếng "bụp" trầm + rung vừa |
| Thả thẻ hỏng | Thẻ rung trong tay, đường cắt xám rung tại chỗ, bóng thẻ bay về | Tiếng "bụp" trầm + rung vừa |
| Thắng | Thớt nảy, viền hộp sáng một vòng, nắp hộp sắt trượt xuống đóng lại; popup hiện sau ~1 giây | "Tách" + rung mạnh; nắp đóng: tiếng kim loại + chuông, rung vừa |

- Âm hiện tại là **âm tạm tự tổng hợp** (`Source/Audio/Gameplay_SFX/sfx_*.wav`). Thay bằng âm thật: giữ nguyên tên file.
- Rung chỉ chạy trên Android (`Core/Foundation/Feedback/Haptics.cs`).

---

## 9. UI và UX

### 9.1 Màn hình chính ✅ MEC-16 · Q-19
- Ở giữa là một **xe bán đồ ăn**, phía dưới là **nút Play** (kèm số level kế tiếp).
- Không có màn chọn level.
- Ảnh tham khảo: Notion > Tham khảo (bố cục kiểu quầy đồ ăn + nút Level to ở dưới). Prototype đã vẽ một xe cơm hộp riêng, có mái sọc, nồi cơm bốc hơi, chồng hộp sắt và linh vật trứng ốp la. ✅ Q-26

### 9.2 Màn chơi ✅ (mockup v3, chốt 2026-11-06)
Mockup: https://dtungz.github.io/PrototypeGame/cut-card/ui-v3.html (mã `E:\DataGame\PrototypeGame\cut-card\ui-v3.html`, tải được SVG từng trạng thái).
- **Tông màu:** ấm, dịu. Nền nâu đỏ trầm, viền nâu sẫm, nét dày kiểu vẽ tay (làm kỹ ở bước gen asset).
- **Đầu màn hình là kệ gỗ:**
  - giữa: bảng level dạng tấm gỗ có tab `Level N`, bên dưới là biểu tượng thẻ + số thẻ còn lại (`×3`);
  - trái: nút Cài đặt (Home và Chơi lại nằm trong menu cài đặt) và nút Gỡ quảng cáo;
  - phải: linh vật trứng ốp la ngồi trên kệ, tay cầm một lá thẻ.
- **Giữa:** thớt to, hộp cơm sắt **không có khoá cài / tay cầm** (bạn chốt 2026-11-08: không dùng art tay cầm), cơm viền trắng dày có hạt.
- **Dưới thớt:** nút Hoàn tác (nhỏ) + tay bài.
- **Không có hàng booster** (Q-08). Chỗ trống dồn cho thớt: ô lưới 44px, thớt căn giữa; nút hoàn tác và khung tay bài thẳng mép với thớt.
- **Không có banner quảng cáo** trên màn chơi; thớt và tay bài dồn xuống (khung 390×844: thớt y 172–636, tay bài 666–782, chừa đáy cho thanh home). ✅ (bạn chốt 2026-11-08)
- **Không có chữ hướng dẫn** trên màn chơi: không gợi ý đầu màn, không thông báo khi thả thẻ hỏng / vướng vách, không dòng "Hết thẻ". Phản hồi chỉ bằng hình (thẻ rung, khung đỏ, miếng về chỗ cũ). ✅ (bạn chốt 2026-11-08). Trường `hint` trong LevelData vẫn giữ làm ghi chú thiết kế.
- **Popup Hoàn thành:** căn giữa màn hình, lớp tối phủ kín màn hình. ✅ (bạn chốt 2026-11-08 khi playtest, thay cho "nằm nửa dưới")

### 9.3 Màn khác 💡
- Popup thắng: nút Tiếp tục, thông báo khi mở được khay hình bóng mới.
- Bộ sưu tập khay hình bóng (Q-15 ✅).
- Cài đặt (âm thanh, rung).

---

## 10. Art và âm thanh ✅ Q-19
- **Phong cách:** 2D dễ thương nhưng **đơn giản**, hợp với 1 người tự làm art.
- **Phong cách art đã chọn (2026-11-06): C · Phẳng tối giản.** Màu pastel ấm, nét viền mảnh bo tròn (hoặc không viền), hình khối đơn giản, chỉ một tông bóng phẳng. Ảnh mẫu: `.claude/art/phong-cach-ABC.png` (cột phải). Thay cho hướng "nét dày kiểu vẽ tay" ghi ở mục 9.2. Bố cục màn chơi theo mockup v3 giữ nguyên.
- **Chủ đề:** hộp cơm văn phòng (hộp sắt hoặc tương tự) thay cho hộp bento. ✅ Q-24
- **Đồ ăn:** cơm nhà, không cần kiểu Nhật: cơm, trứng, thịt, xúc xích…, có thể thêm nhiều món khác.
- **Màn hình chính:** xe bán đồ ăn phía trên nút Play.
- **Mascot:** linh vật trứng ốp la (thiết kế riêng), vẽ lại theo phong cách C.
- **Ràng buộc:** **không dùng** tên, nhân vật, art, level của Perfect Bento hay Inbento.
- **Âm thanh:** tiếng dao chém ngắn, tiếng "tách" khi thả miếng vào khay, nhạc lo-fi nhẹ.

---

## 11. Kiếm tiền ✅ Q-16
- **Mô hình:** quảng cáo + IAP gỡ quảng cáo.
- **Interstitial:** giữa các màn, sau level thứ 3 trở đi.
- **Rewarded:** hiện không có chỗ dùng (không booster Q-08, không thua Q-04). Nếu cần sẽ bàn sau.
- **IAP:** gỡ quảng cáo; về sau có thể bán skin hộp / xe đồ ăn.
- Hoàn tác: miễn phí (Q-07).

---

## 12. Meta và LiveOps
- **Sưu tập khay hình bóng**, mở theo mốc level. ✅ Q-15
- **Daily:** 1 câu đố mỗi ngày, để **sau MVP**. ✅ Q-14 (MEC-07)
- **Băng chuyền thẻ** (ý tưởng B): **bỏ**. ✅ Q-12 (MEC-05 và các loại hộp BOX-01 → 04 chuyển sang `Bỏ`)

---

## 13. Kỹ thuật

### 13.1 Dự án
- Unity 6, repo `gcc-dtung/SuperBento`.
- Cấu trúc theo [UnityProjectStructure.md](UnityProjectStructure.md), thay `<Game>` bằng `InBento`.
- Tài liệu trong `.claude/` được đưa lên git (dự án làm chung).
- Prototype tham chiếu (bản mới nhất): https://dtungz.github.io/PrototypeGame/cut-card/play.html, mã `E:\DataGame\PrototypeGame\cut-card\play.html` (repo `dtungz/PrototypeGame`).

### 13.2 Mô hình dữ liệu
- Tam giác `(x, y, t)` với `t` = 0 trên, 1 phải, 2 dưới, 3 trái.
- Cạnh cắt: `h` (ngang), `v` (dọc), `d` (chéo `\`), `a` (chéo `/`).
- Miếng cơm, khay và món cố định đều là tập tam giác. Vách ngăn là tập cạnh.
- **Level data (đề xuất):**
  - JSON gồm `rice[]` (tập tam giác + vị trí), `tray` (tập tam giác + vị trí + `walls[]` + `fixed[]` món cố định), `cards[]` (id thẻ).
  - Lưu bằng ScriptableObject trong `Data/Levels/`.

### 13.3 Thẻ là dữ liệu + tool chỉnh thẻ ✅ Q-09
- Mỗi thẻ là một bản ghi: `id`, `tên`, `nhóm` (đường / khuôn / chéo / chức năng), `đường cắt` (danh sách điểm trên lưới) hoặc `loại chức năng`, `xoay được`, màu nhận diện.
- Có tool để **tạo, sửa, xóa** thẻ: vẽ đường cắt trên lưới, xem trước, lưu.
- **Chưa làm ở bước prototype.** Tool sẽ làm khi chuyển sang Unity, dạng cửa sổ Editor. ✅ Q-25

### 13.4 Solver và sinh level
- Port từ prototype HTML sang Unity. Solver ở `Runtime/Gameplay/Model/LevelSolver.cs` (dùng được cả trong game cho debug), generator ở `Runtime/Editor/Levels/`.
- Menu `Tools/KoreanGame/Levels/…`: Import Prototype Levels (1-14), Generate Levels 15-40, Generate Missing Levels.
- Cửa sổ `Tools/KoreanGame/Level Browser` (Ctrl+Shift+L): xem từng level, Play from here, Check / Validate all, Regenerate.

### 13.5 Debug chuyển level ✅ (chỉ trong Editor, không hiện trên màn hình game)
- Menu **Tools → KoreanGame → Debug** (khi đang Play): Next Level (Ctrl+Alt+→), Previous Level (Ctrl+Alt+←), Restart Level (Ctrl+Alt+R), Win Now (Ctrl+Alt+W), Log Solution, Go To Level…, Reset Progress.
- Cửa sổ **Level Browser**: hàng điều khiển khi đang Play (◀ / ▶, nhập số + Go, Chơi lại, Thắng ngay, Lời giải, Xoá tiến độ) + Play from here ở từng level.
- Mọi menu dự án nằm dưới **Tools → KoreanGame**. Scene `Main` dựng lại được bằng `Tools/KoreanGame/Build Main Scene`.

---

## 14. Phạm vi MVP và lộ trình

**Trong MVP:**
- Thẻ cắt: 5 thẻ đã chốt + Chữ T + Khuôn 2×2.
- Thẻ chức năng: những thẻ qua được prototype (Q-10).
- 4 loại khay: biến hình, hình bóng, có ngăn, có món cố định.
- 40 level (Q-23), hoàn tác miễn phí, không sao, không chapter, không thua.
- Màn hình chính có xe đồ ăn + nút Play, popup thắng, bộ sưu tập khay hình bóng.
- Tool chỉnh thẻ, tool sinh level + solver.
- Quảng cáo + gỡ quảng cáo. Build Android + web.

**Ngoài MVP:** Daily, các thẻ cắt và chức năng còn lại, Joker. Không có booster (Q-08).

**Lộ trình đề xuất** (1 solo dev, **ước tính**, chưa có hạn từ bạn):

| Tuần | Việc |
|---|---|
| 0 | ✅ Prototype web (play.html): thẻ chức năng, món cố định, màn hình chính có xe đồ ăn |
| 1 | Port lõi cắt (mô hình tam giác) và solver sang Unity |
| 2 | Kéo thả, xem trước, khay, vách, món cố định, hoàn tác |
| 3 | Thẻ chức năng + tool chỉnh thẻ trong Unity |
| 4 | Tool sinh level + 20 level đầu |
| 5 | Art 2D đơn giản, UI (home với xe đồ ăn, popup, bộ sưu tập), âm thanh |
| 6 | 20 level còn lại, khay hình bóng |
| 7 | Quảng cáo, analytics, build Android + web |
| 8 | Playtest, sửa lỗi |

---

## 15. Chỉ số và điều kiện dừng
- **Playtest nội bộ** (ngưỡng ước tính, chưa có nguồn):
  - Dưới 60% người thử tự qua level 5 mà không cần giải thích → sửa phần hướng dẫn.
  - Người thử không muốn chơi tiếp sau level 10 → xem lại cốt lõi.
- **KPI store** (D1, D7, thời gian chơi, CPI): lấy từ benchmark của publisher puzzle hybrid-casual, **chưa xác minh**. Cần tra trước khi soft launch.

---

## 16. Rủi ro

| Rủi ro | Cách giảm |
|---|---|
| Bị coi là giống Inbento / Perfect Bento | Giữ điểm khác là cắt thật và biến hình; đổi chủ đề sang hộp cơm văn phòng; art, tên, level tự làm |
| Solo dev làm cả art | Art 2D đơn giản, ít asset, dùng lại khay hình bóng làm nội dung sưu tập |
| Thẻ chức năng làm level khó kiểm soát | Thử trong prototype trước; mở rộng solver trước khi làm level có thẻ chức năng |
| Miếng tam giác khó kéo trên điện thoại | Playtest máy thật; mở rộng vùng chạm; có thể cho chạm vào ô để chọn miếng |
| Block puzzle quá đông, CPI cao | Test creative (clip 3 giây) sớm, trước khi làm nhiều nội dung |
| Puzzle logic ít chỗ đặt quảng cáo | Interstitial giữa màn + banner + IAP gỡ quảng cáo. Không có booster / rewarded (Q-08) |
| Nội dung cạn nhanh | Sinh level tự động + solver |

---

## 17. Câu hỏi còn mở

Trong **tab Questions** trên Sheet:

| ID | Câu hỏi | Ghi chú |
|---|---|---|
| Q-20 | Tên game chính thức | Chưa chốt |

Cách trả lời: điền cột **"Trả lời của bạn"**, đổi Trạng thái thành `Đã trả lời`, rồi báo mình.

---

## Lịch sử thay đổi

| Ngày | ID | Thay đổi | Ai |
|---|---|---|---|
| 2026-11-06 | | Tạo khung GDD | Claude |
| 2026-11-06 | MEC-01–04, 06, 08–10; CUT-01, 02, 06, 08, 09 | Chốt các phương án đang chạy trong prototype Thẻ Cắt v2 | Bạn chốt, Claude ghi |
| 2026-11-06 | MEC-11–15, Q-01–Q-22 | GDD 1.0 hoàn chỉnh theo prototype v3 (1 khay); các điểm chưa chốt chuyển thành câu hỏi trong tab Questions | Claude |
| 2026-11-06 | Q-01–03, 05, 06, 09–11, 13–19, 22; MEC-02 (Bỏ), MEC-11 (Chốt), MEC-15–17; CUT-03, CUT-10, FN-01–04 | GDD 1.1: 1 khay; xoay miếng bằng thẻ FN-01; không sao, không chapter, chỉ nút Play; thẻ chức năng chung tay bài (đang thử); món cố định vào MVP; chủ đề hộp cơm văn phòng; solo dev. Thêm Q-23 → Q-26 | Bạn trả lời, Claude ghi |
| 2026-11-06 | Q-04, 07, 12, 21, 23–26; MEC-03, 05, 06; BOX-01–04 | GDD 1.2: không thua; hoàn tác miễn phí; bỏ băng chuyền; chốt trụ cột; 40 level; chốt hộp cơm văn phòng; tool chỉnh thẻ để lúc làm Unity. Prototype v4 (12 màn: thẻ chức năng, món cố định, màn hình chính có xe cơm hộp); gỡ v1–v3 | Bạn trả lời, Claude ghi |
| 2026-11-06 | Q-08 | Bỏ booster khỏi game; bố cục màn chơi dồn chỗ cho thớt to hơn (bản chơi thử play.html) | Bạn chốt, Claude ghi |
| 2026-11-08 | | Bỏ prototype v4 (index.html); prototype tham chiếu duy nhất là play.html | Bạn chốt, Claude ghi |
| 2026-11-08 | Q-23 | Bản Unity (KoreanGame, Unity 2022.3): core gameplay + 40 level đã qua solver + debug tool chuyển level (menu Tools). Luật mới: miếng cơm không được vắt ngang mép khay. Mục 7.2 thay bằng danh sách 40 level thật | Claude |
