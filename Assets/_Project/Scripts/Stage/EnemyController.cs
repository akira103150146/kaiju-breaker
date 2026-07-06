using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Runtime component on a spawned trash-enemy instance (stage-system.md §D.2). Receives its data from the
    /// <see cref="WaveSpawner"/> via <see cref="Init"/>: the <see cref="EnemyDef"/> plus the movement/emitter
    /// pattern SOs the def carries (the data wiring Story 002 asserts). HP is derived from the def's
    /// <see cref="HpTier"/>; elite instances tint to the def's aura colour.
    ///
    /// <para>Movement here is a placeholder downward drift so instances are visible/moving; executing the full
    /// <see cref="MovementPatternSO"/> is a movement-system concern, and bullet emission from
    /// <see cref="EmitterPatternSO"/> is blocked by ADR-0001 — both are follow-ups. This component only owns
    /// the per-instance data + a minimal placeholder motion.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyController : MonoBehaviour
    {
        [Tooltip("Placeholder descent speed (world units/sec) until the movement system drives MovementPattern.")]
        [SerializeField] private float _placeholderDriftSpeed = 1.5f;

        /// <summary>The data definition this instance was spawned from (null only if never initialised).</summary>
        public EnemyDef Def { get; private set; }

        /// <summary>Movement pattern SO carried by <see cref="Def"/> (may be null if the def has none assigned).</summary>
        public MovementPatternSO Movement { get; private set; }

        /// <summary>Emitter (bullet) pattern SO carried by <see cref="Def"/>. Wired now; fired once ADR-0001 lands.</summary>
        public EmitterPatternSO Emitter { get; private set; }

        /// <summary>Current hit points, derived from the def's HP tier at spawn.</summary>
        public int Hp { get; private set; }

        /// <summary>Whether this instance is the elite of its wave.</summary>
        public bool IsElite { get; private set; }

        /// <summary>Inject the def + wire its pattern SOs. Called by <see cref="WaveSpawner"/> right after Instantiate.</summary>
        public void Init(EnemyDef def, bool isElite)
        {
            Def = def;
            Movement = def != null ? def.MovementPattern : null;
            Emitter = def != null ? def.EmitterPattern : null;
            IsElite = isElite;
            Hp = HpForTier(def);

            if (isElite && def != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = def.EliteAuraColor;
            }
        }

        private static int HpForTier(EnemyDef def)
        {
            if (def == null) return 1;
            switch (def.HpTier)
            {
                case HpTier.T2: return 3;
                case HpTier.T1:
                default: return 1;
            }
        }

        private void Update()
        {
            transform.position += Vector3.down * (_placeholderDriftSpeed * Time.deltaTime);
        }
    }
}
