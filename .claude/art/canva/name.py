"""Đặt tên cho các món đã cắt (cut/ -> named/) và gộp các món bị tách đôi.
Chạy sau slice.py: python name.py"""
from pathlib import Path
from PIL import Image
import slice as S

HERE = Path(__file__).parent
NAMED = HERE / 'named'; NAMED.mkdir(exist_ok=True)

NAMES = {
    '1-tren-thot': ['rice_tile', 'rice_half', 'tin_3x2', 'food_egg', 'food_sausage', 'food_pork_belly', 'food_tofu', 'food_greens', 'food_shrimp'],
    '2-the': ['card_line', 'card_mold', 'card_diag', 'card_fn', 'icon_rotate', None, 'icon_glue', 'icon_copy', None],
    '3-nut-booster': ['btn_settings', 'btn_home', 'btn_undo', 'btn_restart', 'btn_noads', 'booster_hint', 'booster_wand', 'booster_hammer', 'booster_locked', 'badge_red'],
    '4-bang-khung': ['shelf', 'plaque_tab', 'popup', 'board', 'btn_pill_green', 'label_pill'],
    '5-mau-c-linh-vat': ['sample_rice_4x2', 'sample_tin_4x2', 'sample_card', 'sample_btn_settings', 'mascot_egg'],
}
# món bị tách: (tấm, [chỉ số món cần gộp, tính từ 1], tên)
MERGE = [('2-the', [6, 9], 'icon_flip')]

for sheet, names in NAMES.items():
    for i, n in enumerate(names, 1):
        if n:
            Image.open(HERE / 'cut' / sheet / f'{i:02d}.png').save(NAMED / f'{n}.png')

for sheet, idxs, name in MERGE:
    im = Image.open(HERE / 'sheets' / f'{sheet}.png').convert('RGB')
    w, h = im.size
    bg = S.background_mask(im)
    comps = [c for c in S.components(bg, w, h) if c[0] >= S.MIN_AREA * w * h]
    comps.sort(key=lambda c: (round(c[1][1] / (h / 8)), c[1][0]))
    boxes = [comps[k - 1][1] for k in idxs]
    x0, y0 = min(b[0] for b in boxes) - 6, min(b[1] for b in boxes) - 6
    x1, y1 = max(b[2] for b in boxes) + 6, max(b[3] for b in boxes) + 6
    from PIL import ImageFilter
    alpha = Image.frombytes('L', (w, h), bytes(0 if b else 255 for b in bg)).filter(ImageFilter.GaussianBlur(0.8))
    rgba = im.copy(); rgba.putalpha(alpha)
    rgba.crop((max(0, x0), max(0, y0), min(w, x1), min(h, y1))).save(NAMED / f'{name}.png')

print(len(list(NAMED.glob('*.png'))), 'file')
