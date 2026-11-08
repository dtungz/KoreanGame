"""Đưa asset Canva (named/) vào dự án Unity.

- Bản gốc (5 tấm 1264px) -> Assets/ThirdParty/Canva/Sheets/  (không sửa, không tham chiếu)
- Ảnh dùng trong game    -> Assets/_Project/Source/Art/{UI,InGame}/<nhóm>/
- Chuẩn hoá cho lưới: rice_cell (ô vuông), rice_half (nửa ô chéo), tin_cell (1 ô hộp sắt), tin_latch
- Ghi slicing.json: viền 9-slice (px, theo ảnh) để script import trong Unity áp vào Sprite Border.
Chạy: python export_unity.py
"""
import json, shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).parent
NAMED = HERE / 'named'
ROOT = Path('E:/Everything/Git/InBento/Assets')
ART = ROOT / '_Project/Source/Art'
RAW = ROOT / 'ThirdParty/Canva/Sheets'
CELL = 256

GROUPS = {
    'UI/Common': ['btn_settings', 'btn_home', 'btn_undo', 'btn_restart', 'btn_noads', 'btn_pill_green', 'label_pill', 'popup', 'badge_red', 'mascot_egg'],
    'UI/Gameplay': ['shelf', 'plaque_tab', 'booster_hint', 'booster_wand', 'booster_hammer', 'booster_locked',
                    'card_line', 'card_mold', 'card_diag', 'card_fn', 'icon_rotate', 'icon_flip', 'icon_glue', 'icon_copy'],
    'InGame/Gameplay': ['board', 'food_egg', 'food_sausage', 'food_pork_belly', 'food_tofu', 'food_greens', 'food_shrimp'],
}
# viền 9-slice (trái, dưới, phải, trên) theo pixel ảnh, đo bằng mắt trên ảnh đã cắt
BORDERS = {
    'card_line': [44, 48, 44, 44], 'card_mold': [44, 48, 44, 44], 'card_diag': [44, 48, 44, 44], 'card_fn': [44, 48, 44, 44],
    'popup': [56, 60, 56, 56], 'board': [60, 64, 60, 60], 'label_pill': [68, 0, 68, 0], 'btn_pill_green': [74, 0, 74, 0],
    'shelf': [40, 0, 40, 0],
}


def trim(im):
    box = im.getchannel('A').getbbox()
    return im.crop(box) if box else im


GAP = 6   # khe giữa hai ô cơm cạnh nhau (mỗi ô lùi vào GAP px mỗi phía), giữ kiểu "khối cơm chia ô"
TIN = dict(well=(179, 163, 150), well_shadow=(140, 127, 116), wall=(218, 206, 195), rim=(229, 216, 205),
           rim_shadow=(196, 184, 172), outline=(150, 140, 132))


def rice_cell():
    """Ô cơm vuông, thân cơm nằm giữa ô với khe đều GAP px bốn phía (không còn lệch do bóng)."""
    src = trim(Image.open(NAMED / 'rice_tile.png').convert('RGBA'))
    body = src.resize((CELL - 2 * GAP, CELL - 2 * GAP), Image.LANCZOS)
    out = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0)); out.alpha_composite(body, (GAP, GAP))
    return out


def rice_half(cell):
    """Nửa ô chéo, giữ lại phía trên-trái của đường chéo '/'. Hướng khác: xoay 90° trong game.
    Cùng khung và khe với rice_cell nên đặt cạnh nhau khít lưới."""
    g, n = GAP, CELL
    tri = [(g, g), (n - g - 4, g), (g, n - g - 4)]
    mask = Image.new('L', (n, n), 0); ImageDraw.Draw(mask).polygon(tri, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(0.7))
    out = cell.copy()
    out.putalpha(Image.composite(cell.getchannel('A'), Image.new('L', (n, n), 0), mask))
    # mép chéo: dải gờ cùng màu viền đáy của ô cơm
    edge = Image.new('RGBA', (n, n), (0, 0, 0, 0)); d = ImageDraw.Draw(edge)
    d.line([tri[1], tri[2]], fill=(222, 205, 181, 255), width=8)
    d.line([(tri[1][0] - 6, tri[1][1] + 2), (tri[2][0] + 2, tri[2][1] - 6)], fill=(238, 226, 206, 255), width=5)
    edge = edge.filter(ImageFilter.GaussianBlur(0.6))
    edge.putalpha(Image.composite(edge.getchannel('A'), Image.new('L', (n, n), 0), mask))
    return Image.alpha_composite(out, edge)


