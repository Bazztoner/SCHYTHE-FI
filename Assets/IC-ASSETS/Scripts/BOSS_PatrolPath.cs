using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BOSS_PatrolPath : PatrolPath
{
	public void AddEnemy(EnemyController enemy)
    {
        enemiesToAssign.Add(enemy);
    }
}
