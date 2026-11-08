"""Tách nền trắng và cắt từng món ra khỏi các tấm asset Canva.

Chạy: python slice.py  (đọc sheets/*.png, ghi ra cut/<tên tấm>/NN.png)
- Nền: điểm ảnh gần trắng nối liền với mép ảnh coi là nền (flood fill từ mép),
  nên phần trắng bên trong món (cơm, lòng trắng trứng) không bị xoá.
- Món: mỗi vùng không phải nền đủ lớn là một món; cắt theo khung bao + lề 6px.
"""
from pathlib import Path
from collections import deque
from PIL import Image, ImageFilter

HERE = Path(__file__).parent
SHEETS, CUT = HERE / 'sheets', HERE / 'cut'
WHITE = 236          # ngưỡng "gần trắng"
MIN_AREA = 0.0015    # món nhỏ nhất, tính theo tỉ lệ diện tích ảnh


def background_mask(im):
    w, h = im.size
    px = im.load()
    bg = bytearray(w * h)
    def near(p):
        r, g, b = p[:3]
        if r >= WHITE and g >= WHITE and b >= WHITE:
            return True
        # bóng nhạt màu kem Canva vẽ sẵn quanh món, khoảng (237, 228, 217): coi là nền
        return r >= 228 and g >= 218 and b >= 204 and 0 <= r - b <= 30 and r >= g >= b - 2
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            q.append((x, y))
    while q:
        x, y = q.popleft()
        i = y * w + x
        if bg[i] or not near(px[x, y]):
            continue
        bg[i] = 1
        if x > 0: q.append((x - 1, y))
        if x < w - 1: q.append((x + 1, y))
        if y > 0: q.append((x, y - 1))
        if y < h - 1: q.append((x, y + 1))

    # Lượt 2: phần bóng đậm hơn sát mép (tối đa 18px từ nền), chỉ màu kem ấm, không phải viền xám / nâu của món
    def shadow(p):
        r, g, b = p[:3]
        return r >= 205 and g >= 192 and b >= 176 and 8 <= r - b <= 34 and r >= g >= b - 2
    dist = {}
    q = deque()
    for i in range(w * h):
        if bg[i]:
            x, y = i % w, i // w
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < w and 0 <= ny < h and not bg[ny * w + nx]:
                    q.append((nx, ny, 1))
    while q:
        x, y, dd = q.popleft()
        i = y * w + x
        if bg[i] or dd > 18 or not shadow(px[x, y]):
            continue
        bg[i] = 1
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and not bg[ny * w + nx]:
                q.append((nx, ny, dd + 1))
    return bg


def components(bg, w, h):
    seen = bytearray(w * h)
    out = []
    for start in range(w * h):
        if bg[start] or seen[start]:
            continue
        q = deque([start]); seen[start] = 1
        x0 = y0 = 10 ** 9; x1 = y1 = -1; n = 0
        while q:
            i = q.popleft(); n += 1
            x, y = i % w, i // w
            x0, y0, x1, y1 = min(x0, x), min(y0, y), max(x1, x), max(y1, y)
            for j in (i - 1 if x > 0 else -1, i + 1 if x < w - 1 else -1, i - w if y > 0 else -1, i + w if y < h - 1 else -1):
                if j >= 0 and not bg[j] and not seen[j]:
                    seen[j] = 1; q.append(j)
        out.append((n, (x0, y0, x1 + 1, y1 + 1)))
    return out


def process(path):
    im = Image.open(path).convert('RGB')
    w, h = im.size
    bg = background_mask(im)
    alpha = Image.frombytes('L', (w, h), bytes(0 if b else 255 for b in bg))
    alpha = alpha.filter(ImageFilter.GaussianBlur(0.8))   # mép mềm, tránh răng cưa
    rgba = im.copy(); rgba.putalpha(alpha)
    comps = [c for c in components(bg, w, h) if c[0] >= MIN_AREA * w * h]
    comps.sort(key=lambda c: (round(c[1][1] / (h / 8)), c[1][0]))   # theo hàng rồi theo cột
    dest = CUT / path.stem; dest.mkdir(parents=True, exist_ok=True)
    for k, (_, (x0, y0, x1, y1)) in enumerate(comps, 1):
        pad = 6
        box = (max(0, x0 - pad), max(0, y0 - pad), min(w, x1 + pad), min(h, y1 + pad))
        rgba.crop(box).save(dest / f'{k:02d}.png')
    return len(comps)


if __name__ == '__main__':
    for p in sorted(SHEETS.glob('*.png')):
        print(p.name, process(p), 'món')
