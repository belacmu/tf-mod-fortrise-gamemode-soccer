# Soccer


<img width="420" height="240" alt="soccersoccer_232513_round_10" src="https://github.com/user-attachments/assets/f7e7f6c8-efaa-4ffc-b9dd-679b83530fd3" />

<img width="640" height="400" alt="soccersoccer_123235_round_00" src="https://github.com/user-attachments/assets/f7cb0fe2-cd74-45de-a0bf-7d3eebeff234" />

A **football** versus mode for TowerFall, on FortRise 5.

Not a single arrow: one ball, two goals, two teams. You take the ball by walking over
it, you carry it in front of you, you kick with the shoot button - the harder the longer
you held it - and you lose it when someone shoves you sideways. A goal ends the round.

A mod for **FortRise 5** (>= 5.3.3).

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. Copy `release/tf-mod-fortrise-gamemode-soccer` into `<TowerFall>/FortRise/Mods/`.

## Playing

<img width="687" height="670" alt="image" src="https://github.com/user-attachments/assets/1bf29855-6b34-454b-87b9-3854fd80286d" />

**SOCCER** joins the list of versus modes, next to Last Man Standing and Team
Deathmatch. It is a **team mode**: the game splits the archers into blue and red on the
select screen, and counts points per team.

**Two players are enough** - a team of one is still a team - and it goes up to as many
slots as the game has: four normally, **eight with WiderSet**.

> Eight really means eight. Two spots still assumed four: the spawn loop, which left
> players 5 to 8 without an archer on the pitch, and the charge array, which threw an
> `IndexOutOfRange` the moment a fifth player touched the ball.

| | |
|---|---|
| **Blue** | defends the left goal |
| **Red** | defends the right goal |

### The ball

- It drops in the **middle of the pitch** at the start of the round.
- **Walking over it** takes it. It then sits in front of the archer, at foot height, and
  follows the way they face: you can see at a glance who has it and where they will kick.
- **The shoot button** kicks. Held, it charges: the bar above the archer runs from white
  to red, and the power follows how long you held. The direction is the aiming direction
  - eight ways, or free with the game's *Free Aiming* variant.
- Once kicked, the ball flies **like an arrow**: gravity, bouncing off walls, rolling on
  the ground, and it comes back in on the far side when it leaves through an open edge.
  On a **flat pitch** it does not fall: it brakes on both axes instead of one, and bounces
  off the top and bottom walls the way it already bounced off the sides.
- It cannot be picked up again for about a dozen frames after a kick, or the kicker -
  still standing on it - would catch it straight back.

### Taking it off someone

Nobody can kill anybody: there is nothing to do it with. The ball changes feet by
contact, at the place where the game already makes an arrow change hands:

- **Shoving** someone sideways takes it, provided you are pushing *towards* them. If
  both push towards each other, nobody takes anything: they bounce apart, and that is
  all.
- **Stomping** someone - landing on their head - takes **nothing**. It is a bounce and it
  stays one. Making it steal the ball would have turned jumping on people into the
  easiest way to get it back, and soccer would be played in the air.

That split is not arbitrary: `Player.PlayerOnPlayer` is exactly where the game already
tells a stomp from a shove, and where it already makes an arrow fly from one archer to
the other. The ball follows the same rule, in the same place - nothing to guess about
speeds or hitboxes.

### Scoring

A ball that enters a goal **ends the round** and gives the point to the team opposite
the one defending that goal - so an own goal counts for the other side, as in football.
A ball *carried* into the goal does not count: it has to be kicked.

Scoring, the results screen and the crown are the game's own: the round is built on the
skeleton of `TeamDeathmatchRoundLogic`, which is what everything else expects.

## Goals on a tower that was never drawn for them

A goal at a fixed position would only work on towers with open sides; anywhere else it
would end up buried in stone, and the ball could never get in.

So the position **is not negotiated**: a goal sits at the end of the pitch, at
mid-height, the two facing each other. Whatever tiles are in the way are **carved out** -
the cage is cut into the stone, exactly the way a beam punches through a wall in the
Power mod. A cell is switched off on both sides: in the `Grid`, which carries collisions,
and in the `bitData`, which carries the drawing, because Monocle's `Grid` constructor
clones the bit array. Then `ReloadTiles()` re-tiles the map.

An earlier version looked for a spot that was already free. The goals ended up wherever
the tower would let them - at floor level, sometimes at two different heights - and the
pitch was not symmetric.

