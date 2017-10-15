using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public int playerCount;

    public List<PlayerController> playerControllers { get; protected set; }
    public List<PlayerHub> playerHubs { get; protected set; }

    public GameObject playerControllerPrefab;
    public GameObject playerHubPrefab;
    public GameObject playerHudPrefab;

    [HideInInspector]
    public List<Transform> playerStartPoints;

    // TODO: gather spawn locations
    GameMaster()
    {
        playerCount = 1;
        playerControllers = new List<PlayerController>();
        playerHubs = new List<PlayerHub>();
        playerStartPoints = new List<Transform>();
    }

    void Start()
    {
        GatherPlayerStartPoints();

        for(int i = 0; i < playerCount; ++i)
        {
            playerControllers.Add(Instantiate(playerControllerPrefab,
                                              Vector3.zero,
                                              Quaternion.identity).GetComponent<PlayerController>());

            var babyPlayer = playerControllers[playerControllers.Count - 1];
            Transform startTransform = GetPlayerSpawnPoint(babyPlayer);

            playerHubs.Add(Instantiate(playerHubPrefab,
                                       startTransform.position,
                                       startTransform.rotation).GetComponent<PlayerHub>());

            babyPlayer.Possess(playerHubs[playerHubs.Count - 1]);

            PlayerFPSHUD hud =  Instantiate(playerHudPrefab).GetComponent<PlayerFPSHUD>();
            hud.player = babyPlayer;
        }
    }

    protected void GatherPlayerStartPoints()
    {
        var startlist = GameObject.FindObjectsOfType(typeof(PlayerStart));
        foreach(var start in startlist)
        {
             playerStartPoints.Add((start as PlayerStart).transform);
        }
    }

    protected Transform GetPlayerSpawnPoint(PlayerController player)
    {
        return playerStartPoints[Random.Range(0, playerStartPoints.Count)];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {

    }
#endif
}