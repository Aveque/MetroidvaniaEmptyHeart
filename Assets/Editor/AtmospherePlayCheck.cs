#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AtmospherePlayCheck
{
    public static void Validate()
    {
        StationParallax background = Object.FindFirstObjectByType<StationParallax>();
        Require(background != null && background.Planes.Length == 3, "Three parallax planes are missing.");
        StationMusic music = StationMusic.Instance;
        Require(music != null && music.ExplorationTheme != null && music.BossTheme != null, "Music assets missing.");
        Require(music.ExplorationTheme.length > 100 && music.BossTheme.length > 68, "Music clip is incomplete.");
        Require(music.ExplorationSource.loop && music.BossSource.loop, "Music is not looping.");
        Require(!music.InBossMode && music.ExplorationSource.isPlaying, "Exploration music did not start.");
        Require(AudioListener.volume > 0 && !AudioListener.pause, "Game audio listener is muted.");
        Require(HasSignal(music.ExplorationSource), "Exploration source produces no audio samples.");
        Camera camera = Camera.main; Vector3 saved = camera.transform.position; float size = camera.orthographicSize;
        try
        {
            camera.transform.position = new Vector3(0, 0, saved.z); background.UpdatePlanes();
            Vector3[] from = new Vector3[3];
            for(int i=0;i<3;i++) from[i] = background.Planes[i].root.position;
            camera.transform.position = new Vector3(2, 2, saved.z); background.UpdatePlanes();
            for(int i=0;i<3;i++)
            {
                StationParallax.Plane layer = background.Planes[i];
                Vector3 delta = layer.root.position - from[i];
                Require(Mathf.Abs(delta.x - 2*(1-layer.parallax.x)) < .001f &&
                    Mathf.Abs(delta.y - 2*(1-layer.parallax.y)) < .001f, "Incorrect parallax speed: " + layer.name);
                Require(layer.sortingOrder < -1, "Background could cover the collision tiles.");
            }
            // Vertical shafts, large room snaps and the wider boss framing must have coverage.
            foreach(Vector2 position in new[] { new Vector2(-100,-80), Vector2.zero, new Vector2(33,25), new Vector2(74,2), new Vector2(120,60) })
            {
                camera.transform.position = new Vector3(position.x, position.y, saved.z);
                camera.orthographicSize = 6.5f; background.UpdatePlanes();
                StationParallax.Plane layer = background.Planes[0];
                for(int x=0;x<3;x++) for(int y=0;y<3;y++)
                {
                    Vector3 p = new Vector3(position.x + (x-1)*camera.orthographicSize*camera.aspect,
                        position.y + (y-1)*camera.orthographicSize, 0);
                    bool covered = false;
                    foreach(SpriteRenderer tile in layer.tiles)
                        if(tile.enabled && tile.bounds.Contains(p)) covered = true;
                    Require(covered, "Distant background has a gap after a camera snap.");
                }
            }
            Require(background.GetComponentsInChildren<Collider2D>().Length == 0, "Background colliders affect movement.");
        }
        finally { camera.transform.position = saved; camera.orthographicSize = size; background.UpdatePlanes(); }
        Debug.Log("ATMOSPHERE CHECK: all depth ratios, camera-snap coverage, sorting, no colliders, streaming clips and actual audio output passed.");
    }

    public static bool HasSignal(AudioSource source)
    {
        float[] data = new float[2048]; source.GetOutputData(data, 0);
        float energy = 0; foreach(float value in data) energy += value*value;
        return energy/data.Length > 0.00000001f;
    }
    private static void Require(bool ok,string message) { if(!ok) throw new Exception("ATMOSPHERE CHECK FAILED: " + message); }
}
#endif
