# Player wall cling

Created with the built-in image generator using the original hero sheet and portrait as identity references. Unity preparation reduces the source to a four-frame 128x128 atlas, with 64x64 cells, a 43-pixel maximum character height, a 20-color palette and 100 pixels per unit to match the existing player. The pivot aligns the palm with the capsule side; SpriteRenderer flip mirrors the same pose for the other wall. The loop lasts 0.6 seconds. Final asset: `Assets/Art/Player/WallCling.png`.

## Final generation prompt

Use case: identity-preserve
Asset type: transparent four-frame pixel-art wall cling / wall slide sprite sheet for the existing Unity hero.
Input images: Image 1 is the exact hero sprite sheet and style reference; Image 2 is the exact hero face/hair/scarf reference. Preserve this character, not a new hero.
Primary request: create four subtle looping wall-brace frames of this same male swordsman seen in strict side view facing RIGHT toward an imaginary vertical wall. One hand and the forward boot brace against the wall to the right. Bent knees, torso held close to wall, head upright looking toward wall, free hand holds the curved sword low safely away from wall. Red scarf/cape hangs and gently flutters to the left. Four frames vary breathing, elbow and knee flex and scarf tip slightly; anchored hand and front boot contact points stay fixed. It must read as supported wall sliding, not falling, running, crouching on floor, or climbing a ladder.
Character identity: black hair tied into a short topknot/ponytail, tan face, red scarf/short cape, dark charcoal sleeveless clothing and trousers, brown boots, curved silver sword. Match the original slim proportions, small head, restrained colors and minimal face detail. Avoid bulky armor, helmets, new outfit or hair color.
Style: authentic tiny pixel art appropriate for a 23x43-pixel original hero, about 28 pixels wide by 43 pixels tall per character, limited 16-color palette, hard square pixels, no anti-aliasing or smooth gradients, no glow.
Layout: exact 2 by 2 grid of four equal square cells, each entire character centered at equal size and same vertical alignment, generous transparent padding around each, all face right, no grid/gutters/text or labels. True transparent background. No actual wall or ground, no dust, no shadow, no extra characters or objects. Source will be mechanically reduced to small game frames; use simple clear pixel clusters and keep identity consistent.
