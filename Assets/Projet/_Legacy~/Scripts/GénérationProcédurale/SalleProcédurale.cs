using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SalleProcédurale : MonoBehaviour
{
    [SerializeField] private int sizeX;
    [SerializeField] private int sizeY;

    [SerializeField] private int doors;

    [SerializeField] private List<GameObject> cubes = new List<GameObject>();

    [SerializeField] private List<GameObject> portes = new List<GameObject>();

    [SerializeField] private List<GameObject> enemies = new List<GameObject>();
    public List<GameObject> Enemies => enemies;
    public int SizeX { get => sizeX; set => sizeX = value; }
    public int SizeY { get => sizeY; set => sizeY = value; }
    public int Doors { get => doors; set => doors = value; }
    public List<GameObject> Cubes { get => cubes; set => cubes = value; }

}
