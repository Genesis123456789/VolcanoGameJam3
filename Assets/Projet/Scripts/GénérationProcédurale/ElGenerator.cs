using System.Collections.Generic;
using UnityEngine;

public class ElGenerator : MonoBehaviour
{
    [SerializeField] private GameObject pfbCubeToInstanciate;
    [SerializeField] private GameObject pfbDoor;




    private void Start()
    {
        GenerateRoom();
    }




    public void GenerateRoom()
    {
        Vector2 roomsize = DecideRoomSize();
        int sizeX = (int)roomsize.x;
        int sizeZ = (int)roomsize.y;

        GameObject roomGO = new GameObject("Salle");
        SalleProcédurale room = roomGO.AddComponent<SalleProcédurale>();
        room.SizeX = sizeX;
        room.SizeY = sizeZ;

        int doorNumber = Random.Range(1, 3);

        List<(Vector2Int a, Vector2Int b)> doorList = ChooseDoorPositions(sizeX, sizeZ, doorNumber);
        room.Doors = doorList.Count;

        // Toutes les cases où il ne faut pas poser de cube
        HashSet<Vector2Int> doorCells = new HashSet<Vector2Int>();
        foreach (var d in doorList)
        {
            doorCells.Add(d.a);
            doorCells.Add(d.b);
        }

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 1; y < 4; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isWall = x == 0 || x == sizeX - 1 || z == 0 || z == sizeZ - 1;

                    if (!isWall && y > 1) continue;

                    // Trou de la porte : y = 1 et 2, le cube y = 3 reste en linteau
                    if (isWall && y >= 2 && doorCells.Contains(new Vector2Int(x, z))) continue;

                    GameObject cub = Instantiate(pfbCubeToInstanciate, roomGO.transform);
                    cub.transform.position = new Vector3(x, y, z);
                    cub.name = $"Cube {new Vector2(x, z)}";
                    room.Cubes.Add(cub);
                }
            }
        }

        // Une seule porte par paire de cases, posée au milieu
        foreach (var (a, b) in doorList)
        {
            // Pivot du prefab au centre du bas : le bas du cube y=1 est à 0.5
            Vector3 pos = new Vector3((a.x + b.x) / 2f, 2.5f, (a.y + b.y) / 2f);

            // Même z -> le mur court le long de X ; sinon le long de Z
            bool alongX = a.y == b.y;
            Quaternion rot = alongX ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

            GameObject door = Instantiate(pfbDoor, pos, rot, roomGO.transform);
            door.name = $"Door {a}";
        }
    }

    private List<(Vector2Int a, Vector2Int b)> ChooseDoorPositions(int sizeX, int sizeZ, int count)
    {
        List<(Vector2Int a, Vector2Int b)> candidates = new List<(Vector2Int, Vector2Int)>();

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (!CanGenerateDoor(x, z, sizeX, sizeZ)) continue;

                bool wallAlongX = (z == 0 || z == sizeZ - 1);
                int nx = wallAlongX ? x + 1 : x;
                int nz = wallAlongX ? z : z + 1;

                if (!CanGenerateDoor(nx, nz, sizeX, sizeZ)) continue;

                candidates.Add((new Vector2Int(x, z), new Vector2Int(nx, nz)));
            }
        }

        List<(Vector2Int a, Vector2Int b)> result = new List<(Vector2Int, Vector2Int)>();
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        while (result.Count < count && candidates.Count > 0)
        {
            int i = Random.Range(0, candidates.Count);
            var (a, b) = candidates[i];
            candidates.RemoveAt(i);

            if (used.Contains(a) || used.Contains(b)) continue;

            used.Add(a);
            used.Add(b);
            result.Add((a, b));
        }

        return result;
    }
    // Une porte peut aller sur un mur, mais pas dans un coin
    public bool CanGenerateDoor(int x, int z, int sizeX, int sizeZ)
    {
        bool onWall = x == 0 || x == sizeX - 1 || z == 0 || z == sizeZ - 1;
        bool corner = (x == 0 || x == sizeX - 1) && (z == 0 || z == sizeZ - 1);
        return onWall && !corner;
    }




    public Vector2 DecideRoomSize()
    {
        int sizeX = Random.Range(15, 30);
        int sizeY = Random.Range(15, 30);


        return new Vector2(sizeX, sizeY);
    }
}
