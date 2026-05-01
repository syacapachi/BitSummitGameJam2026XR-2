
using System.Collections.Generic;

public interface ISpawnable
{
    public void SpawnFromEvent(List<SpawnEvent> spawnEvents)
    {
        SpawnFromEvent(spawnEvents, false);
    }
    public void SpawnFromEvent(List<SpawnEvent> spawnEvents,bool useRandomSpawn);
}
