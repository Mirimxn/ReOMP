using System.Linq;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    static void OnUpdate()
    {
        var currentApiWorld = V1.GetCApi1CurrentApiWorld();

        if (currentApiWorld == nint.Zero)
        {
            return;
        }

        var universeProcessingScopeHandle = (NttUniverseProcessingScope.NativeHandle)Marshal.AllocHGlobal(
            Marshal.SizeOf<NttUniverseProcessingScope.NativeData>()
        );

        universeProcessingScopeHandle.Constructor(currentApiWorld.GetUniverse(), true);

        var apiWorldProcessingScopeHandle = (ApiWorldProcessingScope.NativeHandle)Marshal.AllocHGlobal(
            Marshal.SizeOf<ApiWorldProcessingScope.NativeData>()
        );

        apiWorldProcessingScopeHandle.Constructor(currentApiWorld, true);

        if (_charactersInfo == null)
        {
            _charactersInfo = V1.FetchCharactersInfo();
        }

        _playerEntityLastPosition = GetPlayerEntityPosition();

        _spawnerInPlayerEntityRange = _spawners.FirstOrDefault(spawner => spawner.IsInPlayerEntityRange);

        foreach (var spawnNpcTask in _spawnNpcTasks.ToList())
        {
            spawnNpcTask.Update();
        }

        foreach (var npc in _npcs.ToList())
        {
            npc.Update();
        }

        foreach (var spawner in _spawners.ToList())
        {
            spawner.Update();
        }

        apiWorldProcessingScopeHandle.Destructor();

        Marshal.FreeHGlobal(apiWorldProcessingScopeHandle);

        universeProcessingScopeHandle.Destructor();

        Marshal.FreeHGlobal(universeProcessingScopeHandle);
    }
}