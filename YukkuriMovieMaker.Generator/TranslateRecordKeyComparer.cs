using System.Collections.Generic;

#pragma warning disable RS1036
namespace YukkuriMovieMaker.Generator
{
    class TranslateRecordKeyComparer : IEqualityComparer<TranslateRecord>
    {
        public bool Equals(TranslateRecord x, TranslateRecord y) => x.Key == y.Key;
        public int GetHashCode(TranslateRecord obj) => obj?.Key?.GetHashCode() ?? 0;
    }
}
