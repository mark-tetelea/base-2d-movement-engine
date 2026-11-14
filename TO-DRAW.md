# To draw

> One line per drawing: what, canvas, who, done. Grep it (`grep -n 'chest' TO-DRAW.md`).
> Canvases are **proposals** until the system that uses the sprite is built; change them
> then, here.

| Sprite | Canvas | Feet / pivot | Ramps | Who | Done |
|---|---|---|---|---|---|
| Floor tile (test arena) | 16x16 | tile | steel | Claude | yes - `floor.js` (2026-10-18) |
| Wall tile | 16x16 | tile | steel | Claude | yes - `wall.js` (2026-10-18) |
| Player, 8 directions, idle + walk | 16x24 | bottom-centre | blue, skin, steel | Claude | yes - 5 sheets (idle + 2 steps) + mirroring, `player-*.js` (2026-11-14) |
| Aim marker (test arena) | 8x8 | centre | fire, steel | Claude | yes - `aim-marker.js` (2026-11-14) |
| Enemy | 16x16 | bottom-centre | plum, steel | Claude | no |
| NPC | 16x24 | bottom-centre | green, skin, steel | Claude | no |
| Chest, closed + open | 16x16 | bottom-centre | wood, steel | Claude | no |
| Coin (auto pickup) | 8x8 | centre | fire, steel | Claude | no |
| Ammo (auto pickup) | 8x8 | centre | rust, steel | Claude | no |
| Weapon (manual pickup + hotbar icon) | 16x16 | centre | steel, wood | Claude | no |
| Key item (manual pickup + icon) | 16x16 | centre | fire, steel | Claude | no |
| Bullet | 8x8 | centre | fire, steel | Claude | no |
| Interaction prompt | 8x8 | centre | steel | Claude | no |
| Ground: red (deadly) | 16x16 | tile | fire | Claude | yes - `ground-red.js` (2026-11-14) |
| Ground: blue (low barrier, water) | 16x16 | tile | blue, steel | Claude | yes - `ground-blue.js` (2026-11-14) |
| Ground: crumbling - whole, warning, broken | 16x16 x3 | tile | wood, steel | Claude | yes - `crumble*.js` (2026-11-14) |
| Ground: door - open, closed | 16x16 x2 | tile | steel, plum | Claude | yes - `door-open.js`, `door-closed.js` (2026-11-14) |
| Boss | 32x32 | centre | plum, fire, steel | Claude | yes - `boss.js` (2026-11-14) |
| Group leader (enemy with a crown) | 16x16 | centre | plum, steel, fire | Claude | yes - `enemy-leader.js` (2026-11-14) |
| Trail enemy | 16x16 | centre | rust, steel | Claude | yes - `enemy.js --swap plum:rust` (2026-11-14) |
| Ground: trail (goo over the floor) | 16x16 | tile | fire | Claude | yes - `ground-trail.js` (2026-11-14) |
