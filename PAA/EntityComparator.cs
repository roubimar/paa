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
                var xGenomImpactSum = x.GenomImpactClausules.Sum();
                var yGenomImpactSum = y.GenomImpactClausules.Sum();
                if (xGenomImpactSum == yGenomImpactSum)
                {
                    if (x.Weight == y.Weight)
                    {
                        return 0;
                    }
                    return x.Weight > y.Weight ? -1 : 1;
                }
                return xGenomImpactSum > yGenomImpactSum ? -1 : 1;
            }
            return x.Fitness > y.Fitness ? -1 : 1;
        }
    }
}
