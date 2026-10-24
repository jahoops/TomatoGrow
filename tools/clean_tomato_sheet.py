from PIL import Image
from pathlib import Path

INPUT = Path("Assets/tomato_stages_raw.png")
OUTPUT = Path("Assets/tomato_stages_clean.png")

COLUMNS = 4
ROWS = 2

OUT_CELL_W = 96
OUT_CELL_H = 96

img = Image.open(INPUT).convert("RGBA")

cell_w = img.width // COLUMNS
cell_h = img.height // ROWS

sprites = []

def is_fake_transparency_checker(pixel):
    r, g, b, a = pixel

    # Already transparent.
    if a < 20:
        return True

    # Removes light gray / white checkerboard backgrounds.
    bright = r > 175 and g > 175 and b > 175
    low_saturation = max(r, g, b) - min(r, g, b) < 35

    return bright and low_saturation

for row in range(ROWS):
    for col in range(COLUMNS):
        cell = img.crop((
            col * cell_w,
            row * cell_h,
            (col + 1) * cell_w,
            (row + 1) * cell_h
        ))

        pixels = cell.load()

        for y in range(cell.height):
            for x in range(cell.width):
                if is_fake_transparency_checker(pixels[x, y]):
                    pixels[x, y] = (255, 255, 255, 0)

        alpha = cell.getchannel("A")
        bbox = alpha.getbbox()

        if bbox is None:
            cropped = Image.new("RGBA", (1, 1), (255, 255, 255, 0))
        else:
            cropped = cell.crop(bbox)

        sprites.append(cropped)

out = Image.new("RGBA", (OUT_CELL_W * len(sprites), OUT_CELL_H), (255, 255, 255, 0))

for i, sprite in enumerate(sprites):
    scale = min(1.0, OUT_CELL_W / sprite.width, OUT_CELL_H / sprite.height)
    new_w = max(1, int(sprite.width * scale))
    new_h = max(1, int(sprite.height * scale))

    sprite = sprite.resize((new_w, new_h), Image.Resampling.NEAREST)

    x = i * OUT_CELL_W + (OUT_CELL_W - new_w) // 2
    y = OUT_CELL_H - new_h

    out.alpha_composite(sprite, (x, y))

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
out.save(OUTPUT)

print(f"Saved clean sprite sheet to {OUTPUT}")
print(f"Size: {out.width}x{out.height}")