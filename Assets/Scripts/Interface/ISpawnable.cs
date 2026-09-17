
using System.Collections.Generic;

public interface ISpawnable
{
    public void SetRandomSeedServerOnly(int seed);
    public void SpawnFromEvent(SpawnSetting spawnSetting);
}
