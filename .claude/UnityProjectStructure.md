# Mẫu cấu trúc dự án Unity (dùng chung mọi dự án)

> Rút ra từ dự án PawBlock (theo mẫu Snacky_Dash). Thay `<Game>` bằng tên game (vd `PawBlock`), `<F>` bằng tên mảng tính năng.
> Mục tiêu: nhìn đường dẫn là biết file thuộc **loại gì** và **mảng nào**; kiến trúc "vừa đủ", không over-engineering.

---

## 1. Nguyên tắc chung

| # | Nguyên tắc |
| --- | --- |
| 1 | Mọi thứ **của game** nằm trong `Assets/_Project/`. Thứ của bên thứ ba (plugin, asset store, art gốc) nằm ngoài |
| 2 | Chia theo **loại** trước (code / dữ liệu / prefab / tài nguyên gốc), rồi mới tới **mảng tính năng** |
| 3 | Chỉ **3 assembly**: Runtime, Editor, Tests. Không tạo asmdef / asmref cho thư mục con |
| 4 | **1 scene** chính (`Main`), gom object theo nhóm `[Managers]` + `---- … ----` |
| 5 | Thư mục sâu tối đa ~4 tầng; không có `Misc/`, `Temp/`, `Old/`, `Backup/` |
| 6 | Di chuyển / đổi tên asset **chỉ trong Unity** (giữ GUID) |
| 7 | Tên type / method / biến tiếng Anh; comment + tài liệu thống nhất một ngôn ngữ |

---

## 2. Cây thư mục

```text
Assets/
├── _Project/
│   ├── Data/                    ScriptableObject + dữ liệu
│   │   ├── Config/              GameConfig (gốc) + config / database từng mảng (<F>Config, <F>Database)
│   │   └── Levels/              LevelDatabase, level theo nhóm (Custom/, Test/, Generated/ do tool tạo)
│   ├── Prefabs/
│   │   ├── InGame/              Object trong màn chơi (ô, khối, hiệu ứng world…)
│   │   └── UI/
│   │       ├── Popups/          Mọi popup (base + variant)
│   │       ├── Screens/<Màn>/   Màn hình / tab ghép nhiều mảng (Home/, Game/…)
│   │       └── Widgets/         Widget dùng chung (Button_Primary, CloseButton…)
│   │           └── <F>/         Widget riêng của 1 mảng (CoinHUD, thẻ Shop…)
│   ├── Runtime/                 <Game>.Runtime.asmdef
│   │   ├── Core/
│   │   │   ├── Contracts/       Events/ Interfaces/ Types/  — chỉ định nghĩa, không logic
│   │   │   └── Foundation/      Hạ tầng không dính luật game: Audio/ Clock/ Events/ Feedback/ Input/ Save/ Services/ Util/
│   │   ├── Gameplay/            Lõi màn chơi, chia thư mục con theo hệ thống (Board, Level, Input, Timer, UI, Visuals…)
│   │   ├── <F>/                 Mỗi mảng tính năng 1 thư mục (Economy/, Shop/, Profile/, Settings/…)
│   │   ├── LiveOps/<F>/         Sự kiện / liveops (DailyReward/, LuckyWheel/…)
│   │   ├── UI/
│   │   │   ├── Framework/       Base UI dùng chung: PopupView, MenuPanel, ButtonPress, SafeArea…
│   │   │   └── Home/            Main Menu (biết mọi mảng)
│   │   ├── App/                 Composition root: GameBootstrap, GameServices, GameConfig, ScreenFlow, GameFlowUI
│   │   ├── Save/                Migrate save giữa các phiên bản
│   │   ├── Cheat/               Cheat trong bản build (bọc #if !DISABLE_CHEATS)
│   │   ├── Editor/              <Game>.Editor.asmdef — App/ Core/ Gameplay/ UI/ … (theo mảng)
│   │   └── Tests/               <Game>.Tests.asmdef — <mảng>/Editor/ (EditMode)
│   ├── Scenes/                  Main.unity
│   ├── Settings/                URP asset, Renderer, Presets/ (preset nén texture tự áp)
│   └── Source/                  Tài nguyên gốc đang dùng
│       ├── Art/
│       │   ├── UI/<nhóm>/       Common/ (≥ 2 mảng dùng), Home/, Gameplay/, <F>/
│       │   ├── InGame/<nhóm>/   Common/, Gameplay/, <F>/
│       │   ├── Avatars/  Fonts/
│       │   └── Atlases/         Sprite Atlas V2, mỗi atlas trỏ tới thư mục ảnh của nhóm đó
│       ├── Audio/               BGM/, UI_SFX/, Gameplay_SFX/, …
│       └── Effect/              Shaders/, Materials/
├── ThirdParty/<Nguồn>/          Art / dữ liệu gốc: không sửa, không tham chiếu trực tiếp
├── Plugins/                     DOTween, plugin native…
├── TextMesh Pro/
├── Resources/                   Tối thiểu (chỉ thứ plugin bắt buộc, vd DOTweenSettings)
└── Docs/                        Tài liệu dự án
```

