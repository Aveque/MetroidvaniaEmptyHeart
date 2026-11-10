using System.Collections.Generic;
using UnityEngine;

// Session progress survives death and travel between biomes, but a fresh Play starts fresh.
public static class StationJourney
{
    public static readonly HashSet<string> Visited = new(), Collected = new(), Opened = new();
    public static readonly Dictionary<string,string> Checkpoints = new();
    public static bool BossDefeated, CoreCollected, Dash, Travelling;
    public static int MaxHealth=5, Health=5, Fragments;
    public static string Destination;
    public static Vector2 Arrival;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Visited.Clear();Collected.Clear();Opened.Clear();Checkpoints.Clear();BossDefeated=CoreCollected=Dash=Travelling=false;MaxHealth=Health=5;Fragments=0;Destination=null;}
}
