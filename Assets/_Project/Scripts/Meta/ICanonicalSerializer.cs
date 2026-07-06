namespace KaijuBreaker.Meta
{
    /// <summary>
    /// Deterministic (canonical) JSON serialization for <see cref="SaveData"/> — the contract the save
    /// pipeline (atomic write, CRC32 integrity, migration) depends on.
    ///
    /// <para><b>Reconciliation (surfaced for review):</b> Story 001 placed this interface in
    /// <c>KaijuBreaker.Core</c>, but it references <see cref="SaveData"/>, which lives in
    /// <c>KaijuBreaker.Meta</c> (per the story's own assembly placement). Core is the zero-dependency base
    /// (ADR-0005) and must not reference Meta, so the interface lives here in Meta alongside its data type.
    /// Nothing outside Meta consumes it, so the boundary is unaffected.</para>
    /// </summary>
    public interface ICanonicalSerializer
    {
        /// <summary>
        /// Serialize <paramref name="data"/> to canonical JSON: every object's keys sorted A–Z (ordinal),
        /// no whitespace, floats in round-trip form, nulls as literal <c>null</c>. Byte-identical for equal
        /// input across platforms and runs.
        /// </summary>
        string Serialize(SaveData data);

        /// <summary>Parse canonical (or any well-formed) JSON produced for this schema back into a <see cref="SaveData"/>.</summary>
        SaveData Deserialize(string json);
    }
}
