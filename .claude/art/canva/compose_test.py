"""Ghép thử màn chơi (bố cục mockup v3) bằng asset đã xuất, để kiểm tra ô cơm / hộp sắt ghép theo lưới.
Chạy: python compose_test.py -> ghep-thu-man-choi.png (780x1688, tức 390x844 @2x)"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ART = Path('E:/Everything/Git/InBento/Assets/_Project/Source/Art')
S = 2
W, H = 390 * S, 844 * S
img = Image.new('RGBA', (W, H), (243, 233, 218, 255))
d = ImageDraw.Draw(img)
F = lambda n: ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', n * S)
ink = (122, 98, 82)


def load(p):
    return Image.open(ART / p).convert('RGBA')


def put(p_or_im, x, y, w=None, h=None, center=False):
    im = load(p_or_im) if isinstance(p_or_im, str) else p_or_im
    if w and not h: h = round(im.height * w / im.width)
    if h and not w: w = round(im.width * h / im.height)
    if w: im = im.resize((round(w), round(h)), Image.LANCZOS)
    if center: x, y = x - im.width / 2, y - im.height / 2
    img.alpha_composite(im, (round(x), round(y)))
    return im


def nine(p, x, y, w, h, b):
    """Vẽ ảnh 9-slice b=(L,B,R,T) theo pixel ảnh, co theo hệ số s cho vừa."""
    im = load(p); L, B, R, T = b; s = S * 0.5
    L2, B2, R2, T2 = [round(v * s) for v in (L, B, R, T)]
    xs = [0, L, im.width - R, im.width]; ys = [0, T, im.height - B, im.height]
    dx = [x, x + L2, x + w - R2, x + w]; dy = [y, y + T2, y + h - B2, y + h]
    for i in range(3):
        for j in range(3):
            sw, sh = xs[i + 1] - xs[i], ys[j + 1] - ys[j]; tw, th = dx[i + 1] - dx[i], dy[j + 1] - dy[j]
            if sw > 0 and sh > 0 and tw > 0 and th > 0:
                img.alpha_composite(im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])).resize((round(tw), round(th)), Image.LANCZOS), (round(dx[i]), round(dy[j])))


# ---- kệ gỗ + đầu màn hình ----
d.rectangle([0, 0, W, 132 * S], fill=(232, 216, 196))
put('UI/Gameplay/shelf.png', -6 * S, 112 * S, w=W + 12 * S)
put('UI/Common/mascot_egg.png', 296 * S, 58 * S, w=84 * S)     # ngồi trên kệ
pl = put('UI/Gameplay/plaque_tab.png', 195 * S, 72 * S, w=170 * S, center=True)
d.text((195 * S, 60 * S), 'Level 9', font=F(17), fill=(255, 246, 230), anchor='mm', stroke_width=3 * S // 2, stroke_fill=(150, 98, 60))
d.text((195 * S, 90 * S), '×3', font=F(22), fill=(255, 246, 230), anchor='mm', stroke_width=2 * S, stroke_fill=(150, 98, 60))
put('UI/Common/btn_settings.png', 16 * S, 26 * S, w=52 * S)
put('UI/Common/btn_noads.png', 22 * S, 82 * S, w=42 * S)

# ---- thớt + lưới ----
C, O, BX, BY = 40 * S, 12 * S, 43 * S, 150 * S
BW, BH = 7 * C + 2 * O, 10 * C + 2 * O
nine('InGame/Gameplay/board.png', BX, BY, BW, BH, (60, 64, 60, 60))
for i in range(1, 7): d.line([(BX + O + i * C, BY + O), (BX + O + i * C, BY + O + 10 * C)], fill=(222, 200, 168), width=S)
for j in range(1, 10): d.line([(BX + O, BY + O + j * C), (BX + O + 7 * C, BY + O + j * C)], fill=(222, 200, 168), width=S)
cx = lambda x: BX + O + x * C
cy = lambda y: BY + O + y * C

# ---- khay: vành bo tròn bao quanh các ô (code) + ô tin_cell + quai gắn vào vành ----
from PIL import ImageFilter
import json
TC = json.loads((ART / 'slicing.json').read_text(encoding='utf-8'))['tray']['colors']
hexc = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
TRAY = [(2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (4, 2), (1, 3), (2, 3), (3, 3), (4, 3)]
RIM, RAD = round(C * 26 / 256) + 2 * S, round(C * 46 / 256) + 4 * S


def tray_mask(grow):
    """Mặt nạ hình khay nở thêm `grow` px, bo góc ngoài bằng làm mờ + cắt ngưỡng."""
    m = Image.new('L', (W, H), 0); dm = ImageDraw.Draw(m)
    for x, y in TRAY:
        dm.rectangle([cx(x) - grow, cy(y) - grow, cx(x) + C + grow, cy(y) + C + grow], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(RAD / 2))
    return m.point(lambda v: 255 if v > 128 else 0).filter(ImageFilter.GaussianBlur(0.8))


def fill_mask(mask, color, dy=0):
    layer = Image.new('RGBA', (W, H), color + (255,)); layer.putalpha(mask)
    img.alpha_composite(layer, (0, dy))


outer, inner = tray_mask(RIM + 2 * S), tray_mask(RIM)
shadow = Image.new('RGBA', (W, H), (90, 70, 55, 0)); shadow.putalpha(outer.point(lambda v: v * 45 // 255))
img.alpha_composite(shadow, (0, 5 * S))                          # bóng đổ nhẹ dưới khay
fill_mask(outer, hexc(TC['outline']))                            # nét viền ngoài
fill_mask(inner, hexc(TC['rim_shadow']))                         # mép dưới vành (lộ ra ở cạnh dưới)
face = Image.new('RGBA', (W, H), hexc(TC['rim']) + (255,))
face.putalpha(Image.composite(inner, Image.new('L', (W, H), 0), inner.transform((W, H), Image.AFFINE, (1, 0, 0, 0, 1, 3 * S))))
img.alpha_composite(face)                                         # mặt vành sáng
tin = load('InGame/Gameplay/tin_cell.png').resize((C, C), Image.LANCZOS)
for x, y in TRAY: img.alpha_composite(tin, (cx(x), cy(y)))
latch = load('InGame/Gameplay/tin_latch.png')
lh = round(C * .95)
lw = round(latch.width * lh / latch.height)
L = latch.resize((lw, lh), Image.LANCZOS)
ymid = round(cy(2.5) - lh / 2)
img.alpha_composite(L, (round(cx(1) - RIM - lw + 8 * S), ymid))                                  # quai trái chồng lên vành
img.alpha_composite(L.transpose(Image.FLIP_LEFT_RIGHT), (round(cx(5) + RIM - 8 * S), ymid))       # quai phải
put('InGame/Gameplay/food_sausage.png', cx(3), cy(3.5), w=2 * C - 6 * S, center=True)

# ---- cơm: ô vuông + một nửa ô chéo ----
cell = load('InGame/Gameplay/rice_cell.png').resize((C, C), Image.LANCZOS)
half = load('InGame/Gameplay/rice_half.png').resize((C, C), Image.LANCZOS)
for x, y in [(1, 7), (2, 7), (3, 7), (4, 7), (1, 8), (2, 8), (3, 8)]: img.alpha_composite(cell, (cx(x), cy(y)))
img.alpha_composite(half.rotate(180), (cx(4), cy(8)))          # nửa ô góc dưới-phải
img.alpha_composite(cell, (cx(2), cy(1)))                        # 1 ô đã đặt vào khay
img.alpha_composite(half, (cx(3), cy(1)))                        # nửa ô trong khay
put('InGame/Gameplay/food_egg.png', cx(5.5), cy(5.5), w=C - 2 * S, center=True)   # thử món đặt lẻ

# ---- tay bài ----
HY = BY + BH + 8 * S
PX0, PX1, PH = 76 * S, 376 * S, 116 * S                       # khung tay bài
put('UI/Common/btn_undo.png', 14 * S, HY + (PH - 52 * S) // 2, w=52 * S)
d.rounded_rectangle([PX0, HY, PX1, HY + PH], radius=22 * S, fill=(226, 212, 194))
names = [('card_mold', 'Khuôn 2×2'), ('card_line', 'Thẳng 2'), ('card_fn', 'Xoay')]
CW, CH, GAPC = 84 * S, 104 * S, 14 * S
X0 = PX0 + (PX1 - PX0 - (len(names) * CW + (len(names) - 1) * GAPC)) // 2   # căn giữa khung
for k, (n, label) in enumerate(names):
    x = X0 + k * (CW + GAPC); y = HY + (PH - CH) // 2
    nine(f'UI/Gameplay/{n}.png', x, y, 84 * S, 104 * S, (44, 48, 44, 44))
    gx, gy, g = x + 18 * S, y + 14 * S, 16 * S
    if n != 'card_fn':
        for i in range(4):
            d.line([(gx + i * g, gy), (gx + i * g, gy + 3 * g)], fill=(228, 214, 196), width=S)
            d.line([(gx, gy + i * g), (gx + 3 * g, gy + i * g)], fill=(228, 214, 196), width=S)
        if n == 'card_mold': d.rectangle([gx + g * .5, gy + g * .5, gx + g * 2.5, gy + g * 2.5], outline=(232, 140, 60), width=4 * S)
        else: d.line([(gx + g * .5, gy + g * 1.5), (gx + g * 2.5, gy + g * 1.5)], fill=(226, 103, 90), width=5 * S)
    else:
        put('UI/Gameplay/icon_rotate.png', x + 42 * S, y + 38 * S, w=46 * S, center=True)
    d.text((x + 42 * S, y + 86 * S), label, font=F(11), fill=ink, anchor='mm')

# ---- booster ----
BYB = HY + PH + 18 * S + 26 * S                               # cách tay bài 18px
for k, n in enumerate(['booster_hint', 'booster_wand', 'booster_locked']):
    put(f'UI/Gameplay/{n}.png', (113 + k * 82) * S, BYB, w=52 * S, center=True)
put('UI/Common/badge_red.png', 216 * S, BYB - 20 * S, w=20 * S, center=True)
d.text((216 * S, BYB - 20 * S), '2', font=F(11), fill=(255, 255, 255), anchor='mm')
for k, t in enumerate(['Gợi ý', 'Booster 2', 'Lv 15']):
    d.text(((113 + k * 82) * S, BYB + 36 * S), t, font=F(11), fill=ink, anchor='mm')

# ---- banner ----
d.rectangle([0, H - 56 * S, W, H], fill=(236, 232, 226))
d.text((W / 2, H - 28 * S), 'Banner quảng cáo 320×50', font=F(12), fill=(160, 150, 140), anchor='mm')
img.convert('RGB').save(Path(__file__).parent / 'ghep-thu-man-choi.png')
print('ok')
