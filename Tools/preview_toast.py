#!/usr/bin/env python3
"""
preview_toast.py — Renders a pixel-accurate preview of the Ovation achievement toast.

Simulates Unity's CanvasScaler (ScaleWithScreenSize, 1920×1080 reference, match=0.5)
so the output represents exactly what you'd see in a Unity game running at the given
resolution.

Usage:
    python3 preview_toast.py
    python3 preview_toast.py --width 1280 --height 720
    python3 preview_toast.py --name "Dragon Slayer" --rarity "Rare · 4.2%" --slots "Avatar,Banner,Spray"
    python3 preview_toast.py --bg-dark     # dark game background
    python3 preview_toast.py --bg-light    # light game background

Output: preview_toast.png (open with any image viewer)
"""

import argparse
import math
import os
import sys

try:
    from PIL import Image, ImageDraw, ImageFont, ImageFilter
except ImportError:
    print("Error: Pillow not installed. Run: pip3 install Pillow")
    sys.exit(1)

# ─── Configuration (mirrors AchievementToastConfig defaults) ─────────────────

TOAST_WIDTH_CANVAS   = 360     # canvas units
REFERENCE_W          = 1920
REFERENCE_H          = 1080
MATCH                = 0.5     # matchWidthOrHeight

# Colors (RGBA 0-255)
BG_COLOR         = (31,  31,  41, 235)   # #1F1F29 @ 92%
TITLE_COLOR      = (255, 255, 255, 255)
SECONDARY_COLOR  = (191, 191, 199, 255)  # ~75% white
ACCENT_COLOR     = (97,  181, 255, 255)  # Ovation blue
PILL_BG_COLOR    = (97,  181, 255,  51)  # accent @ 20%

# Layout (canvas units at 1920×1080)
PADDING          = 12
ICON_COL_W       = 48
ICON_SIZE        = 36
CONTENT_SPACING  = 4
HEADER_H         = 14
TITLE_H          = 24
RARITY_H         = 15
PILL_H           = 22
PILL_SPACING     = 6
PILL_PAD_X       = 8
PILL_CORNER_R    = 6
TOAST_CORNER_R   = 12
MARGIN           = 20

# Font sizes (canvas units / pt)
FONT_HEADER      = 10
FONT_TITLE       = 17
FONT_RARITY      = 11
FONT_PILL        = 10

# ─── Helpers ─────────────────────────────────────────────────────────────────

def canvas_scale(screen_w, screen_h):
    """Compute Unity CanvasScaler ScaleWithScreenSize scale factor."""
    scale_w = screen_w / REFERENCE_W
    scale_h = screen_h / REFERENCE_H
    return (scale_w ** (1 - MATCH)) * (scale_h ** MATCH)


def c(v, scale):
    """Convert canvas units to screen pixels."""
    return int(round(v * scale))


def load_font(size_px, bold=False, italic=False):
    """Try to load a system font, fall back to PIL default."""
    # Try common system fonts in order of preference
    candidates = []

    if bold and italic:
        candidates = [
            "/System/Library/Fonts/Supplemental/Arial Bold Italic.ttf",
            "/Library/Fonts/Arial Bold Italic.ttf",
        ]
    elif bold:
        candidates = [
            "/System/Library/Fonts/Helvetica.ttc",
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
            "/Library/Fonts/Arial Bold.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        ]
    elif italic:
        candidates = [
            "/System/Library/Fonts/Supplemental/Arial Italic.ttf",
            "/Library/Fonts/Arial Italic.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Oblique.ttf",
        ]
    else:
        candidates = [
            "/System/Library/Fonts/Helvetica.ttc",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/Library/Fonts/Arial.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        ]

    for path in candidates:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size_px)
            except Exception:
                pass

    # PIL fallback (no bold/italic support, pixelated at small sizes)
    try:
        return ImageFont.load_default(size=size_px)
    except TypeError:
        return ImageFont.load_default()


