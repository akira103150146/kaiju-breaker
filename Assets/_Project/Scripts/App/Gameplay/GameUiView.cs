using System;
using KaijuBreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Code-built UGUI + TextMeshPro front-end (ADR-0006), replacing the IMGUI placeholder. Builds a single
    /// Screen-Space-Overlay <see cref="Canvas"/> (portrait reference resolution + <see cref="CanvasScaler"/> so it
    /// scales identically on PC and phone) whose child roots are the game's screens: title, boss-select hub,
    /// upgrade shop, loadout, HUD, and results. Nothing is authored in the scene — the whole hierarchy is created
    /// procedurally in <see cref="Build"/>, matching the project's data-/code-driven asset pattern.
    ///
    /// <para>The scene director (<see cref="GameplaySceneDirector"/>) owns all game state and logic; this view only
    /// renders it and forwards button presses back through the <c>On*</c> callbacks. Colours follow the art-bible
    /// cold palette (warm reserved for threat/defeat).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameUiView : MonoBehaviour
    {
        // ── Art-bible palette ────────────────────────────────────────────────
        static readonly Color Backdrop = new Color(0.02f, 0.03f, 0.06f, 0.55f);
        static readonly Color PanelFill = new Color(0.06f, 0.09f, 0.16f, 0.96f);
        static readonly Color PanelBorder = new Color(0.25f, 0.97f, 1f, 0.85f);
        static readonly Color Cyan = new Color(0.25f, 0.97f, 1f, 1f);
        static readonly Color CyanDim = new Color(0f, 0.75f, 0.88f, 1f);
        static readonly Color Ink = new Color(0.9f, 0.96f, 1f, 1f);
        static readonly Color Warm = new Color(1f, 0.55f, 0.28f, 1f);
        static readonly Color Danger = new Color(1f, 0.34f, 0.30f, 1f);
        static readonly Color BtnFill = new Color(0.09f, 0.15f, 0.26f, 0.98f);
        static readonly Color BtnSelFill = new Color(0.13f, 0.30f, 0.48f, 1f);

        public enum Screen { None, Title, BossSelect, Upgrades, Loadout, Hud, Results }

        // ── Callbacks the director wires ─────────────────────────────────────
        public Action OnTitleTap, OnBossConfirm, OnUpgradesOpen, OnUpgradesClose, OnStart, OnRestart, OnSkipToggled;
        public Action<int> OnBossPicked, OnPrimaryPicked, OnSecondaryPicked, OnDifficultyPicked;
        public Action<int> OnBuyUpgrade; // index into the shop rows (see UpgradeRowId)

        // Shop row identity (matches the order rows are built / refreshed).
        public enum UpgradeRowId { FireRate = 0, DropRate = 1, Ammo = 2, Magnet = 3, IFrame = 4, Speed = 5, HeadStart = 6 }

        // ── Built widgets kept for live updates ──────────────────────────────
        Canvas _canvas;
        readonly GameObject[] _screens = new GameObject[7];

        // title
        TextMeshProUGUI _titlePrompt;
        // hud
        TextMeshProUGUI _hpLabel, _arsenalLabel, _phaseLabel;
        RectTransform _hpFill; float _hpFullWidth;
        RectTransform _phaseBox;
        // L3 波動 charge bar (only shown when the 波動 charge weapon is equipped)
        GameObject _chargeBar; RectTransform _chargeFill; float _chargeFullWidth; TextMeshProUGUI _chargeLabel;
        // results
        TextMeshProUGUI _resultTitle, _resultSub;
        // loadout / boss select selection highlighting
        Image[] _primaryBtns, _secondaryBtns, _difficultyBtns, _bossCells;
        Image _skipBox; TextMeshProUGUI _skipLabel;
        // upgrades live text
        TextMeshProUGUI _shardLabel;
        readonly TextMeshProUGUI[] _rowLevel = new TextMeshProUGUI[7];
        readonly TextMeshProUGUI[] _rowBtnLabel = new TextMeshProUGUI[7];
        readonly Button[] _rowBtn = new Button[7];

        bool _built;

        // ─────────────────────────────────────────────────────────────────────
        // Build
        // ─────────────────────────────────────────────────────────────────────
        public void Build(string[] bossNames, string[] bossCodes, Color[] bossColors, bool[] bossUnlocked,
                          string[] primaryLabels, string[] secondaryLabels, string[] diffLabels)
        {
            if (_built) return;
            _built = true;

            EnsureEventSystem();

            var canvasGo = new GameObject("GameUiCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f); // portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _screens[(int)Screen.Title] = BuildTitle();
            _screens[(int)Screen.BossSelect] = BuildBossSelect(bossNames, bossCodes, bossColors, bossUnlocked);
            _screens[(int)Screen.Upgrades] = BuildUpgrades();
            _screens[(int)Screen.Loadout] = BuildLoadout(primaryLabels, secondaryLabels, diffLabels);
            _screens[(int)Screen.Hud] = BuildHud();
            _screens[(int)Screen.Results] = BuildResults();

            Show(Screen.None);
        }

        public void Show(Screen s)
        {
            for (int i = 0; i < _screens.Length; i++)
                if (_screens[i] != null) _screens[i].SetActive(i == (int)s);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Title
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildTitle()
        {
            var root = ScreenRoot("Title_Screen", withBackdrop: true);
            // Tapping anywhere starts — a full-screen transparent button.
            var hit = Img(root.transform, "TapCatcher", new Color(0, 0, 0, 0)); Stretch(hit.rectTransform);
            hit.raycastTarget = true;
            var b = hit.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => OnTitleTap?.Invoke());

            var panel = Panel(root.transform, 760f, 520f, 0f, 120f);
            Txt(panel, "Title", "殲獸戰機", 108f, Cyan, TextAlignmentOptions.Center, FontStyles.Bold, 760f, 140f, 0f, 120f);
            Txt(panel, "Sub", "KAIJU BREAKER", 52f, Ink, TextAlignmentOptions.Center, FontStyles.Bold, 760f, 60f, 0f, 10f);
            _titlePrompt = Txt(panel, "Prompt", "點擊開始  ·  TAP TO START", 34f, CyanDim,
                               TextAlignmentOptions.Center, FontStyles.Normal, 760f, 48f, 0f, -140f);
            return root.gameObject;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Boss select (MMX-style hub)
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildBossSelect(string[] names, string[] codes, Color[] colors, bool[] unlocked)
        {
            var root = ScreenRoot("BossSelect_Screen", withBackdrop: true);
            int count = names.Length;
            int cols = Mathf.Min(4, count);
            int rows = (count + cols - 1) / cols;
            float cw = 236f, ch = 300f, gap = 26f;
            float pw = cols * cw + (cols - 1) * gap + 90f;
            float ph = 150f + rows * (ch + gap) + 130f;
            var panel = Panel(root.transform, pw, ph, 0f, 0f);

            Txt(panel, "Heading", "選擇獵物  SELECT TARGET", 46f, Ink, TextAlignmentOptions.Center, FontStyles.Bold,
                pw, 60f, 0f, ph * 0.5f - 56f);

            // 強化 (top-right); ◆ instead of ⚙ (Noto Sans TC has no gear glyph).
            Btn(panel, "強化 ◆", 200f, 60f, pw * 0.5f - 116f, ph * 0.5f - 54f, 30f, () => OnUpgradesOpen?.Invoke());

            _bossCells = new Image[count];
            float gridW = cols * cw + (cols - 1) * gap;
            float startX = -gridW * 0.5f + cw * 0.5f;
            float startY = ph * 0.5f - 150f - ch * 0.5f;
            for (int i = 0; i < count; i++)
            {
                int r = i / cols, c = i % cols;
                float x = startX + c * (cw + gap);
                float y = startY - r * (ch + gap);
                var cell = Img(panel, "Cell" + i, unlocked[i] ? BtnFill : new Color(0.10f, 0.11f, 0.14f, 0.96f));
                SetRect(cell.rectTransform, cw, ch, x, y);
                cell.raycastTarget = true;
                _bossCells[i] = cell;
                int idx = i;
                var cellBtn = cell.gameObject.AddComponent<Button>();
                cellBtn.transition = Selectable.Transition.None;
                if (unlocked[i]) cellBtn.onClick.AddListener(() => OnBossPicked?.Invoke(idx));

                var port = Img(cell.transform, "Portrait", unlocked[i] ? colors[i] : new Color(0.28f, 0.30f, 0.36f, 1f));
                port.raycastTarget = false;
                SetRect(port.rectTransform, cw - 56f, 150f, 0f, ch * 0.5f - 96f);
                Txt(cell.transform, "Name", names[i], 38f, unlocked[i] ? Ink : new Color(0.6f, 0.62f, 0.68f), TextAlignmentOptions.Center, FontStyles.Bold, cw, 44f, 0f, -ch * 0.5f + 78f);
                Txt(cell.transform, "Code", unlocked[i] ? codes[i] : "開發中 LOCKED", 26f, CyanDim, TextAlignmentOptions.Center, FontStyles.Normal, cw, 34f, 0f, -ch * 0.5f + 38f);
            }

            Btn(panel, "確定  ▶  裝備", 380f, 78f, 0f, -ph * 0.5f + 58f, 36f, () => OnBossConfirm?.Invoke());
            return root.gameObject;
        }

        public void SetBossSelected(int index)
        {
            if (_bossCells == null) return;
            for (int i = 0; i < _bossCells.Length; i++)
                if (_bossCells[i] != null) _bossCells[i].color = i == index ? BtnSelFill : BtnFill;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Upgrade shop
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildUpgrades()
        {
            var root = ScreenRoot("Upgrades_Screen", withBackdrop: true);
            float pw = 880f, ph = 1120f;
            var panel = Panel(root.transform, pw, ph, 0f, 0f);
            Txt(panel, "Heading", "強化 · UPGRADE", 50f, Ink, TextAlignmentOptions.Center, FontStyles.Bold, pw, 60f, 0f, ph * 0.5f - 56f);
            _shardLabel = Txt(panel, "Shards", "碎片 Shards：0", 30f, CyanDim, TextAlignmentOptions.Left, FontStyles.Normal, pw - 90f, 34f, 0f, ph * 0.5f - 118f);

            float top = ph * 0.5f - 180f;
            BuildUpgradeRow(panel, (int)UpgradeRowId.FireRate, "開火速度  FIRE RATE", top);
            BuildUpgradeRow(panel, (int)UpgradeRowId.DropRate, "掉落率  DROP RATE", top - 108f);

            Txt(panel, "CoresHead", "頭目核心 · CORES", 30f, CyanDim, TextAlignmentOptions.Left, FontStyles.Bold, pw - 90f, 34f, 0f, top - 232f);
            BuildUpgradeRow(panel, (int)UpgradeRowId.Ammo, "副武射速  M-RATE", top - 300f);
            BuildUpgradeRow(panel, (int)UpgradeRowId.Magnet, "道具吸取  MAGNET", top - 408f);
            BuildUpgradeRow(panel, (int)UpgradeRowId.IFrame, "無敵時間  I-FRAME", top - 516f);
            BuildUpgradeRow(panel, (int)UpgradeRowId.Speed, "移動速度  SPEED", top - 624f);
            BuildUpgradeRow(panel, (int)UpgradeRowId.HeadStart, "開場火力  HEAD-START", top - 732f);

            Btn(panel, "返回 BACK", 320f, 70f, 0f, -ph * 0.5f + 54f, 34f, () => OnUpgradesClose?.Invoke());
            return root.gameObject;
        }

        void BuildUpgradeRow(RectTransform panel, int id, string title, float y)
        {
            float pw = panel.sizeDelta.x;
            Txt(panel, "T" + id, title, 34f, Ink, TextAlignmentOptions.Left, FontStyles.Bold, pw - 400f, 40f, -30f, y + 18f);
            _rowLevel[id] = Txt(panel, "L" + id, "Lv 0", 26f, CyanDim, TextAlignmentOptions.Left, FontStyles.Normal, pw - 400f, 32f, -30f, y - 20f);
            var btn = Btn(panel, "升級", 240f, 72f, pw * 0.5f - 150f, y, 30f, () => OnBuyUpgrade?.Invoke(id));
            _rowBtn[id] = btn;
            _rowBtnLabel[id] = btn.GetComponentInChildren<TextMeshProUGUI>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Loadout
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildLoadout(string[] primary, string[] secondary, string[] diff)
        {
            var root = ScreenRoot("Loadout_Screen", withBackdrop: true);
            float pw = 900f, ph = 1000f;
            var panel = Panel(root.transform, pw, ph, 0f, 0f);
            Txt(panel, "Heading", "選擇裝備  LOADOUT", 50f, Ink, TextAlignmentOptions.Center, FontStyles.Bold, pw, 60f, 0f, ph * 0.5f - 56f);

            float top = ph * 0.5f - 190f;
            _primaryBtns = BuildOptionRow(panel, "主武器 · 雷射", primary, top, i => OnPrimaryPicked?.Invoke(i));
            _secondaryBtns = BuildOptionRow(panel, "副武器 · 飛彈", secondary, top - 200f, i => OnSecondaryPicked?.Invoke(i));
            _difficultyBtns = BuildOptionRow(panel, "難度 · 彈幕密度", diff, top - 400f, i => OnDifficultyPicked?.Invoke(i));

            // Skip toggle
            var skip = Btn(panel, "□  跳過道中 · 直達 BOSS (測試)", pw - 120f, 56f, 0f, top - 540f, 28f, () => OnSkipToggled?.Invoke());
            _skipBox = skip.GetComponent<Image>();
            _skipLabel = skip.GetComponentInChildren<TextMeshProUGUI>();

            Btn(panel, "出擊  START", 380f, 88f, 0f, -ph * 0.5f + 66f, 40f, () => OnStart?.Invoke());
            return root.gameObject;
        }

        Image[] BuildOptionRow(RectTransform panel, string title, string[] labels, float y, Action<int> onPick)
        {
            float pw = panel.sizeDelta.x;
            // Title sits clearly ABOVE the button band (buttons are 76 tall → ±38; title bottom must clear +38).
            Txt(panel, "RT:" + title, title, 28f, Ink, TextAlignmentOptions.Left, FontStyles.Bold, pw - 120f, 32f, 0f, y + 64f);
            int n = labels.Length;
            float rowW = pw - 120f;
            float bw = (rowW - (n - 1) * 16f) / n;
            var imgs = new Image[n];
            float startX = -rowW * 0.5f + bw * 0.5f;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var btn = Btn(panel, labels[i], bw, 76f, startX + i * (bw + 16f), y, 30f, () => onPick(idx));
                imgs[i] = btn.GetComponent<Image>();
            }
            return imgs;
        }

        public void SetLoadout(int primary, int secondary, int difficulty, bool skip)
        {
            Highlight(_primaryBtns, primary);
            Highlight(_secondaryBtns, secondary);
            Highlight(_difficultyBtns, difficulty);
            if (_skipBox != null) _skipBox.color = skip ? BtnSelFill : BtnFill;
            if (_skipLabel != null) _skipLabel.text = (skip ? "■" : "□") + "  跳過道中 · 直達 BOSS (測試)";
        }

        static void Highlight(Image[] group, int selected)
        {
            if (group == null) return;
            for (int i = 0; i < group.Length; i++)
                if (group[i] != null) group[i].color = i == selected ? BtnSelFill : BtnFill;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HUD
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildHud()
        {
            var root = ScreenRoot("Hud_Screen", withBackdrop: false);

            // Phase tag — anchored to the TOP edge so it stays on-screen on any aspect ratio.
            _phaseBox = Panel(root.transform, 380f, 68f, 0f, 0f);
            _phaseBox.gameObject.name = "PhaseTag";
            AnchorTop(_phaseBox, 380f, 68f, 40f);
            _phaseLabel = Txt(_phaseBox, "Phase", "", 34f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(_phaseLabel.rectTransform);

            // Arsenal readout + HP label + bar — anchored to the BOTTOM edge (above the touch controls).
            _arsenalLabel = Txt(root.transform, "Arsenal", "", 28f, CyanDim, TextAlignmentOptions.Center, FontStyles.Normal);
            AnchorBottom(_arsenalLabel.rectTransform, 960f, 34f, 132f);

            _hpLabel = Txt(root.transform, "HpLabel", "", 28f, Ink, TextAlignmentOptions.Center, FontStyles.Normal);
            AnchorBottom(_hpLabel.rectTransform, 600f, 32f, 92f);

            _hpFullWidth = 560f;
            var barBg = Img(root.transform, "HpBarBg", new Color(0f, 0f, 0f, 0.6f));
            AnchorBottom(barBg.rectTransform, _hpFullWidth + 6f, 34f, 52f);
            barBg.raycastTarget = false;
            var fill = Img(barBg.transform, "HpFill", Cyan); fill.raycastTarget = false;
            _hpFill = fill.rectTransform;
            _hpFill.anchorMin = new Vector2(0f, 0.5f); _hpFill.anchorMax = new Vector2(0f, 0.5f);
            _hpFill.pivot = new Vector2(0f, 0.5f);
            _hpFill.sizeDelta = new Vector2(_hpFullWidth, 28f);
            _hpFill.anchoredPosition = new Vector2(3f, 0f);

            // 波動 charge bar — sits just above the arsenal readout, only visible when the 波動 (L3) weapon is equipped.
            _chargeFullWidth = 520f;
            _chargeBar = Img(root.transform, "ChargeBarBg", new Color(0f, 0f, 0f, 0.6f)).gameObject;
            var cbBg = _chargeBar.GetComponent<Image>(); cbBg.raycastTarget = false;
            AnchorBottom(cbBg.rectTransform, _chargeFullWidth + 6f, 26f, 176f);
            var cFill = Img(_chargeBar.transform, "ChargeFill", new Color(0.20f, 0.85f, 1f, 1f)); cFill.raycastTarget = false;
            _chargeFill = cFill.rectTransform;
            _chargeFill.anchorMin = new Vector2(0f, 0.5f); _chargeFill.anchorMax = new Vector2(0f, 0.5f);
            _chargeFill.pivot = new Vector2(0f, 0.5f);
            _chargeFill.sizeDelta = new Vector2(0f, 20f);
            _chargeFill.anchoredPosition = new Vector2(3f, 0f);
            _chargeLabel = Txt(_chargeBar.transform, "ChargeLabel", "集氣", 20f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(_chargeLabel.rectTransform);
            _chargeBar.SetActive(false);
            return root.gameObject;
        }

        /// <summary>Drive the 波動 charge bar: show it only when the charge weapon is equipped, fill it by [0,1], and
        /// flash it bright when full so the player knows it's ready to release. Called every frame while the HUD is up.</summary>
        public void SetCharge(bool active, float frac)
        {
            if (_chargeBar == null) return;
            if (_chargeBar.activeSelf != active) _chargeBar.SetActive(active);
            if (!active) return;
            frac = Mathf.Clamp01(frac);
            if (_chargeFill != null)
            {
                _chargeFill.sizeDelta = new Vector2(_chargeFullWidth * frac, 20f);
                var img = _chargeFill.GetComponent<Image>();
                if (img != null)
                    img.color = frac >= 0.999f
                        ? new Color(0.7f, 1f, 1f, 1f)                 // full — bright flash (ready to release)
                        : new Color(0.20f, 0.85f, 1f, 1f);           // charging — sky blue
            }
            if (_chargeLabel != null) _chargeLabel.text = frac >= 0.999f ? "放開發射！" : "集氣";
        }

        public void SetHud(int hp, int maxHp, int weaponPower, int missilePower, string primaryType, string secondaryType, RunState run)
        {
            float frac = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            if (_hpFill != null)
            {
                _hpFill.sizeDelta = new Vector2(_hpFullWidth * frac, 28f);
                var img = _hpFill.GetComponent<Image>();
                if (img != null) img.color = frac > 0.35f ? Cyan : Danger;
            }
            if (_hpLabel != null) _hpLabel.text = "HP " + hp + " / " + maxHp;
            if (_arsenalLabel != null)
                _arsenalLabel.text = "火力 Lv" + weaponPower + "   飛彈 Lv" + missilePower + "   " + primaryType + "/" + secondaryType;

            string phase = run == RunState.Boss ? "頭目戰  BOSS" : run == RunState.Stage ? "道中  STAGE" : "";
            if (_phaseBox != null) _phaseBox.gameObject.SetActive(phase.Length > 0);
            if (_phaseLabel != null) { _phaseLabel.text = phase; _phaseLabel.color = run == RunState.Boss ? Warm : Ink; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Results
        // ─────────────────────────────────────────────────────────────────────
        GameObject BuildResults()
        {
            var root = ScreenRoot("Results_Screen", withBackdrop: true);
            var panel = Panel(root.transform, 680f, 520f, 0f, 0f);
            _resultTitle = Txt(panel, "Title", "", 100f, Cyan, TextAlignmentOptions.Center, FontStyles.Bold, 680f, 130f, 0f, 110f);
            _resultSub = Txt(panel, "Sub", "", 44f, CyanDim, TextAlignmentOptions.Center, FontStyles.Normal, 680f, 52f, 0f, 20f);
            Btn(panel, "重新開始  (R)", 380f, 92f, 0f, -140f, 40f, () => OnRestart?.Invoke());
            return root.gameObject;
        }

        public void SetResults(bool win)
        {
            if (_resultTitle != null) { _resultTitle.text = win ? "勝利！" : "敗北"; _resultTitle.color = win ? Cyan : Danger; }
            if (_resultSub != null) _resultSub.text = win ? "VICTORY" : "DEFEAT";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Upgrade-shop live refresh (director passes the current numbers)
        // ─────────────────────────────────────────────────────────────────────
        public void SetShards(int shards) { if (_shardLabel != null) _shardLabel.text = "碎片 Shards：" + shards; }

        /// <summary>Update one shop row's level text and buy button (cost, or MAX when capped).</summary>
        public void SetUpgradeRow(int id, int level, int max, int cost, bool maxed, string ownedSuffix)
        {
            if (id < 0 || id >= 7) return;
            if (_rowLevel[id] != null) _rowLevel[id].text = "Lv " + level + " / " + max + ownedSuffix;
            if (_rowBtnLabel[id] != null) _rowBtnLabel[id].text = maxed ? "MAX" : "升級 (" + cost + ")";
            if (_rowBtn[id] != null) _rowBtn[id].interactable = !maxed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Title prompt blink (called each frame while the title is up)
        // ─────────────────────────────────────────────────────────────────────
        public void TickTitleBlink()
        {
            if (_titlePrompt == null) return;
            bool on = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;
            var c = _titlePrompt.color; c.a = on ? 1f : 0f; _titlePrompt.color = c;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Widget factory helpers
        // ─────────────────────────────────────────────────────────────────────
        RectTransform ScreenRoot(string name, bool withBackdrop)
        {
            var rt = NewRect(name, _canvas.transform);
            Stretch(rt);
            if (withBackdrop)
            {
                var bd = Img(rt, "Backdrop", Backdrop); Stretch(bd.rectTransform); bd.raycastTarget = true;
            }
            return rt;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // Anchor an element to the CENTRE of its parent, sized w×h, offset (x,y) from centre.
        static void SetRect(RectTransform rt, float w, float h, float x, float y)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // Anchor to the TOP-centre of the parent, dropping yFromTop units below the top edge. Stays on-screen
        // regardless of aspect ratio (unlike a centre anchor with a large offset).
        static void AnchorTop(RectTransform rt, float w, float h, float yFromTop)
        {
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
        }

        // Anchor to the BOTTOM-centre of the parent, yFromBottom units above the bottom edge.
        static void AnchorBottom(RectTransform rt, float w, float h, float yFromBottom)
        {
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, yFromBottom);
        }

        static Image Img(Transform parent, string name, Color c)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            return img;
        }

        // Bordered panel: a border-coloured image with an inset fill child (crisp 3px frame, no sprite needed).
        // Returns the border's RectTransform; callers parent children under it (the fill sits behind, added first).
        RectTransform Panel(Transform parent, float w, float h, float x, float y) => Panel(parent, w, h, x, y, PanelFill);

        RectTransform Panel(Transform parent, float w, float h, float x, float y, Color fill)
        {
            var border = Img(parent, "Panel", PanelBorder);
            SetRect(border.rectTransform, w, h, x, y);
            border.raycastTarget = true;
            var inner = Img(border.transform, "Fill", fill);
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(3f, 3f); inner.rectTransform.offsetMax = new Vector2(-3f, -3f);
            inner.raycastTarget = true;
            return border.rectTransform;
        }

        TextMeshProUGUI Txt(Transform parent, string name, string s, float size, Color c,
                            TextAlignmentOptions align, FontStyles style, float w, float h, float x, float y)
        {
            var t = Txt(parent, name, s, size, c, align, style);
            SetRect(t.rectTransform, w, h, x, y);
            return t;
        }

        TextMeshProUGUI Txt(Transform parent, string name, string s, float size, Color c,
                            TextAlignmentOptions align, FontStyles style)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = s; t.fontSize = size; t.color = c; t.alignment = align; t.fontStyle = style;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        Button Btn(Transform parent, string label, float w, float h, float x, float y, float fontSize, Action onClick)
        {
            var img = Img(parent, "Btn:" + label, BtnFill);
            SetRect(img.rectTransform, w, h, x, y);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.8f, 0.9f, 1f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            var t = Txt(img.transform, "Label", label, fontSize, Cyan, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(t.rectTransform);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
