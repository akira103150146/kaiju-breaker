---
name: project-kaiju-breaker-narrative-structure
description: Current state of KAIJU BREAKER's narrative/zone architecture — 5-zone direction, boss mapping, open questions pending director sign-off
metadata:
  type: project
---

KAIJU BREAKER's story direction was greenlit by the director on 2026-07-03: near-future
Earth overrun by Kaiju (魔物), humanity evacuated to orbital ark "HESTIA", mass-produced
cute-but-lethal humanoid mechs ("型號少女" / Model Girls), player pilots one solo through
5 zones + final boss, breaking Kaiju parts, harvesting bio-material, protagonist's true
identity (possibly the sole non-mass-produced prototype) revealed gradually. AI operator
companion ("管制官") on comms, single character (not split into two like the earlier draft).

Full narrative architecture written to `design/narrative/story-and-zone-structure.md`
(2026-07-03, status DRAFT pending director review of 4 open questions in its §I).

**Why 5 zones is not scope creep**: `design/gdd/game-concept.md` already states Full
Vision range as "3-5 關、3-5 巨獸" and `design/systems-index.md` backlog (P3) already
anticipated "更多巨獸(4–5隻達 Full Vision)". 5 zones sits at the top of the pre-existing
range, not a new ask.

**Key structural decision (mine, recommended, not yet director-confirmed)**: reuse the
3 existing fully-designed kaiju/stages instead of discarding them —
- Zone 1 = CARAPEX (existing REEF OUTPOST ALPHA, unchanged, narrative wrapper only)
- Zone 2 = LACERA (existing ABYSSAL RIFT, unchanged — recommended NOT forcing the v0
  feedback draft's "鏽蝕市街/rusted city" theme onto it since that would contradict
  already-written art direction in stage-system.md G.2)
- Zone 3 = NEW boss, working codename "OVIRA" (孵化母體/broodmother, wetlands/hive theme,
  concentric-ring bullets, swarm/group-spawn mechanic) — needs full kaiju design doc
- Zone 4 = VOLTWYRM (existing VOLTAGE SPIRE), **moved from final boss to 4th slot**,
  reframed narratively as the old orbital-elevator reactor core the Kaiju parasitized —
  this reframing needs zero mechanical changes, matches the feedback draft's "軌道電梯
  遺構" zone-4 concept almost exactly
- Zone 5 = brand-new final boss, working codename "GENITRIX", true climax, needs full
  kaiju design doc (kaiju-part-system.md already permits up to 8 parts for late bosses)

**Why VOLTWYRM moved from finale to zone 4**: its existing vertical-pierce-corridor design
and "energy tower" theme fit "軌道電梯遺構" (orbital elevator ruins) far better than the
final "魔物母巢核心" (nest core) theme; freeing the true finale for a fresh boss also
better serves Pillar 2 "頭目是靈魂" (boss is the soul — finale deserves bespoke design).

**Prior draft superseded (partially)**: `design/narrative/story-concept.md` (v0.1,
2026-07-02) was written *before* the 5-zone direction was approved and aligned to the old
3-stage structure. It proposed protagonist name NOVA, mascot PIP + handler CURATOR (two
support characters). The 2026-07-03 task brief calls for a single AI operator, so the new
doc merges PIP+CURATOR into one "管制官" role. NOVA/ZERO/GENESIS are presented as name
candidates (still open) carrying forward v0.1's NOVA idea.

**4 open questions awaiting director sign-off (see story-and-zone-structure.md §I)**:
1. worldview specifics confirm
2. 5-zone theme/order (my recommended mapping above)
3. protagonist naming (NOVA/ZERO/GENESIS) + mystery weight (light vs heavy "same origin
   as Kaiju" reveal)
4. Kaiju art direction — recommended: visually biological/organic (per art-bible.md's
   existing "Tech vs Flesh" law, unchanged), lore-wise bio-mechanical hybrid (already true
   for the existing 3 kaiju) — no visual assets need to change.

**Cross-domain fallout once director confirms**: `game-designer`/level-designer need to
extend `stage-system.md` from 3 to 5 stages; a kaiju-content-designer needs to write full
design docs for OVIRA and GENITRIX (part tables, HP, bullet patterns) matching the format
of `design/gdd/kaiju/01-carapex.md` etc.; `material-economy.md` owner needs to evaluate
proposed new material names (`core_broodmatter`, `core_genesis`/`essence_origin`) before
they're written into `design/registry/entities.yaml` (narrative-director does not have
write access to that registry).
