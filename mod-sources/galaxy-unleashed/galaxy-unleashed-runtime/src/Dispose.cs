using System;
using System.Linq;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed : IDisposable
{
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _gameFrameworkProcessMethodHook.Dispose();

        _inputHookClient.Dispose();

        _overlay.IsActive = false;

        _overlay.Dispose();

        using (new ProcessingScope())
        {
            _jetpackBooster.Dispose();

            _jumpBooster.Dispose();

            foreach (var npcSpawner in _npcSpawners.ToList())
            {
                npcSpawner.Dispose();
            }

            _npcSpawners.Clear();

            foreach (var spawnNpcTask in _spawnNpcTasks.ToList())
            {
                spawnNpcTask.Dispose();
            }

            _spawnNpcTasks.Clear();

            foreach (var npc in _npcs.ToList())
            {
                npc.Dispose();
            }

            _npcs.Clear();

            _battle.Dispose();
        }

        _isDisposed = true;
    }
}