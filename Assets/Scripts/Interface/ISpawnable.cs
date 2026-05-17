
using System.Collections.Generic;

public interface ISpawnable
{
    public void SetRandomSeed(int seed);
    public void SpawnFromEvent(SpawnSetting spawnSetting);
}