### Đặt file mới ở đâu

| Loại | Chỗ |
| --- | --- |
| Script runtime | `Runtime/<mảng>/` (liveops: `Runtime/LiveOps/<F>/`) |
| Event / interface / enum dùng giữa các mảng | `Runtime/Core/Contracts/{Events, Interfaces, Types}` |
| Hạ tầng không dính luật game | `Runtime/Core/Foundation/<nhóm>/` |
| Script Editor | `Runtime/Editor/<mảng>/` (tool đụng nhiều mảng: `Runtime/Editor/App/`) |
| Test | `Runtime/Tests/<mảng>/Editor/` |
| Config / database / level | `Data/Config/`, `Data/Levels/` |
| Prefab | `Prefabs/InGame/`, `Prefabs/UI/{Popups, Screens/<Màn>, Widgets[/<F>]}` |
| Ảnh UI / ảnh trong màn / âm / font / shader | `Source/Art/UI/…`, `Source/Art/InGame/…`, `Source/Audio/…`, `Source/Art/Fonts`, `Source/Effect/…` |
| Ảnh lấy từ bản gốc | `AssetDatabase.CopyAsset` từ `ThirdParty/` sang `Source/Art/`, tham chiếu bản copy |

---

## 3. Assembly

| asmdef | Thư mục | Cấu hình |
| --- | --- | --- |
| `<Game>.Runtime` | `_Project/Runtime/` | `autoReferenced: true`; references: TMP, UGUI, DOTween.Modules, InputSystem… |
| `<Game>.Editor` | `_Project/Runtime/Editor/` | `includePlatforms: ["Editor"]`; tham chiếu `<Game>.Runtime` + cùng thư viện |
| `<Game>.Tests` | `_Project/Runtime/Tests/` | `includePlatforms: ["Editor"]`, `autoReferenced: false`, `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll", …]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`; references thêm `UnityEngine.TestRunner`, `UnityEditor.TestRunner` |

Mẫu `<Game>.Runtime.asmdef`:

```json
{
    "name": "<Game>.Runtime",
    "rootNamespace": "",
    "references": ["Unity.TextMeshPro", "UnityEngine.UI", "DOTween.Modules", "Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Thư mục `Editor/` / `Tests/` nằm **trong** `Runtime/` nhưng có asmdef riêng nên không lọt vào bản build.

---

## 4. Mảng code và chiều phụ thuộc

```text
Core/Contracts ← Core/Foundation ← UI/Framework ← Gameplay, <F>, LiveOps/<F> ← App, UI/Home
(event, interface,  (EventBus, Save,    (PopupView,     (luật từng mảng)            (Bootstrap, luồng màn hình,
 type dùng chung)    Audio, Clock, Ads)  widget UI)                                  Main Menu)
