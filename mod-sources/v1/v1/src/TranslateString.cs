using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static string TranslateString(string stringId)
    {
        return NuStringTable.GetByName(stringId, 0) ?? stringId;
    }
}