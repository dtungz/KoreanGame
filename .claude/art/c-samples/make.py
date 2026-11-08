"""Bộ 5 món mẫu phong cách C (phẳng tối giản), vẽ bằng SVG.
Chạy: python make.py  -> ghi các file .svg và preview.html cạnh file này."""
from pathlib import Path

OUT = Path(__file__).parent

# ---------- Bảng màu C ----------
P = dict(
    line='#8C7867',      # nét viền nâu xám
    rice='#FBF6EC', rice_sh='#EFE4D1', grain='#E6D7BF', rice_line='#D2C3AB',
    tin='#A69C91', tin_sh='#8F857A', tin_rim='#BDB4A9', well='#E2DCD3', well_sh='#CFC6BA', tin_line='#7F7469',
    card='#FBF3E4', card_sh='#EFE2CC', card_line='#CDB79E', grid='#E9DCC8', cut='#E2675A',
    wood='#BC875D', wood_sh='#9E6C47', wood_line='#7C5539', cream='#FBF3E4',
    white='#FFFFFF', white_sh='#EEE7DC', yolk='#FFB84D', yolk_sh='#F39C33', blush='#F6A497', eye='#5A4636',
)


def svg(w, h, body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" '
            f'fill="none">{body}</svg>\n')


def rice_block(cols=4, rows=2, c=56, gap=3):
    pad = 8
    w, h = cols * c + pad * 2, rows * c + pad * 2 + 4
    b = ''
    grains = [(.28, .30, 25), (.66, .38, -30), (.40, .66, 60), (.72, .72, 10)]
    for y in range(rows):
        for x in range(cols):
            X, Y = pad + x * c + gap / 2, pad + y * c + gap / 2
            s = c - gap
            b += f'<rect x="{X}" y="{Y + 3}" width="{s}" height="{s}" rx="11" fill="{P["rice_sh"]}"/>'
            b += f'<rect x="{X}" y="{Y}" width="{s}" height="{s}" rx="11" fill="{P["rice"]}" stroke="{P["rice_line"]}" stroke-width="2"/>'
            for i, (fx, fy, r) in enumerate(grains):
                if (x + y + i) % 3 == 2:
                    continue
                gx, gy = X + fx * s, Y + fy * s
                b += f'<ellipse cx="{gx:.1f}" cy="{gy:.1f}" rx="4.2" ry="2.4" fill="{P["grain"]}" transform="rotate({r} {gx:.1f} {gy:.1f})"/>'
    return svg(w, h, b)


def lunch_tin(cols=4, rows=2, c=50):
    pad, hw = 14, 12
    w, h = cols * c + pad * 2 + hw * 2, rows * c + pad * 2 + 6
    x0 = hw
    b = ''
    # tay cầm hai bên
    for hx in (2, w - hw - 2):
        b += f'<rect x="{hx}" y="{h / 2 - 16}" width="{hw}" height="28" rx="5" fill="{P["tin_sh"]}" stroke="{P["tin_line"]}" stroke-width="2"/>'
    bw, bh = cols * c + pad * 2, rows * c + pad * 2
    b += f'<rect x="{x0}" y="6" width="{bw}" height="{bh}" rx="18" fill="{P["tin_sh"]}"/>'
    b += f'<rect x="{x0}" y="0" width="{bw}" height="{bh}" rx="18" fill="{P["tin"]}" stroke="{P["tin_line"]}" stroke-width="2.5"/>'
    b += f'<rect x="{x0 + 6}" y="6" width="{bw - 12}" height="{bh - 12}" rx="13" fill="{P["tin_rim"]}"/>'
    for y in range(rows):
        for x in range(cols):
            X, Y = x0 + pad + x * c + 3, pad + y * c + 3
            s = c - 6
            b += f'<rect x="{X}" y="{Y}" width="{s}" height="{s}" rx="8" fill="{P["well_sh"]}"/>'
            b += f'<rect x="{X}" y="{Y + 4}" width="{s}" height="{s - 4}" rx="8" fill="{P["well"]}"/>'
    return svg(w, h, b)


def card(w=140, h=184):
    g, n, ox, oy = 26, 3, (w - 78) / 2, 26
    b = f'<rect x="3" y="7" width="{w - 6}" height="{h - 8}" rx="18" fill="{P["card_sh"]}"/>'
    b += f'<rect x="3" y="2" width="{w - 6}" height="{h - 8}" rx="18" fill="{P["card"]}" stroke="{P["card_line"]}" stroke-width="3"/>'
    for i in range(n + 1):
        b += f'<line x1="{ox + i * g}" y1="{oy}" x2="{ox + i * g}" y2="{oy + n * g}" stroke="{P["grid"]}" stroke-width="2.5" stroke-linecap="round"/>'
        b += f'<line x1="{ox}" y1="{oy + i * g}" x2="{ox + n * g}" y2="{oy + i * g}" stroke="{P["grid"]}" stroke-width="2.5" stroke-linecap="round"/>'
    # đường cắt chữ L, nằm đúng trên lưới
    x1, y1, x2, y2 = ox + 2 * g, oy, ox + 2 * g, oy + g
    b += (f'<path d="M{x1} {y1} V{y2} H{ox}" stroke="{P["cut"]}" stroke-width="6" stroke-linecap="round" '
          f'stroke-linejoin="round" stroke-dasharray="0.1 11"/>')
    b += f'<circle cx="{x1}" cy="{y1}" r="5" fill="{P["cut"]}"/>'
    # tên thẻ: thanh tròn đơn giản ở đáy
    b += f'<rect x="{w / 2 - 34}" y="{h - 46}" width="68" height="20" rx="10" fill="{P["grid"]}"/>'
    b += (f'<text x="{w / 2}" y="{h - 31}" text-anchor="middle" font-family="Nunito, Segoe UI, sans-serif" '
          f'font-size="13" font-weight="800" fill="{P["line"]}">Chữ L</text>')
    return svg(w, h, b)


