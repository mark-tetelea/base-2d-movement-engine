# Where the kit is now

> Rewritten, never appended. True as of **2026-11-15**, end of phase 14 (make everything swappable).

## How it starts

Press Play on `Assets/Scenes/StartMenu.unity` (first in the build list): a title and four
buttons - **Playground**, **Arena shooter**, **Little RPG room**, **Dungeon**. In every demo **Esc**
pauses (the game stops; Resume, Restart, Save, Load, Controls, Main menu). **Controls** lists every key
(two per action); click one and press a new key or mouse button - the HUD's help line follows. Save and Load open a list of
3 slots (scene and time, or empty); loading a slot from any demo goes to the demo it was saved in.

## The Playground

`Assets/Scenes/Playground.unity` (the old test arena). A blue placeholder player in a stone
arena of four rooms: A (start), B to the right (its doors lock once you are inside), big
room C above, and area D reached by a brown warp pad in C. Along the top of A: a yellow
speed pad (x1.6), brown mud (x0.5) and a cyan ice patch. In A: a chest (a key and 10 coins),
the green Guide to talk to, a row of coins, and to its right one item per pickup mode: a potion
(walk onto it), a gem (pulled in from 4.5 tiles - its own magnet reach), a crown (press E; it
cannot be dropped) and a coin set to "press E" in the scene instead of its usual magnet. In D: two
green wanderers to shoot (they may drop a coin, three coins or a heart) and a
red grid-step chaser that hops one tile at a time and goes round walls. In the upper part of C: a
group - a crowned leader wandering with three red chasers in a V behind it (they break off and
chase you when you come within 4 tiles) - and a brown trail enemy that leaves a fading trail of
red goo (1 damage a second to stand in).

The **tile garden** in the lower left of C has every kind of ground: a red pool (kills), a
blue strip (water you cannot walk through but can shoot over), a crumbling bridge across a
red moat (it cracks when stepped on, breaks 0.75 s later into a deadly hole, is back 3 s
after), a door in a short wall opened by a lever, and a door opened with the chest's key.
B's north door is door ground too: it shuts while B is locked and opens when B is cleared.

A **settings panel** on the left (Tab hides it) shows every choice the kit has, each as a
button; a click moves to the next value, live:

- Feel: Instant (like Deltarune) / Glide (like Diep.io) / Ice / Heavy
- Grid step: off / Grid (one tile at a time, hold to walk) / Grid 8 (diagonals too) /
  Grid tap (one press, one tile)
- Dash (Space): on / off - Facings: 4 / 8 - Face towards: walk / aim - Aim from: mouse / walk
- Camera: Open / Rooms slide / Rooms fade - Weapon: Pistol / Shotgun / Rapid / Sword / Charger / Bouncer
- Aim assist: off / 0.35 / 0.7 (bends each shot toward the nearest enemy in a 15 degree cone)
- Best target: facing / closest - Use: press / hold - Reach: 1 / 1.5 / 2.5 tiles
- Magnet: off / 2.5 / 5 tiles - Room: shows the room; click clears a locked one
- Hit-stop / Particles / Sounds: on / off - Volume: 100% / 50% / 25% - Day/night: Off / Fixed
  (a blue night tint) / Cycle (day, evening, night, morning over 60 s)

WASD move, mouse aims, left click shoots (not when the click lands on a screen), R reloads,
1-6 pick a weapon (Sword swings an arc; Charger: hold to charge, release to fire, up to x4
damage; Bouncer: bullets bounce off walls 3 times), E uses, K knocks the player away from the mouse,
Q drops one of the hotbar item, I opens the bag (click a slot to drop that stack). The HUD shows health,
weapon and ammo, the hotbar (click a slot) and the controls.

## The Arena shooter demo (phase 4)

`Assets/Scenes/ArenaShooter.unity`. Glide feel, faces the mouse, rooms slide. WASD, mouse aim,
left click shoots, R reloads, 1-3 or the wheel pick Pistol / Shotgun / Rapid, Space dashes
(invulnerable). Walking into B, C or D shuts the doors and sends waves (red chasers, purple
shooters, green wanderers); the last wave opens them. Chasers and shooters walk round pillars and
walls (shooters go round a wall that blocks their shot). Clearing B opens a door in its east wall
to **room E, the boss room** (pillars and a wall in the middle): a big horned blob with a health
bar at the top of the screen, three phases - bouncing 3-shot spread, then slow charged shots from
60% health, then from 30% it rushes you and swings a wide sword. HUD: health bar, weapon and ammo,
"wave 2/3", the boss bar, "Arena cleared - you win" (all of B, C, D and E), "You died" (the scene
restarts after 1.5 s).

