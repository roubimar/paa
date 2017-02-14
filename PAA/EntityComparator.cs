using System.Collections.Generic;
using System.Linq;

namespace PAA
{
    /// <summary>
    /// Comparator for sorting Entities Conservatively
    /// </summary>
    public class EntityComparator : IComparer<Entity>
    {
        public static EntityComparator Instance = new EntityComparator();

        public int Compare(Entity x, Entity y)
        {
            if (x.Fitness == y.Fitness)
            {
                if (x.CorrectClausules == y.CorrectClausules)
                {
                    if (x.Weight == y.Weight)
                    {
                        return 0;
                    }
                    return x.Weight > y.Weight ? -1 : 1;
                }
                return x.CorrectClausules > y.CorrectClausules ? -1 : 1;
            }
            return x.Fitness > y.Fitness ? -1 : 1;
        }
    }

    /// <summary>
    /// Comparator for sorting Entities
    /// </summary>
    public class CustomEntityComparator : IComparer<Entity>
    {
        public static CustomEntityComparator Instance = new CustomEntityComparator();

        public int Compare(Entity x, Entity y)
        {
            var xImpactIndex = x.GenomImpactClausules.Sum();
            var yImpactIndex = y.GenomImpactClausules.Sum();

            if (yImpactIndex == xImpactIndex)
                return 0;
            return xImpactIndex > yImpactIndex ? 1 : -1;
        }
    }
}
