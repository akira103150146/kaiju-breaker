# Game Concept: 殲獸戰機 / KAIJU BREAKER

*Created: 2026-06-30*
*Status: Draft*

---

## Elevator Pitch

> 一款科幻縱向彈幕射擊遊戲——你駕駛一架小巧精密的高科技戰機，擊破巨型怪獸的各個部位、蒐集素材永久強化武器，在四階難度中一遍遍狩獵更強的巨獸。
>
> *(It's a sci-fi vertical bullet-hell shooter where you pilot a small high-tech aircraft to break the body parts of giant kaiju, harvest materials to permanently upgrade your arsenal, and hunt ever-tougher titans across four difficulty tiers.)*

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 科幻縱向彈幕射擊（Bullet-hell / STG）＋ 狩獵養成 |
| **Platform** | 多平台（PC + 手機）Cross-platform |
| **Target Audience** | 成就者 × 精通者（見下方 Target Player Profile） |
| **Player Count** | 單人 Single-player |
| **Session Length** | 單輪 20–40 分；可短時段刷關 |
| **Monetization** | 尚未決定（PC 傾向買斷；手機可能 F2P——待定） |
| **Estimated Scope** | Medium（4–5 個月，solo；MVP 約 3–4 週可玩） |
| **Comparable Titles** | Sky Force Reloaded/Anniversary、雷電 Raiden、Drainus |

---

## Core Fantasy

你是一架小而精密的高科技戰機，孤身對抗大而狂野的巨獸。**以小搏大、以智取勝**——你不靠蠻力壓制，而靠讀懂巨獸、選對武器、精準命中弱點，用對的火力擊破比你大百倍對手的每一個部位，看著牠在你面前崩解。

這是「精準 vs 蠻力」的權力幻想：勝利感不來自「我變強了」，而來自「我看穿了你、拆解了你」。在別處你是大艦隊的一員；在這裡你是唯一一架，撕開巨獸的那一架。

---

## Unique Hook

**「像雷電，AND ALSO 把魔物獵人的『破壞部位狩獵循環』裝進彈幕射擊。」**

巨獸有可破壞部位，**破壞同時是技術表現與素材來源**——破得越多、越精準，素材越好，用來永久強化 8 種「等價而異性格」的武器。技巧與養成共用同一個動作：你不需要額外的獎勵系統，破壞部位本身就是經濟引擎。這把純粹的 shmup 爽感，接上了狩獵遊戲的長期黏著。

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 2 | 像素彈幕的爽脆射擊回饋、破部位的視聽爆發、街機式音效 |
| **Fantasy** (make-believe, role-playing) | 3 | 高科技戰機 vs 巨獸的科幻幻想；尺寸與性質的強烈對比 |
| **Narrative** (drama, story arc) | N/A | 劇情刻意輕量——僅作為狩獵的框架 |
| **Challenge** (obstacle course, mastery) | 1 | 四階難度、讀懂頭目模式、破更難的部位、優化 loadout |
| **Fellowship** (social connection) | N/A | 單人遊戲 |
| **Discovery** (exploration, secrets) | 5 | 發掘武器-情境最佳解、巨獸弱點、隨機關卡組合 |
| **Expression** (self-expression, creativity) | 4 | 主+副武器 loadout 搭配；不同打法風格 |
| **Submission** (relaxation, comfort zone) | 6 | 最低難度階提供低壓力刷關入口 |

### Key Dynamics (Emergent player behaviors)

- 玩家會依情境切換 loadout：散射清小怪、集中打小弱點、速射追移動部位。
- 玩家會反覆挑戰同一巨獸，只為破完所有部位、刷到更好的素材。
- 玩家會主動爬難度階尋求挑戰，把彈幕密度當成自我設定的目標。
- 玩家會試驗「主武器 × 副武器」組合，尋找協同打法。

### Core Mechanics (Systems we build)

1. **縱向彈幕射擊與閃避** — 手感核心；移動、射擊、讀彈、貼弱點。
2. **雙武器池系統** — 主武器（雷射系 4 種）＋ 副武器（飛彈系 4 種），兩池不互通，靠場地掉落取得。
3. **可破壞部位巨獸** — 每個部位有獨立判定、狀態與掉落綁定。
4. **素材 → 永久武器強化的養成經濟** — 破部位得素材，跨輪永久升級/解鎖武器。
5. **四階難度系統** — 主要縮放敵彈密度，玩家於開局自選。

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | 選主+副武器 loadout、選難度階、選要優先破哪個部位 | Core |
| **Competence** (mastery, skill growth) | 四難度階梯、破更難部位、武器永久升級＝看得見的成長 | Core |
| **Relatedness** (connection, belonging) | 輕量劇情、孤機對抗巨獸的科幻幻想撐起；不靠厚重敘事 | Minimal |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** — 破完所有巨獸所有部位、升滿全部 8 種武器、爬完四階難度。完成度即驅動力。
- [x] **Explorers** — 發掘武器與情境的最佳配對、巨獸弱點規律、隨機關卡的應對（Supporting）。
- [ ] **Socializers** — 不服務此型（單人、無社群系統）。
- [x] **Killers/Competitors** — 透過自我挑戰高難度，以及夢想層的排行榜/每日挑戰（Supporting）。

### Flow State Design

- **Onboarding curve**: 前 10 分鐘——最低難度第一關教移動與射擊 → 撿到第一把武器 → 打第一個有可破壞部位的頭目 → 破第一個部位立即掉素材並示範升級。一條龍展示核心循環。
- **Difficulty scaling**: 四階難度由玩家自選（敵彈密度縮放）；關內由小怪段漸進到頭目。
- **Feedback clarity**: 部位破壞的視聽爆發、素材入袋提示、武器升級的數值與外觀變化。
- **Recovery from failure**: 街機式快速重來，一輪結束即可再開；失敗是學習頭目模式的過程，不是懲罰。永久養成進度永不丟失，降低挫折。

---

## Core Loop

### Moment-to-Moment (30 seconds)
移動戰機、持續射擊、閃避來襲彈幕、瞄準並命中巨獸的特定部位；途中撿拾場地掉落的武器。射擊手感與閃避的張力是這一層必須「在隔離狀態下就好玩」的核心。

### Short-Term (5-15 minutes)
一個關卡段：清掉隨機組合的小怪波 → 抵達巨獸 → 集中火力破壞部位 → 擊倒。「再一次」心理發生在「這次我要把那個部位也破掉」。

### Session-Level (30-120 minutes)
跑數個關卡，或反覆刷同一隻巨獸；一輪結束後結算素材 → 永久強化/解鎖武器 → 帶著更強的 loadout 挑戰更高難度或下一隻巨獸。自然停點落在每輪結算，且每次結算都製造「下一輪想試新武器」的回頭理由。

### Long-Term Progression
蒐集並升滿全部 8 種武器、破完所有巨獸的所有部位、爬完四階難度。成長以「選項深度＋完成度」呈現，而非單純數值膨脹（見〔橫向選擇〕支柱）。

### Retention Hooks
- **Curiosity**: 尚未解鎖的武器、尚未破壞的部位、更高難度階更密的彈幕。
- **Investment**: 永久武器升級進度——玩家不想浪費已投入的素材與成長。
- **Social**: （弱）夢想層的排行榜與每日挑戰。
- **Mastery**: 破更難的部位、爬難度階、優化 loadout 與走位。

---

## Game Pillars

### Pillar 1: 科技對巨獸 (Tech vs. Titan)
「小而精密」對抗「大而狂野」的對比，是一切視覺、機制與情感的核心。

*Design test*: 在「讓戰機也變得巨大華麗」與「保持戰機小巧、讓巨獸壓迫」之間 → 選後者。

### Pillar 2: 頭目是靈魂 (The Boss is the Soul)
每一關的高潮與記憶點都是與可破壞部位巨獸的對決；小怪是前菜，頭目是主菜。

*Design test*: 在「多做一隻有記憶點的頭目」與「多做兩關小怪內容」之間 → 選頭目（深度 > 廣度）。

### Pillar 3: 橫向選擇 (Sidegrades, not Upgrades)
8 種武器總威力等價而性格各異；強化深化特性，不製造唯一最優解。

*Design test*: 在「讓某武器數值更高」與「給某武器獨特用途」之間 → 選獨特用途。

### Pillar 4: 破壞即獎勵 (Breaking is the Reward)
擊破部位同時是技術表現與素材來源；狩獵完成度驅動養成。

*Design test*: 在「隨機寶箱掉落」與「掉落綁定被破壞的部位」之間 → 選綁定部位。

### Pillar 5: 難度是門，不是牆 (Difficulty is a Door)
四階難度只改變彈幕密度等可及性參數，不鎖內容；爽感對所有人開放。

*Design test*: 在「高難度才解鎖的武器/頭目」與「完整內容人人可達」之間 → 選後者。

### Anti-Pillars (What This Game Is NOT)

- **NOT 長篇分支劇情或大量過場**：會壓縮頭目與打磨時間，違背〔頭目是靈魂〕與時程。
- **NOT 數值最強解武器或碾壓裝備**：會殺死收集的意義，違背〔橫向選擇〕。
- **NOT 純手速門檻把玩家擋在外**：違背〔難度是門，不是牆〕。
- **NOT 開放世界或自由探索地圖**：違背縱捲 shmup 的純粹與時程。（註：**擁抱**隨機關卡元素——隨機小怪波、掉落點、手作關卡段的隨機組合——以服務刷關黏著；但走輕量「手作池隨機組合」路線，而非完整程序生成。）

---

## Visual Identity Anchor

*這一節是美術聖經（`/art-bible`）的種子，捕捉「彈幕永遠讀得懂」這個在後續會議間最容易被遺忘的核心決定。*

- **視覺方向**：像素街機懷舊 (Retro Pixel Arcade)
- **一句視覺鐵則**：「**彈幕永遠讀得懂**」——用有限調色盤的高對比，讓玩家子彈、敵彈、判定點三者在任何混亂中都一眼可辨（致敬 Cave／東方的亮色彈幕慣例）。
- **視覺原則**：
  1. **冷科技 vs 暖血肉（像素版）** — 戰機＝乾淨、少色、冷色像素；巨獸＝大塊、多細節、暖／病態色像素。即使在像素限制下，色溫仍分敵我。🧭 *測試*：任何新素材先問「這是科技還是血肉？」來決定色溫。
  2. **判定點至上的彈幕可讀性** — 敵彈用亮暖色（粉／橘／黃）＋清晰像素外框，玩家判定點恆亮一格。🧭 *測試*：在「華麗特效」與「子彈看得清」之間 → 選看得清。
  3. **尺寸壓迫** — 巨獸是螢幕級的大像素 boss（致敬 R-Type／Blazing Star），戰機保持小。🧭 *測試*：在「戰機變大變帥」與「巨獸更有壓迫感」之間 → 選後者。
- **色彩哲學**：有限調色盤（街機感）下仍恪守 **冷色＝你（安全、精密、科技）、暖色＝威脅（敵彈、巨獸、危險）**。玩家靠色溫就能在 0.1 秒內判斷生死——這同時服務了〔難度是門〕的可及性。

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| **Sky Force Reloaded / Anniversary** | 重刷關卡收集資源 → 升級戰機與武器 → 跨難度挑戰 | 疊上破壞部位的狩獵深度 | 驗證了「刷關養成 shmup」的商業成功（手機數千萬下載、Steam 好評） |
| **雷電 Raiden** | 場地掉落換武器、縱向彈幕的純粹手感 | 加入雙武器池、破部位、永久養成 | 驗證了「掉落換武器」的即時爽感與經典骨架 |
| **Monster Hunter** | 破壞部位、素材驅動的狩獵循環 | 搬進 2D 彈幕、大幅簡化 | 驗證了破部位狩獵帶來的長期黏著 |
| **Drainus** | 帶武器升級樹的現代 shmup | 用素材經濟取代線性升級樹 | 驗證了「shmup + 養成」當代仍可行且受好評 |

**Non-game inspirations**: 哥吉拉／環太平洋等巨獸電影（尺寸與壓迫感的構圖語言）；80–90 年代街機廳的像素美學與音效質感。

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18–35 |
| **Gaming experience** | Mid-core 到 Hardcore（但最低難度階為 Casual 留入口） |
| **Time availability** | 通勤／晚間 20–40 分短時段，週末較長刷關 |
| **Platform preference** | 手機為主、PC 次之 |
| **Current games they play** | Sky Force、雷電／東方 Project、Monster Hunter |
| **What they're looking for** | 有深度但能短時段消化、難度可調、帶收集養成的射擊遊戲 |
| **What would turn them away** | 重敘事與長過場、無進度存檔、純手速地獄門檻、單局過長 |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity — 多平台（尤其手機）生態成熟、2D 工具完整、與 Sky Force 類標竿對齊 |
| **Key Technical Challenges** | 手機彈幕效能（物件池／可考慮 DOTS+Burst）、雙輸入手感（觸控＋鍵鼠/手柄）、可破壞部位系統 |
| **Art Style** | Pixel（像素街機懷舊） |
| **Art Pipeline Complexity** | Low–Medium — 像素整體便宜，但螢幕級巨獸的像素動畫是主要成本驅動；部位模組化 sprite 可複用 |
| **Audio Needs** | Moderate — 街機式 SFX ＋ 配樂；破部位音效需特別「爽」 |
| **Networking** | None（單人；排行榜屬夢想層的輕後端） |
| **Content Volume** | 3–5 關、3–5 巨獸（各 3–4 部位）、8 武器、4 難度階；靠刷素材／破完部位堆疊到數十小時 |
| **Procedural Systems** | 輕量——手作小怪波／關卡段池的隨機組合，**非**完整程序生成 |

---

## Risks and Open Questions

### Design Risks
- **【最高】武器平衡**：「等價而異性格」的橫向選擇極難調——〔橫向選擇〕支柱成敗全繫於此。8 種武器總威力均衡卻又各有不可取代用途，是全專案最硬的設計題。
- **養成經濟曲線**：素材 → 升級的曲線容易膨脹，需刻意保持簡單。
- **破部位的「爽感」未驗證**：若破壞回饋不夠強，核心循環會失色。

### Technical Risks
- **手機彈幕效能**：同畫面上千子彈 × 觸控 × 手機 → 物件池／DOTS 是必修。
- **可破壞部位系統**：每部位獨立判定＋狀態＋掉落綁定，中等複雜度。
- **觸控彈幕精準度**：需驗證手指拖曳偏移操作（Sky Force 解法）的手感。

### Market Risks
- **低**——Sky Force 已驗證模型；shmup 本身小眾，但「養成＋手機」顯著擴大受眾。

### Scope Risks
- 8 武器 × 平衡 × 升級階，在「幾週」內無法全做完 → 必須靠規模分層。
- 巨獸像素動畫工時可能超估。

### Open Questions
- 武器是否一開始就上 8 種，還是先做 4 種驗證循環？→ 由 `/prototype` 回答。
- 觸控操作手感是否成立？→ 由 `/prototype` 回答。
- 破部位的視聽回饋強度需要多誇張才「爽」？→ 由 `/prototype` 回答。

---

## MVP Definition

**Core hypothesis**：「破壞部位 → 掉素材 → 永久升級武器 → 再戰」這條循環，配上爽脆的彈幕射擊，能讓玩家想一玩再玩。

**Required for MVP**:
1. 1 個關卡（隨機小怪段 ＋ 1 隻有 2–3 可破壞部位的巨獸）。
2. 2 種武器（1 主 1 副，各 1–2 升級階），透過場地掉落取得。
3. 「破部位 → 素材 → 武器永久升級」經濟端到端跑通。
4. 1–2 個難度階（驗證彈幕密度縮放手感）。

**Explicitly NOT in MVP** (defer to later):
- 完整 8 種武器、4–5 隻巨獸、隨機關卡組合系統。
- 劇情框架、排行榜、每日挑戰、戰機外觀。

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 1 關、1 巨獸（2–3 部位）、2 武器 | 核心循環＋破部位經濟 | ~3–4 週 |
| **Vertical Slice** | 1 完整關卡＋首隻巨獸全部位、4 武器 | 核心＋養成＋2 難度階 | ~6–8 週 |
| **Alpha** | 全關卡/巨獸佔位、8 武器、4 難度、隨機關卡 | 全功能（粗糙） | ~3 個月 |
| **Full Vision** | 3–5 關、3–5 巨獸完整、8 武器全升級、輕劇情 | 全功能（打磨） | ~4–5 個月 |

---

## Next Steps

- [ ] （Lean 模式：creative-director 概念核可為選用，可於 `/gate-check` 統一進行）
- [ ] 設定引擎與版本參考文件（`/setup-engine` → Unity）
- [ ] 建立美術聖經（`/art-bible`）——用上方視覺錨點當起點；GDD 之前先做
- [ ] 驗證概念完整性（`/design-review design/gdd/game-concept.md`）
- [ ] **原型驗證核心循環**（`/prototype` 破部位→素材→升級）——寫 GDD 前先確認好玩
- [ ] 若原型 PROCEEDS：拆解系統（`/map-systems`）
- [ ] 逐系統撰寫 GDD（`/design-system`）——把原型學到的填進 Tuning Knobs / Formulas
- [ ] 前製期建置 vertical slice（`/vertical-slice`）驗證完整遊戲循環
- [ ] 以 playtest 驗證核心循環（`/playtest-report`）
- [ ] 規劃第一個里程碑（`/sprint-plan new`）
