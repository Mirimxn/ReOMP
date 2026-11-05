namespace OMP.LSWTSS;

partial class Overlay1
{
    static void SortInstances()
    {
        _instances.Sort((a, b) => a.Order.CompareTo(b.Order));
    }
}