def rice_texture():
    """Vân cơm ghép liền mạch (1 ô = 1 tấm): cắt phần giữa ảnh ô cơm, lật gương 2×2 rồi thu lại,
    nên mép trái khớp mép phải, mép trên khớp mép dưới. Dùng để phủ cả miếng cơm thành một khối liền."""
    # Màu nền và màu hạt lấy từ ảnh ô cơm Canva; hạt rải ngẫu nhiên (cố định seed), hạt chạm mép được vẽ lặp
    # sang mép đối diện nên các tấm ghép không lộ đường nối.
    import random, math
    src = trim(Image.open(NAMED / 'rice_tile.png').convert('RGBA')).convert('RGB')
    w, h = src.size
    base = src.crop((int(w * .3), int(h * .3), int(w * .7), int(h * .6))).resize((1, 1), Image.LANCZOS).getpixel((0, 0))
    k, n = 4, CELL
    big = Image.new('RGBA', (n * k, n * k), base + (255,)); d = ImageDraw.Draw(big)
    rnd = random.Random(7)
    shade = tuple(max(0, c - 16) for c in base)        # bóng hạt
    for i in range(46):
        cx, cy = rnd.uniform(0, n), rnd.uniform(0, n)
        L, W, a = rnd.uniform(15, 20), rnd.uniform(7, 9), rnd.uniform(0, math.pi)
        for ox in (-n, 0, n):
            for oy in (-n, 0, n):
                def grain(dx, dy, col, s=1.0):
                    pts = []
                    for j in range(24):
                        t = 2 * math.pi * j / 24
                        x, y = L / 2 * s * math.cos(t), W / 2 * s * math.sin(t)
                        pts.append(((cx + ox + dx + x * math.cos(a) - y * math.sin(a)) * k, (cy + oy + dy + x * math.sin(a) + y * math.cos(a)) * k))
                    d.polygon(pts, fill=col)
                grain(0, 1.4, shade)                    # viền tối phía dưới hạt
                grain(0, 0, (255, 255, 252))            # thân hạt
                grain(-.6, -.8, (255, 255, 255), .5)    # điểm sáng
    return big.resize((n, n), Image.LANCZOS).convert('RGB')


def tin_cell():
    """Một ô của khay sắt: nền vách + một ngăn lõm bo góc. Vành ngoài khay vẽ bằng code theo hình khay (màu ở TIN)."""
    n, k = CELL, 4
    big = Image.new('RGBA', (n * k, n * k), TIN['wall'] + (255,)); d = ImageDraw.Draw(big)
    m, r = 22 * k, 34 * k
    d.rounded_rectangle([m, m, n * k - m, n * k - m], radius=r, fill=TIN['well_shadow'])
    d.rounded_rectangle([m, m + 14 * k, n * k - m, n * k - m], radius=r, fill=TIN['well'])
    return big.resize((n, n), Image.LANCZOS)


def tin_well():
    """Ngăn lõm của khay, dạng 9-slice: kéo dài phủ 1 ô hoặc nhiều ô gộp (ngăn chứa món cố định dài)."""
    n, k = 128, 4
    big = Image.new('RGBA', (n * k, n * k), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    r = 30 * k
    d.rounded_rectangle([0, 0, n * k - 1, n * k - 1], radius=r, fill=TIN['well_shadow'])
    d.rounded_rectangle([0, 12 * k, n * k - 1, n * k - 1], radius=r, fill=TIN['well'])
    return big.resize((n, n), Image.LANCZOS)


def tin_latch():
    t = Image.open(NAMED / 'tin_3x2.png').convert('RGBA')
    w, h = t.size
    return trim(t.crop((0, int(h * .22), int(w * .13), int(h * .78))))


def main():
    RAW.mkdir(parents=True, exist_ok=True)
    for p in (HERE / 'sheets').glob('*.png'):
        shutil.copy2(p, RAW / p.name)
    written = []
    for grp, names in GROUPS.items():
        d = ART / grp; d.mkdir(parents=True, exist_ok=True)
        for n in names:
            trim(Image.open(NAMED / f'{n}.png').convert('RGBA')).save(d / f'{n}.png')
            written.append(f'{grp}/{n}.png')
    g = ART / 'InGame/Gameplay'
    rc = rice_cell(); rc.save(g / 'rice_cell.png'); rice_half(rc).save(g / 'rice_half.png')
    rice_texture().save(g / 'rice_texture.png'); written.append('InGame/Gameplay/rice_texture.png')
    tin_cell().save(g / 'tin_cell.png'); tin_latch().save(g / 'tin_latch.png'); tin_well().save(g / 'tin_well.png')
    written += [f'InGame/Gameplay/{n}.png' for n in ('rice_cell', 'rice_half', 'tin_cell', 'tin_latch', 'tin_well')]
    (ART / 'slicing.json').write_text(json.dumps({
        'note': 'Sprite Border (L, B, R, T) tính theo pixel ảnh. Script import trong Unity sẽ áp vào TextureImporter.spriteBorder.',
        'borders': {**{f'{g}/{n}.png': b for g, ns in GROUPS.items() for n in ns if n in BORDERS for b in [BORDERS[n]]},
                    'InGame/Gameplay/tin_well.png': [32, 32, 32, 40]},
        'tray': {'note': 'Vành khay vẽ bằng code bao quanh các ô tin_cell, bo góc; quai tin_latch gắn chồng lên vành hai bên.',
                 'colors': {k: '#%02X%02X%02X' % v for k, v in TIN.items()}, 'rim_px_at_cell_256': 26, 'corner_px_at_cell_256': 46},
        'rice': {'gap_px_at_cell_256': GAP},
    }, ensure_ascii=False, indent=2), encoding='utf-8')
    print(len(written), 'ảnh ->', ART)


if __name__ == '__main__':
    main()
