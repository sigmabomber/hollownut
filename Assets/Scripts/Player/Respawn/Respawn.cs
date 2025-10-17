using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class Respawn : MonoBehaviour
{

    private Transform currentCheckpoint;
    public Transform defaultCheckPoint;
    public Action commitRespawn;
    public Action<Transform> changeCheckpoint;
    public static Respawn Instance;
    private HealingStation healingStation;
    private Transform follow;
    public RespawnUI respawnUI;




    
    void Start()
    {

        Instance = this;
        commitRespawn = () => StartCoroutine(StartRespawnCharacter());
        changeCheckpoint += changeCheckpoint;

        currentCheckpoint = defaultCheckPoint;
        healingStation = defaultCheckPoint.GetComponent<HealingStation>();
      
    }

    IEnumerator StartRespawnCharacter()
    {
        yield return new WaitForSeconds(1f);
        if (currentCheckpoint == null) yield break ;

        yield return StartCoroutine(respawnUI.FadeIn());


    }

    bool ChangeCheckPoint(Transform checkpoint)
    {
        if (checkpoint == null) return false;

        if (checkpoint == currentCheckpoint) return false;

        currentCheckpoint = checkpoint;
        healingStation = checkpoint.GetComponent<HealingStation>();
        return true;
    }

   
}