```

Mũi tên = "được dùng bởi".

| Luật | Cách làm |
| --- | --- |
| Mảng tính năng **không** `using` mảng tính năng khác | Giao tiếp qua event (`EventBus<T>`) / interface trong `Core/Contracts` |
| `Core/Contracts` chỉ định nghĩa | Không logic |
| Chỉ `App` và `UI/Home` biết mọi mảng | Màn hình trộn nhiều mảng ghép ở `Prefabs/UI/Screens` |
| Chung 1 assembly → compiler **không** chặn sai chiều | Soát khi review; namespace theo mảng (`<Game>.Core.Contracts`, `<Game>.Core.Foundation`, `<Game>.Shared`, `<Game>.Features.<F>`, `<Game>.Game`) để nhìn `using` là biết phụ thuộc |

---

## 5. Khởi tạo và tham chiếu

| Quy tắc | Cách làm |
| --- | --- |
| **GameBootstrap** là nơi duy nhất tạo service | `[DefaultExecutionOrder(-1000)]`: save → migrate → settings, clock, ads, IAP → `GameServices` → gọi `Init(...)` từng component |
| Component cần dependency | `public void Init(...)`, giữ vào field; không đọc dependency trong `Awake` |
| Bootstrap → component | `[SerializeField]` trong Bootstrap; script Editor (Collector) tự điền khi lưu scene / vào Play |
| Không Singleton cho game logic | Chỉ hạ tầng (vd `AudioManager`) dùng `Singleton<T>`, đặt sẵn trong scene |
| Không `Find` / `GetComponent` / `AddComponent` / `Camera.main` lúc chạy | Tham chiếu đặt sẵn: `[SerializeField]` + `OnValidate()` tự điền trong Editor; object con dựng bằng `[ContextMenu]` rồi lưu |
| Pool | Template là con tắt sẵn, `Instantiate(template)` |
| Config | `GameConfig` gốc ở `Data/Config` trỏ tới config con; Bootstrap truyền config con qua `Init`. Không `Resources.Load` |
| Save | Mỗi mảng 1 class `[Serializable]` có `const string Key`; đổi cấu trúc thì thêm bước migrate + test |
| SDK (Ads / IAP / giờ) | Interface + bản giả (`FakeAdsService`…); tích hợp thật = thêm class, đổi 1 dòng trong Bootstrap |

---

## 6. Tổ chức scene `Main`

Nhóm `[Managers]` trên cùng; các nhóm ngăn cách `---- TÊN ----` là object rỗng ở gốc toạ độ, chỉ để gom (đổi cha không làm mất tham chiếu).

```text
[Managers]
├── GameBootstrap        GameBootstrap, SaveRunner, ClockRunner (runner hạ tầng)
├── Manager              Manager / controller của các mảng (Grid, Input, Level, Timer, Booster, Energy, ScreenFlow…)
│   └── <Template> (tắt)  mẫu cho pool
└── AudioManager         AudioManager, GameAudioEvents, GameHapticEvents; Music + Sfx_0..N (AudioSource đặt sẵn)
---- MÀN CHƠI ----
├── bg                   Nền (BackgroundFitter)
├── <Board / Spawner>    Lưới, khung, trang trí
└── <Object tắt sẵn>     Bubble, hand tutorial… bật khi cần
---- ĐIỀU KHIỂN ----
├── Main Camera
└── Global Light 2D
---- GIAO DIỆN ----
├── Canvas_HUD           [prefab] HUD màn chơi              sortingOrder 0
├── Canvas_Menu          [prefab] Main Menu (các tab)       sortingOrder 10
└── Canvas_Popup         luôn bật                           sortingOrder 20
    └── Popups           GameFlowUI; instance popup prefab (tắt sẵn), mỗi popup Canvas + GraphicRaycaster riêng