## The Little RPG room demo (phase 5)

`Assets/Scenes/LittleRpg.unity`. Instant feel, 4 facings, rooms fade. E uses the best target
in front (an "E" shows over it), 1-5 pick a hotbar slot, Q drops one, I opens the bag (click a
slot to drop it). Talk to the Guide, pick up coins (magnet 2.5 tiles), find the key in a chest in
C, open the pink door, take the crown in B to win (the crown cannot be dropped). Chests open a
transfer screen (click a slot, T takes all, E closes); the hermit's chest in D throws its coins
out on the floor instead the first time. A dropped item is not taken straight back: walk away
from it and come back. A plum
**shopkeeper** in A sells potions (4 coins, 5 of them) and gems (15 coins, 2); click a line to buy
one, click a bag slot to sell one back for half (only what the shop sells). Potions and gems do
nothing yet.

## The Dungeon demo (phase 11)

`Assets/Scenes/Dungeon.unity`. Plays like the Arena shooter (glide, faces the mouse, rooms slide,
Pistol / Shotgun / Rapid, dash), but **the level is new every run**: 7 screen-sized rooms joined
by doors, built from hand-made room pieces - a start room with crates, Pillars, Storeroom (lots of
crates, wanderers), a long Hall (two screens wide), Pool (blue water to shoot over, red corners), a
tall Tower (two screens high), and always last, as far from the start as the layout allows, the
Boss room with the horned blob. Walking into a room shuts its doors (door ground) and sends two
waves; clearing it opens them. **Crates** (wooden boxes) block walking and bullets; three hits break
one, and it may drop a coin, three coins or a heart; enemies (not the boss) drop the same way. HUD status: "Rooms 2/6  Coins 3  Seed 12345"
(the seed names the layout); "Dungeon cleared - you win" when the boss room is cleared; death or
Restart gives a new layout. A fixed seed on the generator gives the same layout every time.

## Sounds, effects, hit-stop, tint (phase 12)

Every scene, the start menu included, has placeholder sounds: steps, shots, sword swings, dash,
hits, a sword hit, the player getting hurt, deaths, a crate breaking, pickups, doors (room doors,
the key door, levers), crumbling ground and UI clicks. Hits, deaths, dashes, pickups, crate breaks
and crumbling ground throw a few pixel bits. Sword hits, the player getting hurt, deaths and crate
breaks freeze the game for a moment (0.04-0.1 s); bullet hits do not. The day/night tint is off in
every demo; the Playground's settings panel turns it on. No music (the slot is empty).

## Saving, the map and touch (phase 13)

Every demo saves and loads from the pause menu: where the player is and their health, the bag
and coins, chests, opened doors and levers, cleared and visited rooms, pickups taken and items
lying loose, shop stock, and the scene (the Dungeon comes back with the same layout). Sound and
map/touch choices are kept between runs on their own. A broken save file falls back to its
backup; a file from a newer version is refused; nothing is ever overwritten that is not a good
save.

The three demos show a small **map** in the top right (rooms appear once visited, a white dot
for you, red for enemies, gold for items); **M** or a click on it opens the whole map. The
Playground's map is off (settings rows "Map (M)", "Map reveal").

