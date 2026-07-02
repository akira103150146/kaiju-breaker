namespace KaijuBreaker.Core
{
    /// <summary>
    /// Stable identifier for each of the 8 weapons across the two pools
    /// (Primary = laser family L1–L4, Secondary = missile family M1–M4).
    /// Used as a data key by Weapons / Economy / Meta / Stage (weapon pods).
    /// See weapon-system.md. Cross-system shared type (ADR-0005 §Core).
    /// </summary>
    public enum WeaponId
    {
        // Primary pool — 雷射系 (Laser family)
        L1 = 0, // 散波雷射 Spread Laser
        L2 = 1, // 集束雷射 Focus Beam
        L3 = 2, // 波動砲 Wave Cannon
        L4 = 3, // 穿透雷射 Pierce Beam

        // Secondary pool — 飛彈系 (Missile family)
        M1 = 4, // 追蹤飛彈 Homing Missile
        M2 = 5, // 蜂群飛彈 Swarm Launcher
        M3 = 6, // 穿甲魚雷 AP Torpedo
        M4 = 7  // 叢集炸彈 Cluster Bomb
    }
}