**Three cells are cleared in front of the cage**, on the pitch side. The two-pixel margin
was enough to place the goal, not to reach it: on a tower with a wall running just in
front, the cage was cut into the stone and still unreachable - a goal you could see but
neither aim at nor cross. Three, and no more: the pitch has to stay a TowerFall pitch,
with its walls and its platforms.

### Why the goals are not flush against the edge

They are inset by **one tile**, and the strip behind them is **walled in**.

The screen wraps. A goal flush against the edge turns its own net into a door: leaving on
the left means coming back on the right, which amounts to walking through the goal. The
AIs were not cheating - they were using a passage that really existed.

The back wall runs **one cell above and below** the cage: without that, an archer passing
just over the post would come back on the other side of the screen, and the goal would
still be a door.

Nothing in there knows the geometry of any tower: it all goes through the tile grid, so a
tower added by another mod works too, and a level widened by WiderSet places its goals at
*its* edges without this code knowing.

The **ball** is born midway between the two goals, up high. That point is not always
empty - a platform, a column - and it used to be born inside the stone: you could not
see it, and you had to break the wall to get it back. It now looks for the nearest free
spot, in square rings, on the level's first frame: at the moment it is added, the solids
are in the same batch of entities as it is, and the order between them is not ours.

## What is switched off

- **Arrows**: the quiver is emptied when the level loads, and `ShootArrow` never creates
  an arrow - the button is the kick.
- **Everything on screen that talks about arrows**: the counter above the head, the bow,
  and the aimer that follows the aiming direction. The gesture is the same as a shot, so
  the game used to draw a nocked arrow on every kick: it announced a shot that would
  never come.
- **Chests**: they would only give arrows and things that kill.

The tower's traps stay: a lava or spike tower can still kill. An archer who dies drops
the ball, and if a whole team falls the other one takes the round - otherwise the match
would be stuck on a pitch with nobody to play.

## What is drawn

- An **arrow** above the carrier: at a glance, who has the ball.
- A **power bar** above them while charging, white to red. It used to be under their
  feet, which meant inside the ground - an archer's origin *is* at scenery level, and
  twelve pixels lower put it out of sight.
