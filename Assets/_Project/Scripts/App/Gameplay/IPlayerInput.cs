using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Input seam for the player ship so the same ship logic serves both platforms at parity
    /// (technical-preferences: PC keyboard/mouse and mobile touch are both first-class). PC uses
    /// <see cref="KeyboardMouseInput"/>; the mobile virtual joystick + secondary button (Phase E) is a
    /// second implementation — the ship never branches on platform, it just reads this.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Digital/analog move axis in [-1, 1] per component (keyboard / stick / joystick).</summary>
        Vector2 MoveAxis { get; }

        /// <summary>True while a pointer-drag (mouse / touch) target is active; the ship eases toward it.</summary>
        bool HasPointerTarget { get; }

        /// <summary>World-space position of the pointer-drag target (valid only when <see cref="HasPointerTarget"/>).</summary>
        Vector2 PointerWorld { get; }

        /// <summary>Edge-triggered: true for the single frame the secondary-fire input was pressed.</summary>
        bool SecondaryPressedThisFrame { get; }

        /// <summary>True while the CHARGE (集氣) input is held — the 波動 primary charges while held and fires on
        /// release. PC: hold left mouse / J. Mobile: hold the on-screen charge button. Ignored by non-charge
        /// primaries (they auto-fire), so it is safe to read every frame.</summary>
        bool PrimaryHeld { get; }
    }
}
