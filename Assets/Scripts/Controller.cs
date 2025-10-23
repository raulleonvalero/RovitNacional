using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameObject piece_1, piece_2, piece_3;
    [SerializeField] private Transform spawnPoint_1, spawnPoint_2, spawnPoint_3;

    void Start()
    {
        piece_1.SetActive(true);
        piece_2.SetActive(true);
        piece_3.SetActive(true);

        piece_1.GetComponent<SimpleRespawn>().SetSpawnPoint(spawnPoint_2);
        piece_2.GetComponent<SimpleRespawn>().SetSpawnPoint(spawnPoint_3);
        piece_3.GetComponent<SimpleRespawn>().SetSpawnPoint(spawnPoint_1);

        piece_1.GetComponent<SimpleRespawn>().Respawn();
        piece_2.GetComponent<SimpleRespawn>().Respawn();
        piece_3.GetComponent<SimpleRespawn>().Respawn();
    }
}
