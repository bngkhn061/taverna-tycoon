# Playtest Report

## Session Info
- **Date**: 2026-11-09
- **Build**: Windows dev build, Unity 6000.6.8f1 (built 2026-11-09, includes goal-box text and book-scrollbar USS fixes)
- **Duration**: Not reported
- **Tester**: bengokhan (developer)
- **Platform**: PC (Windows test build; target platform is mobile)
- **Input Method**: Not reported
- **Session Type**: Returning (developer playtest of a build with known open UI issues)

## Test Focus
General look-and-feel pass of the current build: main menu, UI, audio. Notes were
given as four short remarks, not a structured session.

## First Impressions (First 5 minutes)
- **Understood the goal?** Not reported
- **Understood the controls?** Not reported
- **Emotional response**: Not reported (all four notes are negative on presentation)
- **Notes**: Tester's verbatim notes (Turkish):
  1. "şimdi ana menüdeki arka planı değişmemiz lazım biraz daha eğlenceli bir arkplan ve aktif bi arka plan hareketli olacak"
  2. "ui silinip baştan taşarlanması lazım"
  3. "ses efektleri çok kötü baştan yapılması lazım"
  4. "ai sloop gözükmemesi lazım"

## Gameplay Flow
### What worked well
- Not reported

### Pain points
- Main menu background is boring and static — Severity: Medium
- UI as a whole is unsatisfactory; tester wants it scrapped and redesigned — Severity: High
- Sound effects are poor; tester wants them redone from scratch — Severity: High
- The game must not look "AI slop" — Severity: UNCLEAR (see Open Questions)

### Confusion points
- Not reported

### Moments of delight
- Not reported

## Bugs Encountered
| # | Description | Severity | Reproducible |
|---|-------------|----------|-------------|
| — | No defects reported | — | — |

## Feature-Specific Feedback
### Main menu background
- **Understood purpose?** Not reported
- **Found engaging?** No
- **Suggestions**: More fun background; it must be active and animated.
- **Design conflict**: `GAME_DESIGN.md` §10 specifies "canlı taverna arka planı (Barlo ve Pip)" (live tavern background). The build shows one static image (`MainMenuController.background` → `Menu_Background`). The design intent is not implemented.

### UI
- **Understood purpose?** Not reported
- **Found engaging?** No
- **Suggestions**: Delete the UI and redesign it from scratch.
- **Design conflict**: Changes the approved landscape UI v2 (`design/ui/ui_v2_landscape.html`) and the "wood sign + parchment" language in `DESIGN_RULES.md` §9. Project rule from the tester (2026-11-08): UI changes are shown as mockups before they are applied. Scope (menu / HUD / panels / all) not stated.

### Sound effects
- **Understood purpose?** Not reported
- **Found engaging?** No ("çok kötü")
- **Suggestions**: Redo all sound effects from scratch.
- **Design conflict**: Current audio is code-synthesized sine waves (`Scripts/Core/Sfx.cs`, 9 effects: Coin, Pop, Serve, Buy, LevelUp, Rush, Tick, Claim, Angry), documented there as a placeholder. `design/game-brief.md` lists "Gerçek müzik ve ses kayıtları" under Out of scope; this note moves it into scope.

## Quantitative Data (if available)
- Not reported

## Overall Assessment
- **Would play again?** Not reported
- **Difficulty**: Not reported
- **Pacing**: Not reported
- **Session length preference**: Not reported

## Top 3 Priorities from this session
1. Redesign the UI (after approving mockups first).
2. Redo the sound effects.
3. Replace the static main menu background with an animated one.

## Open Questions
- ~~"ai sloop gözükmemesi lazım" — which part looks AI-generated?~~ **Answered 2026-11-09:** buttons and boxes, and icons. Tester added (verbatim): "tavern master oyunundaki grafiklere biraz daha yaklaşmalıyız gibi özellikle mekan tasarımında çok küçük kalıyo mekan".

## Follow-up findings (2026-11-09, same tester)
- **Venue too small** — Design change. The tavern space feels small; tester wants the venue design closer to Tavern Master's (genre reference, not a copy). Affects `GAME_DESIGN.md` §3.2 / §3.2.1 (map: Meyhane 6×5, Ana Salon 8×5, Bahçe 8×5) and the camera. Route: `/propagate-design-change GAME_DESIGN.md` before changing the map.
- **UI redesign scope** — all screens (menu, HUD, shop, upgrade panel, quests, settings, level-up, offline). The current wood-plate/parchment buttons, boxes and icons read as generic and must be replaced. Mockups first.
- **Main menu background** — done 2026-11-09: live 3D diorama, approved by tester ("Olur"). Evidence: `production/qa/evidence/2026-11-09-live-menu-*.png`.

## Session 2 — Android phone (APK v0.5, UI v3), same tester
- Touch joystick, button size, text readability: **all fine** ("3 üde iyi sorun yok").
- Tester's notes, verbatim (Turkish):
  - "görevliler belli olmuyor"
  - "dekoru kişiselleştirme duvarlar zeminler bunları mağazadan satın alıp dükkanımızı güzelleştirebilelim"
  - "Zemin asseti kötü bazı noktalarda bazı şeyler parlayıp sönüyo"
  - "çok fazla yakınlaştırılıp uzaklaştılırıyo"
  - "reklam basma özelliği olsun daha çok müşteri gelsin"
  - "interaktif gece/ gündüz özelliği gelsin"
  - "oyun çok kolay biraz başta zorlaşsın insanlar oynamak istesin ekonomiyi ona göre dengeleyelim"
  - "mekan biraz daha büyük olsun çok sıkışık fazla görevliyle kötü olur"
  - "oyuncu sürekli gelişmek istemeli"
  - "bazen oyuncuya para harcatmalıyız veya reklam izletmeliyiz mesela video izleyerek 2. garsonu çağırabilrsin veya garsonların/ görevlilerin upgradeleri olsun onları hem reklam ile hem para ile artırabilsin"
  - "içecekler bitsin sipariş verebilelim öyle yeniden dolsunlar içecek depomuz felan olsun"
  - "ve biraz daha özellik ekleyelim oyun hemen bitmesin"

### Bugs
| # | Description | Severity | Reproducible |
|---|-------------|----------|-------------|
| 1 | Floor flickers ("parlayıp sönüyor") at some spots — likely z-fighting between overlapping flat meshes (floor tiles / rug / grass / pavement) | Medium | Not stated (Android) |

### Design changes
- Staff are not distinguishable from customers.
- Camera zoom range is too wide (currently 7–18).
- Early game too easy → rebalance; drinks should run out and be restocked by ordering (stock/depot system).
- Venue bigger, less cramped with many staff (repeats Session 1 "venue too small").
- New systems: advertising (pay → more customers), interactive day/night, wall/floor customization in shop, staff upgrades (coins or rewarded ad), rewarded ad for a temporary 2nd waiter, more long-term content.

### Balance adjustments
- Early economy too generous. Route: `/balance-check` before tuning `Balance.cs`.

## Action Routing
- **Design changes**: UI redesign → run `/propagate-design-change` on `DESIGN_RULES.md` (§9) and `design/ui/`. Animated menu background → implements the existing `GAME_DESIGN.md` §10 intent, no document change needed. Audio → update `design/game-brief.md` Out of scope (real sound moves in scope).
- **Balance adjustments**: None.
- **Bugs**: None.
- **Polish items**: None; all four notes are design-level.

## Creative Director Assessment
CD-PLAYTEST skipped — Solo mode.
