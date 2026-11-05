using System.Linq;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    void Update()
    {
        UpdateOverlay();

        var currentApiWorld = V1.GetCApi1CurrentApiWorld();

        if (currentApiWorld == nint.Zero)
        {
            return;
        }

        using (new ProcessingScope())
        {
            _jetpackBooster.Update();

            _jumpBooster.Update();

            UpdateCharactersInfo();

            _playerEntityLastPosition = FetchPlayerEntityPosition();

            _closestNpcSpawnerInPlayerEntityRange = _npcSpawners
                .Where(npcSpawner => npcSpawner.IsInPlayerEntityRange)
                .OrderBy(npcSpawner => npcSpawner.DistanceToPlayerEntity)
                .FirstOrDefault();

            foreach (var npcSpawner in _npcSpawners.ToArray())
            {
                npcSpawner.Update();
            }

            foreach (var spawnNpcTask in _spawnNpcTasks.ToArray())
            {
                spawnNpcTask.Update();
            }

            foreach (var npc in _npcs.ToArray())
            {
                npc.Update();
            }

            _battle.Update();
        }
    }
}