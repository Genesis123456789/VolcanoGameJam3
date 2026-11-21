using System.Collections.Generic;
using UnityEngine;

public class ElGenerator : MonoBehaviour
{
    [SerializeField] private GameObject pfbCubeToInstanciate;
    [SerializeField] private GameObject pfbDoor;

    [SerializeField] private GameObject pfbenemy;


    [SerializeField] private List<SalleProcédurale> rooms = new List<SalleProcédurale>();

    private void Start()
    {
        GenerateRoom();
    }



public PorteScript GenerateRoom(PorteScript fromDoor = null)
{
    WallSide? entrySide = fromDoor != null ? Opposite(fromDoor.wall) : (WallSide?)null;

    int sizeX = 0, sizeZ = 0;
    List<(Vector2Int a, Vector2Int b)> doorList = null;
    Vector3 origin = Vector3.zero;

    if (fromDoor == null)
    {
        // Première salle
        Vector2 s = DecideRoomSize();
        sizeX = (int)s.x;
        sizeZ = (int)s.y;
        doorList = ChooseDoorPositions(sizeX, sizeZ, Random.Range(1, 3), null);
    }
    else
    {
        // Direction qui sort de la salle d'où on vient
        Vector3 outward = -InwardDirection(fromDoor.wall);
        bool placed = false;

        for (int attempt = 0; attempt < 15 && !placed; attempt++)
        {
            Vector2 s = DecideRoomSize();
            sizeX = (int)s.x;
            sizeZ = (int)s.y;
            doorList = ChooseDoorPositions(sizeX, sizeZ, Random.Range(2, 4), entrySide);

            // doorList[0] = porte d'entrée de la nouvelle salle
            var (ea, eb) = doorList[0];
            Vector3 entryMid = new Vector3((ea.x + eb.x) / 2f, 0, (ea.y + eb.y) / 2f);

            // Sa porte doit tomber juste à côté de celle de la salle précédente
            origin = fromDoor.doorCenter + outward - entryMid;
            placed = !OverlapsExisting(origin, sizeX, sizeZ);
        }

        if (!placed)
        {
            // Plus de place à côté : on pose la salle plus loin (le joueur sera téléporté)
            float farthest = 0f;
            foreach (var r in rooms)
                farthest = Mathf.Max(farthest, r.transform.position.x + r.SizeX);
            origin = new Vector3(farthest + 10f, 0, 0);
        }
    }

    GameObject roomGO = new GameObject($"Salle {rooms.Count}");
    roomGO.transform.position = origin;

    SalleProcédurale room = roomGO.AddComponent<SalleProcédurale>();
    rooms.Add(room);
    room.SizeX = sizeX;
    room.SizeY = sizeZ;
    room.Doors = doorList.Count;

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
                if (isWall && y >= 2 && doorCells.Contains(new Vector2Int(x, z))) continue;

                GameObject cub = Instantiate(pfbCubeToInstanciate, roomGO.transform);
                cub.transform.localPosition = new Vector3(x, y, z);
                cub.name = $"Cube {new Vector2(x, z)}";
                room.Cubes.Add(cub);
            }
        }
    }

    PorteScript entryDoor = null;
    List<Vector3> safePoints = new List<Vector3>();
    for (int i = 0; i < doorList.Count; i++)
    {
        var (a, b) = doorList[i];

        Vector3 mid = new Vector3((a.x + b.x) / 2f, 0, (a.y + b.y) / 2f);
        bool alongX = a.y == b.y;

        GameObject door = Instantiate(pfbDoor, roomGO.transform);
        door.transform.localPosition = new Vector3(mid.x, 2.5f, mid.z);
        door.transform.localRotation = alongX ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
        door.name = $"Door {a}";

        WallSide side = GetWallSide(a, sizeX, sizeZ);
        PorteScript porte = door.GetComponentInChildren<PorteScript>();
        porte.wall = side;
        porte.generator = this;
        porte.doorCenter = origin + mid;
        porte.arrivalPoint = origin + mid + InwardDirection(side) * 2f;
        safePoints.Add(mid + InwardDirection(side) * 2f); // zone devant chaque porte (coordonnées locales)

        if (entrySide.HasValue && i == 0) entryDoor = porte;
    }

        // Première salle : le joueur spawn au centre
        if (fromDoor == null)
        {
            Vector3 center = new Vector3(sizeX / 2f, 0, sizeZ / 2f);
            safePoints.Add(center);
            TeleportPlayer(origin + center);
        }

        SpawnEnnemies(room, sizeX, sizeZ, safePoints);

        return entryDoor;
        
}

    [SerializeField] private Vector2Int enemyCountRange = new Vector2Int(2, 6); // min inclus, max exclus
    [SerializeField] private float enemySafeRadius = 4f; // distance minimale aux portes / au joueur
    private void SpawnEnnemies(SalleProcédurale room, int sizeX, int sizeZ, List<Vector3> safePoints)
    {
        if (pfbenemy == null) return;

        // Cases du sol : on exclut les murs (bords) et les zones proches des portes
        List<Vector3> freeCells = new List<Vector3>();
        for (int x = 1; x < sizeX - 1; x++)
        {
            for (int z = 1; z < sizeZ - 1; z++)
            {
                Vector3 cell = new Vector3(x, 0, z);

                bool tooClose = false;
                foreach (Vector3 p in safePoints)
                {
                    if (Vector3.Distance(cell, p) < enemySafeRadius) { tooClose = true; break; }
                }

                if (!tooClose) freeCells.Add(cell);
            }
        }

        int count = Mathf.Min(Random.Range(enemyCountRange.x, enemyCountRange.y), freeCells.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, freeCells.Count);
            Vector3 cell = freeCells[index];
            freeCells.RemoveAt(index); // une case = un seul ennemi

            GameObject enemy = Instantiate(pfbenemy, room.transform);
            enemy.transform.localPosition = new Vector3(cell.x, 2f, cell.z); // dessus du cube y=1, à ajuster selon le pivot
            PlaceOnFloor(enemy, room.transform.position.y + 1.5f);
            enemy.name = $"Ennemi ({cell.x}, {cell.z})";
            room.Enemies.Add(enemy);
        }
    }









    // Vrai si un rectangle (X/Z) de cette taille chevauche une salle déjà générée
    private bool OverlapsExisting(Vector3 origin, int sizeX, int sizeZ)
{
    foreach (var r in rooms)
    {
        Vector3 p = r.transform.position;
        bool separated = origin.x + sizeX <= p.x || p.x + r.SizeX <= origin.x
                      || origin.z + sizeZ <= p.z || p.z + r.SizeY <= origin.z;
        if (!separated) return true;
    }
    return false;
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

    private List<(Vector2Int a, Vector2Int b)> ChooseDoorPositions(
    int sizeX, int sizeZ, int count, WallSide? forcedSide = null)
    {
        var candidates = new List<(Vector2Int a, Vector2Int b)>();

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

        var result = new List<(Vector2Int a, Vector2Int b)>();
        var used = new HashSet<Vector2Int>();

        // 1) La porte d'entrée, obligatoire, sur le mur demandé
        if (forcedSide.HasValue)
        {
            var onSide = candidates.FindAll(c => GetWallSide(c.a, sizeX, sizeZ) == forcedSide.Value);
            if (onSide.Count > 0)
            {
                var pick = onSide[Random.Range(0, onSide.Count)];
                used.Add(pick.a);
                used.Add(pick.b);
                result.Add(pick);
            }
        }

        // 2) Les autres portes, au hasard
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

    // Remonte l'objet pour que le bas de son modèle touche le dessus du sol
    private void PlaceOnFloor(GameObject obj, float floorTopY)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        obj.transform.position += Vector3.up * (floorTopY - b.min.y + 0.02f);
    }
    public Vector2 DecideRoomSize()
    {
        int sizeX = Random.Range(15, 30);
        int sizeY = Random.Range(15, 30);


        return new Vector2(sizeX, sizeY);
    }

    // Convention : Nord = +Z, Est = +X
    private WallSide GetWallSide(Vector2Int cell, int sizeX, int sizeZ)
    {
        if (cell.y == 0) return WallSide.South;
        if (cell.y == sizeZ - 1) return WallSide.North;
        if (cell.x == 0) return WallSide.West;
        return WallSide.East;    // cell.x == sizeX - 1
    }



    //////////////////////////////////////////////Gestion de linking et du joueur ////////////////////////////////////////////////



    [SerializeField] private Transform player;
    private int roomCount = 0;

    private float lastTeleportTime = -10f;
    public void UseDoor(PorteScript door)
    {
        // Évite que l'arrivée dans le trigger de la porte te renvoie aussitôt en arrière
        if (Time.time - lastTeleportTime < 0.5f) return;

        if (!door.linkToRoom)
        {
            PorteScript entry = GenerateRoom(door);   
            door.linkedDoor = entry;
            entry.linkedDoor = door;
        }

        TeleportPlayer(door.linkedDoor.arrivalPoint);
    }
    private void TeleportPlayerInFront(PorteScript door)
    {
        Vector3 pos = door.transform.position + InwardDirection(door.wall) * 2f;
        TeleportPlayer(pos);
    }

    private void TeleportPlayer(Vector3 pos)
    {
        pos.y = 2f; // à ajuster selon la taille de ton joueur
        Debug.Log($"Téléportation de '{player.name}' vers {pos}", player);

        
        Rigidbody rb = player.GetComponent<Rigidbody>();

     
        player.position = pos;
        if (rb != null) rb.position = pos;
        Physics.SyncTransforms();
      
    }
    public static WallSide Opposite(WallSide s)
    {
        switch (s)
        {
            case WallSide.East: return WallSide.West;
            case WallSide.West: return WallSide.East;
            case WallSide.North: return WallSide.South;
            default: return WallSide.North;
        }
    }

    // Direction qui part du mur vers l'intérieur de la salle
    private static Vector3 InwardDirection(WallSide s)
    {
        switch (s)
        {
            case WallSide.East: return Vector3.left;
            case WallSide.West: return Vector3.right;
            case WallSide.North: return Vector3.back;
            default: return Vector3.forward;
        }
    }
}


public enum WallSide
{
    East,
    West,
    North,
    South
}