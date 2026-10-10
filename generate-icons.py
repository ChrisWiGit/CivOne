#!/usr/bin/env python3
#
# Generates every program icon of the project from one source image.
#
# Purpose
#   The game logo used to exist once per platform and format, each file maintained by hand.
#   This script makes "resources/icon.png" the single source and derives the rest from it, so
#   changing the logo means replacing one file and running this script once.
#
# Usage
#   pip install Pillow
#   python generate-icons.py
#
#   Options:
#     --source PATH     Use a different source image (default: resources/icon.png).
#     --keep-background Do not make the background transparent.
#
# Input
#   resources/icon.png                      The logo at full resolution.
#
# Output
#   resources/windows/CivOne.ico            Windows application and shortcut icon (16..256 px).
#   resources/linux/CivOne.png              Linux desktop icon (256 px).
#   runtime/sdl/Resources/WindowIcon.png    Icon of the running game window (48 px), stored as an
#                                           indexed PNG because the game decodes it with its own
#                                           palette-based image pipeline.
#   resources/WizardIcon.png                Icon drawn in the startup wizard header (256 px), kept
#                                           as RGBA because the wizard scales it down itself and
#                                           needs the alpha channel to do that smoothly. Embedded
#                                           into the CivOne assembly.
#
# The source artwork sits on a solid dark background. By default the background is flood filled
# from the four corners and turned transparent, which only affects the area around the logo and
# leaves dark pixels inside the logo untouched.

import argparse
import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Pillow is required. Install it with: pip install Pillow")

ROOT = os.path.dirname(os.path.abspath(__file__))

SOURCE = os.path.join("resources", "icon.png")
ICO_TARGET = os.path.join("resources", "windows", "CivOne.ico")
LINUX_TARGET = os.path.join("resources", "linux", "CivOne.png")
WINDOW_TARGET = os.path.join("runtime", "sdl", "Resources", "WindowIcon.png")
WIZARD_TARGET = os.path.join("resources", "WizardIcon.png")

ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]
LINUX_SIZE = 256
WINDOW_SIZE = 48
WIZARD_SIZE = 256
BACKGROUND_THRESHOLD = 40


def remove_background(image):
    """Flood fills the background from the four corners and makes it transparent."""
    width, height = image.size
    for corner in [(0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)]:
        if image.getpixel(corner)[3] == 0:
            continue
        ImageDraw.floodfill(image, corner, (0, 0, 0, 0), thresh=BACKGROUND_THRESHOLD)
    return image


def write_ico(image, path):
    image.save(path, format="ICO", sizes=[(size, size) for size in ICO_SIZES])


def write_png(image, path, size):
    image.resize((size, size), Image.LANCZOS).save(path, format="PNG")


def write_indexed_png(image, path, size):
    """Writes an indexed PNG (colour type 3), which the game can decode without a quantiser."""
    resized = image.resize((size, size), Image.LANCZOS)
    # One palette slot is reserved for transparency, so the colours get the remaining 255.
    indexed = resized.convert("RGB").quantize(colors=255, method=Image.MEDIANCUT)

    palette = indexed.getpalette()[: 255 * 3]
    palette += [0, 0, 0] * (256 - len(palette) // 3)
    indexed.putpalette(palette)

    transparent = resized.getchannel("A").point(lambda value: 255 if value < 128 else 0)
    indexed.paste(255, transparent)
    indexed.info["transparency"] = 255
    indexed.save(path, format="PNG", optimize=True)


def main():
    parser = argparse.ArgumentParser(description="Generates the program icons from one source image.")
    parser.add_argument("--source", default=SOURCE, help="Source image (default: %(default)s)")
    parser.add_argument("--keep-background", action="store_true", help="Do not make the background transparent")
    arguments = parser.parse_args()

    source = os.path.join(ROOT, arguments.source)
    if not os.path.isfile(source):
        sys.exit(f"Source image not found: {source}")

    image = Image.open(source).convert("RGBA")
    print(f"source: {arguments.source} ({image.width}x{image.height})")

    if not arguments.keep_background:
        image = remove_background(image)
        print("background: removed")

    for path, write in [
        (ICO_TARGET, lambda target: write_ico(image, target)),
        (LINUX_TARGET, lambda target: write_png(image, target, LINUX_SIZE)),
        (WINDOW_TARGET, lambda target: write_indexed_png(image, target, WINDOW_SIZE)),
        (WIZARD_TARGET, lambda target: write_png(image, target, WIZARD_SIZE)),
    ]:
        target = os.path.join(ROOT, path)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        write(target)
        print(f"written: {path} ({os.path.getsize(target)} bytes)")


if __name__ == "__main__":
    main()