def gear_button(s=120):
    import math
    cx = cy = s / 2 - 2
    pts = []
    for i in range(16):
        a = math.pi * i / 8 - math.pi / 2
        r = 30 if i % 2 == 0 else 23
        for d in (-.19, .19):
            pts.append(f'{cx + r * math.cos(a + d):.1f} {cy + r * math.sin(a + d):.1f}')
    gear = 'M' + ' L'.join(pts) + 'Z'
    b = f'<rect x="4" y="10" width="{s - 8}" height="{s - 12}" rx="26" fill="{P["wood_sh"]}"/>'
    b += f'<rect x="4" y="2" width="{s - 8}" height="{s - 12}" rx="26" fill="{P["wood"]}" stroke="{P["wood_line"]}" stroke-width="2.5"/>'
    b += f'<path d="{gear}" fill="{P["cream"]}" stroke-linejoin="round"/>'
    b += f'<circle cx="{cx}" cy="{cy}" r="10" fill="{P["wood"]}"/>'
    return svg(s, s, b)


def egg_mascot():
    w, h = 200, 168
    white = ('M44 92 C30 60 54 36 82 40 C96 18 132 20 142 42 C172 40 186 70 172 94 '
             'C186 120 160 146 132 138 C116 156 82 156 70 140 C40 146 22 118 44 92 Z')
    b = f'<path d="{white}" fill="{P["white_sh"]}" transform="translate(4 6)"/>'
    b += f'<path d="{white}" fill="{P["white"]}" stroke="{P["line"]}" stroke-width="3" stroke-linejoin="round"/>'
    # tay
    b += f'<ellipse cx="34" cy="104" rx="11" ry="8" fill="{P["white"]}" stroke="{P["line"]}" stroke-width="3" transform="rotate(-30 34 104)"/>'
    b += f'<ellipse cx="178" cy="98" rx="11" ry="8" fill="{P["white"]}" stroke="{P["line"]}" stroke-width="3" transform="rotate(35 178 98)"/>'
    # lòng đỏ
    b += f'<circle cx="104" cy="88" r="38" fill="{P["yolk"]}"/>'
    b += f'<path d="M136 72 A38 38 0 0 1 80 118 A40 40 0 0 0 136 72Z" fill="{P["yolk_sh"]}"/>'
    b += f'<ellipse cx="88" cy="66" rx="9" ry="5" fill="#FFFFFF" opacity=".7" transform="rotate(-25 88 66)"/>'
    # mặt
    b += f'<ellipse cx="92" cy="88" rx="4" ry="5.5" fill="{P["eye"]}"/><ellipse cx="116" cy="88" rx="4" ry="5.5" fill="{P["eye"]}"/>'
    b += f'<path d="M97 100 Q104 106 111 100" stroke="{P["eye"]}" stroke-width="3" stroke-linecap="round"/>'
    b += f'<ellipse cx="80" cy="100" rx="6" ry="3.5" fill="{P["blush"]}"/><ellipse cx="128" cy="100" rx="6" ry="3.5" fill="{P["blush"]}"/>'
    return svg(w, h, b)


ITEMS = [
    ('c_rice_block.svg', 'Khối cơm', rice_block()),
    ('c_lunch_tin.svg', 'Hộp cơm sắt', lunch_tin()),
    ('c_card.svg', 'Thẻ cắt', card()),
    ('c_btn_settings.svg', 'Nút cài đặt', gear_button()),
    ('c_mascot_egg.svg', 'Linh vật trứng', egg_mascot()),
]
for name, _, s in ITEMS:
    (OUT / name).write_text(s, encoding='utf-8')

cells = ''.join(f'<figure><div class="box">{s}</div><figcaption>{label}</figcaption></figure>' for _, label, s in ITEMS)
(OUT / 'preview.html').write_text(f'''<!doctype html><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Nunito:wght@800;900&display=swap" rel="stylesheet">
<style>body{{margin:0;background:#F6EFE4;font-family:Nunito,sans-serif;color:#8C7867}}
.grid{{display:grid;grid-template-columns:repeat(3,1fr);gap:28px;padding:36px;align-items:center;justify-items:center}}
figure{{margin:0;text-align:center}}.box{{height:210px;display:flex;align-items:center;justify-content:center}}
figcaption{{font-weight:900;font-size:18px;margin-top:8px}}h1{{margin:28px 36px 0;font-size:22px}}</style>
<h1>Phong cách C · 5 món mẫu (SVG)</h1><div class="grid">{cells}</div>''', encoding='utf-8')
print('ok', [n for n, _, _ in ITEMS])
