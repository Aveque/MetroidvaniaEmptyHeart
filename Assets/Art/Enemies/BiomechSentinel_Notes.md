# Biomechanical sentinel

Final game asset: `Assets/Art/Enemies/BiomechSentinel_PixelWalk.png`.
Six frames in a 3 by 2 layout, 64 by 48 pixels per frame, 18 opaque palette colors plus transparency.
Point filtering, no compression or mipmaps, 40 pixels per unit.
All frames share a ground baseline. The visible creature occupies up to 56 by 38 pixels.

Artwork was created with the built-in image generator, then sliced, aligned, reduced with nearest sampling and mapped to the game palette by the Unity editor import preparation in `BuildEchoStationMap`.
The high-resolution generated source is retained as `BiomechSentinel_Walk.png`; scene enemies use only the small pixel atlas.

## Final generation prompt

Use case: stylized-concept
Asset type: transparent 2D Unity metroidvania enemy walk-cycle sprite sheet
Input images: Image 1 is the current biomechanical enemy and is a design reference only; Image 2 is the player sprite sheet and is the required pixel-density/style reference
Primary request: redesign the biomechanical station sentinel as a much simpler low-resolution enemy and create exactly six sequential walking frames
Subject: compact quadrupedal biomechanical crawler, dark metal shell, small muted organic tissue areas, one cyan eye/core, a few restrained red lights; preserve the recognizable hunched silhouette of Image 1 but remove tiny mechanical detail
Style/medium: authentic low-resolution pixel art matching Image 2, chunky readable pixel clusters, hard pixel edges, no anti-aliasing, limited 12-to-16-color palette; each creature should read as roughly 55 by 38 logical pixels before enlargement
Composition/framing: one single sprite sheet in an exact 3-column by 2-row grid, six equally sized cells, reading left-to-right across the top row then left-to-right across the bottom row; all six sprites face right, same scale and center, feet on exactly the same baseline, generous transparent padding inside each cell, no grid lines and no gutters
Animation: complete looping walk cycle with visibly different leg positions, subtle body compression and cyan core pulse; frame 1 contact, frame 2 recoil, frame 3 passing, frame 4 opposite contact, frame 5 recoil, frame 6 passing
Scene/backdrop: genuine transparent background
Lighting/mood: ominous industrial sci-fi but simple and readable at game scale
Constraints: exactly six copies of the same enemy only; no text, labels, UI, ground, shadow, backdrop, border, cell dividers, watermark, extra effects, detached parts, or pose drift; do not render a detailed illustration; preserve clean transparency around every frame
