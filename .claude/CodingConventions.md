# Quy ước code bắt buộc (KoreanGame)

> Đọc trước khi viết hay sửa code trong `Assets/_Project/`. Áp dụng cho cả người và Claude. Bổ sung cho [UnityProjectStructure.md](UnityProjectStructure.md).

## 1. DOTween: luôn bảo vệ tween

Mọi tween (DOTween.To, Sequence, `.DOScale`, `.DOPunchPosition`…) **bắt buộc**:

| # | Quy tắc | Cách làm |
|---|---|---|
| 1 | Gắn chủ cho tween | Gọi `.Guard(owner)` (`KoreanGame.Core.TweenSafety`): đặt target = owner và tự Kill khi GameObject của owner bị huỷ. Không dùng `.SetTarget` / `.SetLink` trực tiếp |
| 2 | Kill trước khi tạo tween mới trên cùng owner | `owner.KillTweens(complete)` (không gọi `DOKill` / `DOTween.Kill` trực tiếp, vì helper đã bỏ qua owner null / đã huỷ) |
| 3 | Dọn khi view tắt / bị huỷ | Mỗi MonoBehaviour có tween phải `KillTweens` trong `OnDisable` (hoặc `OnDestroy` nếu object luôn bật) |
| 4 | Setter của `DOTween.To` không được chạm object có thể đã huỷ | Kiểm tra null trong setter (vd `if (slashRenderer == null) return;`) |
| 5 | Chỉ dùng API lõi của DOTween | Các module (`DOAnchorPos`, `DOFade`, `DOShakeAnchorPos`…) nằm ở `Plugins/…/Modules`, assembly `KoreanGame.Runtime` **không** tham chiếu được. Dùng `DOTween.To(getter, setter, …)` |
| 6 | Không bật lại tween log thừa | Giữ `DOTweenSettings`: Safe Mode bật, log behaviour mặc định |

Mẫu:

```csharp
using KoreanGame.Core;

body.KillTweens(true);
DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .3f).Guard(body);
body.DOScale(1f, .3f).SetEase(Ease.OutBack).Guard(body);

void OnDisable() => body.KillTweens(true);
```

## 2. Console phải sạch

- Sau mỗi lần sửa code: Console không có **error** và **warning** nào từ code dự án. Có warning thì sửa ngay, không để dồn.
- Thêm / đổi `[SerializeField]` trong component của scene → sửa `SceneBuilder` rồi chạy **Tools → KoreanGame → Build Main Scene**. Test `MainSceneTests` báo đỏ nếu còn tham chiếu chưa gán (tránh `UnassignedReferenceException` khi bấm Play).
- Chạy EditMode test (`KoreanGame.Tests`) trước khi commit: phải xanh hết, gồm test "40 level giải được".
- Warning `MCP-FOR-UNITY: Suppressed duplicate command…` **không phải lỗi code dự án**: đó là MCP for Unity chặn lệnh bị gửi lại khi Unity trả lời chậm (thường do cửa sổ Unity ở nền). Giảm bằng: Edit → Preferences → General → **Interaction Mode = No Throttling**, và để Unity không bị thu nhỏ khi Claude đang thao tác. Xoá bằng nút Clear của Console.

## 3. UI màn chơi

- Không có chữ hướng dẫn trên màn chơi (gợi ý, thông báo lỗi, "hết thẻ"…). Phản hồi bằng hình: rung, khung đỏ/xanh, trả về chỗ cũ.
- Không có banner quảng cáo trên màn chơi.
- Lớp tối của popup phủ **kín màn hình**, không chỉ khung 390×844. Popup căn giữa màn hình.
- Debug chỉ nằm trong menu **Tools → KoreanGame** và cửa sổ Level Browser, không đặt trên màn hình game.
