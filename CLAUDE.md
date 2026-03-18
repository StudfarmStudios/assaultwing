# Assault Wing 2 — Original MonoGame/XNA Codebase

**This directory is read-only reference.** Do not modify anything here. Use it to understand existing gameplay logic, content definitions, and architecture when porting to `aw3/`.

---

## Solution Structure

**Solution:** `AssaultWing.sln`

| Project | Purpose |
|---|---|
| `AssaultWingCore/` | Core game library — all gameplay logic, physics, networking, sound (~512 C# files) |
| `AssaultWing/` | Game executable — menus, UI, entry point |
| `AssaultWingCoreContent/` | Core content pipeline — XML definitions, textures, sounds, music, fonts |
| `AssaultWingContent/` | Game content — 3D models (.X) and their textures |
| `Farseer Physics Engine/` | Bundled 2D physics library |
| `ArenaEditor/` | WPF arena editing tool |
| `DedicatedServer/` | Headless server executable |
| `AWUnitTests/` | Unit tests |
| `UncompressedHost/` | Content compression utility |

---

## Source Code Layout (`AssaultWingCore/`)

| Directory | What's in it |
|---|---|
| `Core/` | Game loop entry (`AWGame.cs`), engine init, pre/post-frame logic engines |
| `Game/` | Base `Gob.cs`, `Arena.cs`, `DataEngine.cs`, `LogicEngine.cs` |
| `Game/Gobs/` | All gameplay object types (Ship, Bullet, Rocket, Bot, Explosion, Wall, Peng, Bonus, etc.) |
| `Game/Weapons/` | Weapon implementations (ForwardShot, LightningGun, PowerCone, Shield, Cloak, Blink, etc.) |
| `Game/GobUtils/` | Ship subsystems (Weapon base, ShipDevice, Thruster, ChargeProvider, TargetSelector, DamageInfo, etc.) |
| `Game/Players/` | Player, BotPlayer, Spectator, Team |
| `Game/Logic/` | GameplayMode, Standings, ArenaStatistics |
| `Game/Collisions/` | CollisionArea, CollisionEvent, CollisionCategories, CollisionMaterial |
| `Game/Pengs/` | Custom particle system internals (SprayEmitter, Particle, PhysicalUpdater, curves) |
| `Game/Arenas/` | Arena loading, GobCollection, Navigator |
| `Graphics/` | Viewport, rendering engine, model renderer, HUD overlay components |
| `Helpers/` | CanonicalString, RandomHelper, Geometry (42 KB), AWMathHelper (29 KB), PIDController, TypeLoader, Serialization |
| `Net/` | Custom networking (NOT ported) — NetworkEngine, 18 message types, connection types |
| `Sound/` | SoundEngineXNA, spatial audio |
| `Settings/` | AWSettings, controls, graphics, sound settings |
| `Stats/` | PilotRanking, MultiElo, Steam leaderboards |

### Game Executable (`AssaultWing/`)

| Directory | What's in it |
|---|---|
| `Menu/` | Menu engine, main/equip/arena menu components |
| `UI/` | Input handling, game window management |

---

## Content Locations

### XML Definitions (`AssaultWingCoreContent/`)

| Directory | What | Count | Example filenames |
|---|---|---|---|
| `gobdefs/` | Game object type definitions | 58 | `Ship_Bugger.xml`, `Bot_bazooka bot.xml`, `Explosion_bomb explosion.xml`, `Dock_dock.xml` |
| `weapondefs/` | Weapon type definitions | 23 | `ForwardShot_bazooka.xml`, `Blink_blink.xml`, `Cloak_catmoflage.xml` |
| `particledefs/` | Peng particle definitions | 97 | `Peng_airstreaks.xml`, `Peng_bonus_collect.xml`, `Peng_ship_explo_center.xml` |
| `gameplaymodes/` | Match rule definitions | 1 | `GameplayMode_mayhem.xml` |
| `arenas/` | Arena definitions (XML + BIN pairs) | 5 | `Arena_Junk.xml` + `Arena_Junk.bin`, `Arena_Blood Bowl.xml`, etc. |

### Assets (`AssaultWingCoreContent/`)

| Directory | What | Count | Formats | Examples |
|---|---|---|---|---|
| `textures/` | 2D textures (UI, particles, backgrounds) | 195 | PNG | `alienworld_parallax_background.png`, `gui_playerinfo_bar_main.png`, `lightning.png` |
| `sounds/` | Sound effects + sound definitions | 87 + 1 | WAV, XML | `bazooka01.wav`, `explosion01.wav`, `shieldBlock01.wav`, `sounds.xml` |
| `music/` | Background music | 5 | MP3 | `BG_Amazon.mp3`, `BG_Dark.mp3`, `menu music.mp3` |
| `fonts/` | Font assets | 9 | TTF, spritefont | `Ash.ttf`, `Hotel Coral Essex.ttf` |
| `effects/` | XNA shader effects | 3 | FX | `basicshaders.fx`, `bomber_rage.fx`, `gaussian_blur.fx` |

### 3D Models & Textures (`AssaultWingContent/Content/`)

| Directory | What | Count | Format | Examples |
|---|---|---|---|---|
| `models/` | All 3D models | 136 | DirectX .X | `bugger.X`, `windlord.X`, `plissken.X`, `mine.X`, `dock.X` |
| `textures/` | Model textures | 87 | PNG | `bugger.png`, `windlord.png`, `bloodbowl_axe.png` |

**Model naming convention by arena theme:**
- `alienworld_*` — Alien World arena props/walls
- `amazon_*` — Amazonas arena
- `bloodbowl_*` — Blood Bowl arena
- `junk_*` — Junk arena
- `lumipesu_*` — Lumipesu arena
- `snow_*` — Snow arena (unused/cut?)
- No prefix — gameplay objects (ships, projectiles, pickups)

**Ship models:** `bugger.X`, `windlord.X` (+ `windlord_claw.X`, `windlord_claw2.X`), `plissken.X` (snake ship), `snake.X`

**Note:** `berserker.X` is a projectile model (rocket upgrade), not a ship — there is no Ship_Berserker.xml definition.

**Projectile/weapon models:** `dumbfire.X` (bullet), `missile.X` (rocket), `mine.X`, `bouncefire.X`, `trizooka.X`, `chainend.X`, `straight_chain.X`, `uchain.X`, `snake_bullet.X`, `clustermine.X`

**Gameplay models:** `bonus.X`, `megabonus.X`, `dock.X`, `bot.X`, `fragment.X`, `dust_fragment_*.X`

---

## Key Architecture Patterns (for porting reference)

### Named Bones Convention for Attachment Points

Ship models use named bones with prefix conventions, queried at runtime via `GetNamedPositions(prefix)`:

| Prefix | Purpose |
|---|---|
| `Thruster` | Exhaust particle spawn points |
| `Gun` | Weapon barrel positions (alphabetical sort = barrel index) |
| `Peng_` | Birth-time particle/effect anchors (`Peng_typename_number`) |
| `DockEffect` | Dock visual effect positions |

### TypeParameter / RuntimeState Attributes

- `[TypeParameter]` — serialized from XML template, cloned to instances (= exported Resource properties in aw3)
- `[RuntimeState]` — serialized for network sync (= MultiplayerSynchronizer properties in aw3)
- Unmarked fields — computed/cached, not serialized

### Sound Variations

Sound effects use numbered suffixes: `bazooka01.wav`, `bazooka02.wav`, etc. The engine picks randomly at play time. Mapped in `sounds.xml`.
