using System.Collections.Generic;

namespace DataModels
{
    public class CollectionMinSizeComparer : IComparer<Collection>
    {
        public int Compare(Collection x, Collection y)
        {
            if (x.Size > y.Size) return 1;
            if (x.Size < y.Size) return -1;
            else return 0;
        }
    }
}