**Touch controls** appear by themselves on a phone or after a finger touches the screen (the
Playground's "Touch" row forces them on, to try with the mouse): a move stick bottom left, an aim
stick bottom right (pushing it fires), Fire, Dash, Use, Gun (next weapon) and a pause button.

## Swapping and removing (phase 14)

Every key is a setting (`Assets/Resources/KitControls`) and the player can change them in the pause menu;
the change is kept with the other settings. The look of every screen comes from one theme asset
(`Assets/Resources/KitTheme`: colours, font, box / button / slot / bar art, where each HUD part sits); a
pixel font drawn for the kit is ready in `Assets/UI/KitThemePixel` (not switched on). A character's art is
one animations asset (the player: `Assets/Data/Art/PlayerAnims`, idle and a 4-frame walk; any action, any
number of frames, any speed). Dropped items can have their own floor prefab. Each system is its own folder
that can be deleted (the README's table says what goes with what); the menu **TopDownKit > Check removable
systems** proves the rest still compiles. Nothing looks or plays differently from phase 13b in the demos.

## Screens

One set, used by all four: a HUD (health bar, weapon + ammo, hotbar, a status line, a centre
banner, a help line - each demo shows only what it has), the text box, the chest screen, the
bag screen, the shop screen, the pause menu and its save/load slot list, the map, the touch controls, the start
menu, and the Playground's settings panel. Placeholder look: dark
Endesga 32 boxes, the default font, drawn in the game's 640x360 pixels and scaled by whole
numbers like the game (x3 at 1920x1080, x2 at 1366x768).

## What is measured

Same keys give the same movement step for step at 30 and 144 fps; combat too; grid steps
land exactly on tile centres and on the same tiles at both. Each tile rule is checked: red
kills (a dash over it protects until it ends there), blue blocks walking but not bullets,
crumbling breaks and returns on time, door ground opens by lever, key and cleared room. Reused bullets
cost ~4 us and ~150 B against ~24 us and ~4 KB. Bots clear the Arena shooter (boss room included; enemies
walk round walls) and finish the
RPG room. The UI scale matches the camera at 1920x1080 and 1366x768, and a click on the
settings panel does not shoot. Save then load gives back exactly what was saved (RPG room, Dungeon, and loading
one demo's slot from another), and broken, cut-down or newer save files never crash or lose the
last good save.

## What the kit has (the user's list)

1. Movement - 8 directions, instant-to-glide, Ice/Heavy, dash, knockback, speed zones: **built**.
2. Facing and animation - 4 or 8, aim separate from walking: **built**. Camera and rooms: **built**.
3. Interaction - one key, best nearby target: **built**.
4. Pickups and inventory - hotbar, chests as inventories: **built**. Phase 13b: each item picks
   how it is taken (walk onto it / press E / magnet, magnet reach and speed per item; a scene object
   can override), every source puts items on the floor the same way (chests that spill, the shop's
   floor delivery, crate and enemy drops, dropping), and the bag screen drops any slot (a per-item
   "droppable" switch).
5. Shooting - weapons, reused bullets, health, damage, recoil, tiny enemies: **built**.
6. Screens and the Playground picker: **built** (phase 6).
7. Ground tiles (normal, red, blue, crumbling, door) and grid-step movement: **built** (phase 8,
   round 2). Only the Playground uses them; no demo does.
8. Combat additions - melee swing, charge shots, bouncing bullets, aim assist: **built** (phase 9).
   Only the Playground has the Sword, Charger and Bouncer; the demos keep their weapons.
9. Enemy additions - pathfinding round walls (free and grid-step walkers), a boss with phases and a
   HUD bar, groups that move together, a trail enemy: **built** (phase 10). The boss is in the Arena;
   groups, trail and grid-step enemies are in the Playground.

10. World additions - generated levels from room pieces (start, end, allowed pieces, room count,
   seed), destructible crates with a drop table: **built** (phase 11). Only the Dungeon uses them.
11. Feel additions - hit-stop per hit kind, effect slots, swappable sound slots with placeholder
   sounds and volumes, day/night tint: **built** (phase 12). All scenes.
12. Systems - save and load (opt-in per system, slots, backups), a coin shop, a minimap, touch
   controls: **built** (phase 13). Save, map and touch in all four demos; the shop only in the RPG room.
13. Swappable everything - keys (with a rebind screen), UI theme and a pixel font, animations per action,
   item floor art, sounds only through slots; every optional system deletable, checked: **built** (phase 14).

## What is not done

- Every playtest: movement feel (Instant vs Glide, wall slide), facing (8 directions, walk
  speed), combat numbers, the RPG room (reach, facing vs closest, magnet), and now the screens
  (sizes, readability, the settings panel), and now grid step (pace, slide time) and the
  tiles (crumble timing, how forgiving red is), and the phase 9 weapons (swing, charge, bounce, assist), and the phase 10 enemies (boss difficulty, group spacing, trail damage), and the Dungeon (room sizes, wave difficulty, how often crates drop), and the sounds, bits, hit-stop lengths and tint colours, and now the shop prices, the map and the touch controls (never touched with a finger), and the pickup modes (gem reach, how far away "walk away first" is, the bag screen's look), and the rebind screen and the pixel font. Nobody has played or heard any of it yet.
- Real art for everything in `TO-DRAW.md` except the floor, wall, player and aim marker.
- WebGL build (module not installed), GIFs (only PNG screenshots in `Media/`), GitHub repo and push (the user's call).
