using System.Linq;
using UnityEngine;

public class KillEnemiesCondition : MonoBehaviour
{

    public HealthModule[] enemies;
    int enemyCount = 0;
    int totalEnemies = 0;

    

    bool isMet = false;
    void Start()
    {
        
    }

    
    void HandleDeath()
    {
        enemyCount++;

        if (enemyCount >= enemies.Count())
        {
            isMet = true;
        }
    }

    void GiveReward()
    {

    }


    void SetUpTracking()
    {
      foreach(HealthModule enemy in enemies)
        {
            enemy.onDeath += HandleDeath;
            totalEnemies++;
        }
    }
}