EventSystem              InputSystemUIInputModule
```

- Tool Editor tìm object theo tên trong **cả cây**, không chỉ gốc scene.
- Phần UI đổi liên tục (đồng hồ, thanh tiến độ) đặt Canvas con riêng để không rebuild cả Canvas.
- Ẩn bằng `SetActive(false)`.

---

## 7. Prefab và popup

| Quy tắc | Cách làm |
| --- | --- |
| Sửa UI | Mở prefab trong Prefab Mode, không sửa instance trong scene rồi Apply All |
| Popup mới | Kế thừa `PopupView`; hàm riêng nhận dữ liệu rồi `Open()`; dùng `OnOpened` / `OnClosed`. Root có `CanvasGroup` + `Canvas` + `GraphicRaycaster`. Thêm instance tắt vào `Canvas_Popup/Popups` |
| Script gốc prefab | Prefab màn hình / tab có script View ở gốc, **trùng tên prefab**; tự `Init` view con, Bootstrap chỉ gọi gốc |
| Tab Main Menu mới | Kế thừa `MenuPanel`, thêm vào navigator + 1 nút tab |
| Widget | Chung: `Prefabs/UI/Widgets`; riêng mảng: `Prefabs/UI/Widgets/<F>/`; widget mảng này không lồng widget mảng khác |

---

## 8. Asset, atlas, nén texture

| Quy tắc | Cách làm |
| --- | --- |
| Bản gốc | `ThirdParty/<Nguồn>/` giữ nguyên, không sửa, không tham chiếu |
| Sprite Atlas | V2 ở `Source/Art/Atlases/`, 1 atlas / nhóm. Không Rotation, không Tight Packing, Padding 4, không mipmap, max 2048. Nền to, texture Spine, particle **không** vào atlas |
| Preset nén | `Settings/Presets` + Preset Manager lọc theo đường dẫn: `Backgrounds/` ASTC 8x8, `Art/UI/` ASTC 4x4, còn lại ASTC 6x6; tắt Read/Write, mipmap |
| Tool Editor tìm config | Theo kiểu (`AssetDatabase.FindAssets("t:<Type>")`), không viết cứng đường dẫn |

---

## 9. Thư mục `Docs/`

```text
Docs/
├── README.md               Mục lục
├── ProjectStructure.md     Mảng code, thư mục, scene, event (bản của dự án đó, theo mẫu này)
├── Modules/<F>.md          Mảng có > vài script: tóm tắt, phụ thuộc, event, save, config, cách Bootstrap nối
├── Features/               Hành vi từng tính năng, cách sửa
├── Refactor/
│   ├── UnityCodingGuidelines.md   Guide chung (không sửa theo dự án)
│   └── ProjectConventions.md      Ngoại lệ / bổ sung riêng dự án, kèm lý do
└── RoadMap/                Mỗi lần push 1 file (NN_<tên>.md); tài liệu lịch sử không sửa
```

---

## 10. Checklist khi dựng dự án mới

- [ ] Tạo `Assets/_Project/{Data, Prefabs, Runtime, Scenes, Settings, Source}` theo cây mục 2
- [ ] Tạo 3 asmdef (mục 3), thêm thư viện cần dùng vào references
- [ ] Tạo `Core/Contracts`, `Core/Foundation` (EventBus, Save, Audio, Singleton<T>, Util), `UI/Framework` (PopupView, MenuPanel, ButtonPress)
- [ ] `App/GameBootstrap` + `GameServices` + `GameConfig` (ScriptableObject ở `Data/Config`)
- [ ] Scene `Main` với `[Managers]`, `---- MÀN CHƠI ----`, `---- ĐIỀU KHIỂN ----`, `---- GIAO DIỆN ----`, 3 Canvas (0 / 10 / 20)
- [ ] Preset nén texture + Preset Manager; thư mục `Source/Art/Atlases`
- [ ] `.gitignore` Unity chuẩn (+ thư mục art chỉ để học nếu có); `.editorconfig` mức warning
- [ ] `Docs/` với `ProjectStructure.md`, `Refactor/ProjectConventions.md`, `RoadMap/`
- [ ] Rà: không mảng nào `using` mảng khác, không `.Instance` cho game logic, không `Find`/`GetComponent` lúc chạy
