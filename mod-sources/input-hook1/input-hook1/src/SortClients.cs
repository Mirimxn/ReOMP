namespace OMP.LSWTSS;

partial class InputHook1
{
    static void SortClients()
    {
        lock (_clients)
        {
            _clients.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}