- A **direction marker** in front of anyone charging or carrying, replacing the game's
  aimer - see [Where the kick will go](#where-the-kick-will-go).

## The pitch, and playing flat

Three settings, all **on by default**, in **Options > Mods > Soccer** — or, better, on
**Y** from the SOCCER button of the versus screen, without leaving the menu. That popup is
the same gesture as in the survival, tag and handicap mods, and it earns its place here
more than anywhere: you do not know in advance whether you prefer the stadium or a real
tower, nor whether weightlessness suits you, and the only way to find out is to alternate
from one match to the next.

| Setting | What it does |
|---|---|
| `Soccer field` | replaces the tower with a walled, empty stadium and paints a pitch on the floor |
| `Top-down play (no gravity)` | no falling, no ducking: you move in four directions as on a board seen from above |
| `Precise aim` | the kick leaves at the exact stick angle, with no rounding to eight directions |

**They are separate although they go together**, and that is deliberate. Emptying the
tower into a stadium is a safe change: you know what it looks like. Removing gravity
changes the nature of the game — it is the experiment, the one nobody knows yet whether
they like. Merging them into a single switch would force you to give up the stadium in
order to give up weightlessness.

### The stadium

The tower is **not replaced, it is rewritten**. It is already loaded, with its collision
grid, its tileset, its spawn points and its background: the cells are cleared, the border
is switched back on, and the game is left to repaint. Which tile goes where depends on its
neighbours, so the walls pick up the edges, corners and wear of the original tower. A
rectangle drawn by hand would have none of that, and it would show.

Same mechanism as carving the goal cages: the cell has to change on **both** sides — in
the `Grid`, which carries collisions, and in the `bitData`, which carries the drawing —
because Monocle's `Grid` constructor clones the bit array. Change only one and you get
either a wall you walk through or a wall you cannot see.

### Kick-off in formation

The tower's spawn points stop making sense on a stadium: they were placed relative to
platforms that no longer exist, and the ones that hugged an edge end up inside the
perimeter wall. Each team is laid out as a football team instead, **ordered from the most
advanced to the most withdrawn**:

| Players | Who is on the pitch |
|---|---|
| 1 | a striker, on the centre circle |
| 2 | striker and keeper |
| 3-4 | plus the two full-backs |
| 5-8 | plus midfielders, rather than a second keeper |

That order is what makes the formation degrade properly when you are fewer: never a keeper
with nobody in front of them. Distances are **fractions** of the half-pitch, not pixels,
so a stadium widened by WiderSet places its players correctly without this code knowing.

### Flat play

The lever is `Player.OnGround`, and no other was needed. In `NormalUpdate` all of gravity
lives in one branch — `if (OnGround) { … } else { … Speed.Y = Approach(…) … }`. An archer
the game believes is standing is never pulled down: no fall, no wall slide, no ledge grab,
they all live in that same branch. Gravity is not disabled; you stand on the side where it
does not apply, which is the only way not to fight it every frame.

The vertical then becomes a **speed like the horizontal**: `Speed.Y` is written from the
player's intention at 1.5 px/frame, the same as the game's run. There is no reason to be
slower going up than sideways on a board seen from above, and a difference would read
immediately as a bug.

Everything happens **around `NormalUpdate`**, and the timing is the whole point. A fix
applied after `Player.Update` would do nothing: `Update` recomputes `OnGround` from
`GetBelow()` *before* running the state machine, so a value written at the end of one
frame is erased at the start of the next, before anyone has read it.

Two things the lever gets wrong on its own, both intercepted:

- **Ducking.** `InputDucking` means exactly "on the ground, no horizontal input, pressing
  down" — which is half of what you want to be able to do. The value `NormalUpdate` hands
  back to the state machine is converted, so pressing down alone walks down instead of
  crouching.
- **Aiming.** Holding shoot used to pin you in place. You can now move in all four
  directions while charging, which is what makes passing while running possible at all.

Dodge and the other states are untouched: they do not go through `NormalUpdate`, a dodge
never had gravity anyway, and an archer frozen before kick-off must stay frozen.

### Jumping, on a pitch with no gravity

Jumping still works, and it is not decoration: **it is how you step over a rolling ball
without picking it up** — the ball checks whether the archer is mid-hop before letting
itself be taken. A tackle, in reverse.

It is written by hand precisely because gravity is gone. The game's jump sets an impulse
and lets the fall do the rest; with no fall the archer would have left upwards and never
come back. The starting height is remembered, our own gravity is applied for the length of
the flight, and the archer is set back down on exactly that line — through `MoveV`, not a
direct write, because the game's movement stops on walls and a raw write would seal the
archer inside one.

### Where the kick will go

The game's aimer is an **arrowhead**: it announced a shot that never comes in this mode,
and it appeared on every kick. Hiding it took away the only thing that said which way you
were about to strike — and on a flat pitch, where aiming covers a full 360°, that is the
single most useful thing on screen.

So a direction marker replaces it: a mark set out from the archer with a tail back towards
them, growing as the kick charges. **One entity for every archer**, not one attached to
each: it is added once per round and walks the players, so there is nothing to clean up
when someone dies and nothing to reconnect when they come back. It only appears when it is
useful — while charging, or while carrying the ball. Shown permanently it would be eight
arrows announcing eight gestures, seven of which will not happen.

### Precise aim

This matters far more at football than with a bow. A pass fifteen degrees out finds
nobody, whereas an arrow fifteen degrees out still hits. This is **not** the game's *Free
Aiming* variant: the player's own settings are left alone, and the question the game asks
itself when aiming (`PlayerInput.GetAimDirection`, whose `snap` argument decides the
rounding) is simply answered on their behalf.

Everything else stays fixed in code: `Ball.cs` for gravity, bouncing and braking,
`SoccerMatch.cs` for kick power, `Goal.cs` for goal size.

## API for other mods

The mod exposes `ISoccerApi`, enough to play with the ball without knowing the mode:

| Member | What it gives |
|--------|---------------|
| `IsSoccerMatch()` | is the current match a soccer match |
| `BallPosition()` | where the ball is |
| `BallCarrier()` | who carries it, or `-1` if it is loose |
| `TargetGoal(playerIndex)` | the goal this player must score in - the far one |

The goal is given **per player** and not per team: the caller knows which archer it is
talking about, not which colour, and that mapping belongs to this mod.

It is used by **AIJimmy**, which goes for the ball and then for the far goal. Without
it the AI chased the nearest player and pressed shoot - that is, it kicked at thin air,
the ball being somewhere else.

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |

Paths (game folder, module name) are set in `script/config.bat`.
