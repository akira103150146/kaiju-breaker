using System.Collections.Generic;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// The single aggregation point for all static tuning ScriptableObjects (ADR-0003).
    /// One <c>ContentRegistry</c> asset is referenced by the App composition root, which
    /// pulls the individual configs from here and injects them into the systems that need
    /// them. Systems never load content themselves (no Resources.Load / Addressables in
    /// gameplay code) — they receive their config via DI.
    ///
    /// Pure data + lookup tables; no gameplay logic.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/ContentRegistry", fileName = "ContentRegistry")]
    public sealed class ContentRegistry : ScriptableObject
    {
        [Header("Global Config Singletons")]
        [SerializeField] private WeaponBalanceConfig _weaponBalance;
        [SerializeField] private PartSystemConfig _partSystem;
        [SerializeField] private DifficultyConfig _difficulty;
        [SerializeField] private GameFeelConfig _gameFeel;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private InputSettings _input;
        [SerializeField] private SaveConfig _save;

        [Header("Weapon Definitions (one per WeaponId, 8 total)")]
        [SerializeField] private WeaponDef[] _weapons = new WeaponDef[0];

        [Header("Kaiju Definitions")]
        [SerializeField] private KaijuDef[] _kaiju = new KaijuDef[0];

        private Dictionary<WeaponId, WeaponDef> _weaponLookup;

        // ── Global singletons ─────────────────────────────────────────────────────
        public WeaponBalanceConfig WeaponBalance => _weaponBalance;
        public PartSystemConfig PartSystem => _partSystem;
        public DifficultyConfig Difficulty => _difficulty;
        public GameFeelConfig GameFeel => _gameFeel;
        public EconomyConfig Economy => _economy;
        public InputSettings Input => _input;
        public SaveConfig Save => _save;

        /// <summary>All weapon definitions (unordered).</summary>
        public IReadOnlyList<WeaponDef> Weapons => _weapons;

        /// <summary>All kaiju definitions (unordered).</summary>
        public IReadOnlyList<KaijuDef> Kaiju => _kaiju;

        /// <summary>Look up a weapon's tuning data by its stable <see cref="WeaponId"/>. Null if absent.</summary>
        public WeaponDef GetWeapon(WeaponId id)
        {
            if (_weaponLookup == null) BuildWeaponLookup();
            return _weaponLookup.TryGetValue(id, out var def) ? def : null;
        }

        private void BuildWeaponLookup()
        {
            _weaponLookup = new Dictionary<WeaponId, WeaponDef>(_weapons.Length);
            for (int i = 0; i < _weapons.Length; i++)
            {
                var w = _weapons[i];
                if (w == null) continue;
                _weaponLookup[w.Id] = w; // last one wins if duplicated (OnValidate warns)
            }
        }

        private void OnEnable() => _weaponLookup = null; // rebuild lazily after (re)load

        private void OnValidate()
        {
            var seen = new HashSet<WeaponId>();
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] == null) continue;
                if (!seen.Add(_weapons[i].Id))
                    Debug.LogError($"[ContentRegistry] '{name}': duplicate WeaponDef for {_weapons[i].Id}.", this);
            }
        }
    }
}