def text_width(draw, text, font):
    bbox = draw.textbbox((0, 0), text, font=font)
    return bbox[2] - bbox[0]


def rounded_rect(draw, x1, y1, x2, y2, radius, fill, outline=None, outline_width=1):
    """Draw a rounded rectangle."""
    r = min(radius, (x2 - x1) // 2, (y2 - y1) // 2)
    draw.rectangle([x1 + r, y1, x2 - r, y2], fill=fill)
    draw.rectangle([x1, y1 + r, x2, y2 - r], fill=fill)
    # corners
    draw.ellipse([x1, y1, x1 + 2*r, y1 + 2*r], fill=fill)
    draw.ellipse([x2 - 2*r, y1, x2, y1 + 2*r], fill=fill)
    draw.ellipse([x1, y2 - 2*r, x1 + 2*r, y2], fill=fill)
    draw.ellipse([x2 - 2*r, y2 - 2*r, x2, y2], fill=fill)

    if outline:
        draw.arc([x1, y1, x1 + 2*r, y1 + 2*r], 180, 270, fill=outline, width=outline_width)
        draw.arc([x2 - 2*r, y1, x2, y1 + 2*r], 270, 0,   fill=outline, width=outline_width)
        draw.arc([x1, y2 - 2*r, x1 + 2*r, y2], 90,  180, fill=outline, width=outline_width)
        draw.arc([x2 - 2*r, y2 - 2*r, x2, y2], 0,   90,  fill=outline, width=outline_width)
        draw.line([x1 + r, y1, x2 - r, y1], fill=outline, width=outline_width)
        draw.line([x1 + r, y2, x2 - r, y2], fill=outline, width=outline_width)
        draw.line([x1, y1 + r, x1, y2 - r], fill=outline, width=outline_width)
        draw.line([x2, y1 + r, x2, y2 - r], fill=outline, width=outline_width)


def draw_icon(draw, cx, cy, size, icon_path=None):
    """Draw the Ovation icon or a placeholder."""
    r = size // 2
    x1, y1, x2, y2 = cx - r, cy - r, cx + r, cy + r

    if icon_path and os.path.exists(icon_path):
        ico = Image.open(icon_path).convert("RGBA")
        ico = ico.resize((size, size), Image.LANCZOS)
        # Return the icon image to be composited
        return ico, (x1, y1)

    # Placeholder: accent-colored rounded square
    rounded_rect(draw, x1, y1, x2, y2, r // 3, fill=(*ACCENT_COLOR[:3], 180))
    return None, None


# ─── Main render ─────────────────────────────────────────────────────────────

def render_preview(
    screen_w=1920,
    screen_h=1080,
    achievement_name="Dragon Slayer",
    rarity_text="Rare · 4.2%",
    slot_names=None,
    dark_bg=True,
    output_path="preview_toast.png",
    icon_path=None,
    position="topright",
):
    if slot_names is None:
        slot_names = ["Avatar Frame", "Banner", "Spray"]

    MAX_PILLS = 5

    scale = canvas_scale(screen_w, screen_h)

    # ── Calculate toast layout in canvas units ────────────────────────────────
    has_rarity = bool(rarity_text)
    has_slots  = len(slot_names) > 0

    pills_to_show = slot_names[:MAX_PILLS]
    overflow      = len(slot_names) - len(pills_to_show)
    if overflow > 0:
        pills_to_show = slot_names[:MAX_PILLS - 1]
        overflow = len(slot_names) - len(pills_to_show)

    # Content column height
    content_h = HEADER_H + CONTENT_SPACING + TITLE_H
    if has_rarity:
        content_h += CONTENT_SPACING + RARITY_H
    if has_slots:
        content_h += CONTENT_SPACING + PILL_H

    # Panel height = max(icon + padding, content + top/bottom padding)
    panel_h = max(ICON_SIZE, content_h) + PADDING * 2
    panel_w = TOAST_WIDTH_CANVAS

    # ── Convert to screen pixels ──────────────────────────────────────────────
    PW  = c(panel_w, scale)
    PH  = c(panel_h, scale)
    PAD = c(PADDING, scale)
    ICW = c(ICON_COL_W, scale)
    ICS = c(ICON_SIZE, scale)
    SP  = c(CONTENT_SPACING, scale)
    HH  = c(HEADER_H, scale)
    TH  = c(TITLE_H, scale)
    RH  = c(RARITY_H, scale)
    PLH = c(PILL_H, scale)
    MGN = c(MARGIN, scale)
    CR  = c(TOAST_CORNER_R, scale)
    PCR = c(PILL_CORNER_R, scale)
    PSP = c(PILL_SPACING, scale)
    PPX = c(PILL_PAD_X, scale)

    # Font sizes in screen pixels
    fs_header = max(8,  c(FONT_HEADER, scale))
    fs_title  = max(10, c(FONT_TITLE,  scale))
    fs_rarity = max(8,  c(FONT_RARITY, scale))
    fs_pill   = max(8,  c(FONT_PILL,   scale))

    # ── Create image ──────────────────────────────────────────────────────────
    img  = Image.new("RGBA", (screen_w, screen_h), (0, 0, 0, 0))
    bg_color = (20, 20, 25, 255) if dark_bg else (230, 230, 235, 255)
    bg_img = Image.new("RGBA", (screen_w, screen_h), bg_color)

    # Position toast
    if "right" in position:
        tx = screen_w - PW - MGN
    elif "left" in position:
        tx = MGN
    else:
        tx = (screen_w - PW) // 2

    if "top" in position:
        ty = MGN
    else:
        ty = screen_h - PH - MGN

    # ── Draw toast panel ──────────────────────────────────────────────────────
    # Use a compositing approach for alpha blending
    overlay = Image.new("RGBA", (screen_w, screen_h), (0, 0, 0, 0))
    draw    = ImageDraw.Draw(overlay)

    rounded_rect(draw, tx, ty, tx + PW, ty + PH, CR, fill=BG_COLOR)

    # ── Icon ──────────────────────────────────────────────────────────────────
    icon_cx = tx + PAD + ICW // 2
    icon_cy = ty + PH // 2
    icon_img, icon_pos = draw_icon(draw, icon_cx, icon_cy, ICS, icon_path)

    # ── Text start ────────────────────────────────────────────────────────────
    text_x  = tx + PAD + ICW + c(PADDING // 2, scale)  # a bit of gap after icon col
    text_y  = ty + PAD
    # Vertically center if content is shorter than panel
    content_px_h = HH + SP + TH
    if has_rarity: content_px_h += SP + RH
    if has_slots:  content_px_h += SP + PLH
    text_y = ty + (PH - content_px_h) // 2

    content_w = PW - PAD - ICW - c(PADDING // 2, scale) - PAD  # available width for text

    fnt_header = load_font(fs_header)
    fnt_title  = load_font(fs_title, bold=True)
    fnt_rarity = load_font(fs_rarity, italic=True)
    fnt_pill   = load_font(fs_pill)

    # Header
    header_text = "ACHIEVEMENT UNLOCKED"
    draw.text((text_x, text_y), header_text, font=fnt_header, fill=ACCENT_COLOR)
    cursor_y = text_y + HH + SP

    # Title (ellipsis if too wide)
    title_display = achievement_name
    while text_width(draw, title_display, fnt_title) > content_w and len(title_display) > 3:
        title_display = title_display[:-1]
    if title_display != achievement_name:
        title_display = title_display[:-1] + "…"
    draw.text((text_x, cursor_y), title_display, font=fnt_title, fill=TITLE_COLOR)
    cursor_y += TH + SP

    # Rarity
    if has_rarity:
        draw.text((text_x, cursor_y), rarity_text, font=fnt_rarity, fill=SECONDARY_COLOR)
        cursor_y += RH + SP

    # Pills
    if has_slots:
        pill_x = text_x
        pill_y = cursor_y
        pill_labels = list(pills_to_show)
        if overflow > 0:
            pill_labels.append(f"+{overflow}")

        for label in pill_labels:
            lw = text_width(draw, label, fnt_pill)
            pw = lw + PPX * 2
            ph = PLH

            # Don't overflow toast width
            if pill_x + pw > tx + PW - PAD:
                break

            rounded_rect(draw, pill_x, pill_y, pill_x + pw, pill_y + ph, PCR, fill=PILL_BG_COLOR)
            ty_text = pill_y + (ph - fs_pill) // 2
            draw.text((pill_x + PPX, ty_text), label, font=fnt_pill, fill=ACCENT_COLOR)
            pill_x += pw + PSP

    # ── Composite everything ──────────────────────────────────────────────────
    result = Image.alpha_composite(bg_img, overlay)

    # Composite icon image if loaded
    if icon_img and icon_pos:
        result.paste(icon_img, icon_pos, icon_img)

    # ── Add a helpful annotation bar ─────────────────────────────────────────
    ann_h = 36
    ann = Image.new("RGBA", (screen_w, ann_h), (10, 10, 10, 220))
    ann_draw = ImageDraw.Draw(ann)
    ann_fnt  = load_font(12)
    label = (
        f"Screen: {screen_w}×{screen_h}  |  "
        f"Canvas scale: {scale:.3f}  |  "
        f"Toast: {PW}×{PH}px  |  "
        f"Reference: {REFERENCE_W}×{REFERENCE_H}"
    )
    ann_draw.text((8, 10), label, font=ann_fnt, fill=(200, 200, 200, 255))
    result.paste(ann, (0, screen_h - ann_h))

    result = result.convert("RGB")
    result.save(output_path)
    print(f"Saved: {output_path}  ({screen_w}×{screen_h}, scale={scale:.3f}, toast={PW}×{PH}px)")


# ─── CLI ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Preview Ovation achievement toast")
    parser.add_argument("--width",   type=int, default=1920)
    parser.add_argument("--height",  type=int, default=1080)
    parser.add_argument("--name",    default="Dragon Slayer")
    parser.add_argument("--rarity",  default="Rare · 4.2%")
    parser.add_argument("--slots",   default="Avatar Frame,Banner,Spray",
                        help="Comma-separated slot names")
    parser.add_argument("--no-rarity", action="store_true")
    parser.add_argument("--no-slots",  action="store_true")
    parser.add_argument("--bg-light",  action="store_true", help="Light game background")
    parser.add_argument("--position",  default="topright",
                        choices=["topright","topleft","bottomright","bottomleft","topcenter"])
    parser.add_argument("--icon",    default=None, help="Path to icon PNG")
    parser.add_argument("--output",  default="preview_toast.png")
    # Generate multiple sizes at once
    parser.add_argument("--all-sizes", action="store_true",
                        help="Generate previews for common screen sizes")

    args = parser.parse_args()

    icon_path = args.icon
    # Auto-detect icon next to this script
    if not icon_path:
        guess = os.path.join(os.path.dirname(__file__), "..", "icon.png")
        if os.path.exists(guess):
            icon_path = guess

    slot_names = [] if args.no_slots else [s.strip() for s in args.slots.split(",") if s.strip()]
    rarity     = None if args.no_rarity else args.rarity

    if args.all_sizes:
        sizes = [
            (1920, 1080, "preview_1080p.png"),
            (2560, 1440, "preview_1440p.png"),
            (3840, 2160, "preview_4k.png"),
            (1280,  720, "preview_720p.png"),
            ( 800,  600, "preview_small.png"),
        ]
        for w, h, out in sizes:
            render_preview(w, h, args.name, rarity, slot_names,
                           dark_bg=not args.bg_light, output_path=out,
                           icon_path=icon_path, position=args.position)
    else:
        render_preview(args.width, args.height, args.name, rarity, slot_names,
                       dark_bg=not args.bg_light, output_path=args.output,
                       icon_path=icon_path, position=args.position)


if __name__ == "__main__":
    main()
