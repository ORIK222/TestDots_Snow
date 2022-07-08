using System.Collections.Generic;
using Download.Core.Editor;

public class GlbDataMinSizeComparer : IComparer<GLBData>
{
    public int Compare(GLBData x, GLBData y)
    {
        if (x.Size > y.Size) return 1;
        if (x.Size < y.Size) return -1;
        else return 0;
    }
}
