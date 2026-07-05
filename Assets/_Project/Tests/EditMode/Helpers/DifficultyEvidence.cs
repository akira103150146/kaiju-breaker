using System.IO;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    /// <summary>
    /// Writes an invariance-matrix evidence file under <c>production/qa/evidence/</c> (relative to the
    /// Unity project root) for designer review. Used by the difficulty Story 003 BLOCKING tests to emit
    /// the TTB (4×3) and weapon-output (4×8) matrices. Editor-only File I/O in a test context — never
    /// touched at game runtime (control-manifest §1.6 test isolation still holds: this is output, not a
    /// dependency the assertions read back).
    /// </summary>
    public static class DifficultyEvidence
    {
        public static void Write(string filename, string content)
        {
            // Application.dataPath = <project>/Assets → go up one level to the project root.
            string dir = Path.Combine(Application.dataPath, "..", "production", "qa", "evidence");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, filename), content);
        }
    }
}
