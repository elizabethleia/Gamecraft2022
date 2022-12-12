using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using veci2 = UnityEngine.Vector2Int;

[System.Serializable]
public struct BiomeType
{
    //public string name;
    public Element element;
    public string name;
    public Color colour;
}


public class Region
{
    public BiomeType biome;
    public veci2 Center = veci2.zero;
}
public class VoronoiMapGen : MonoBehaviour
{
    public static VoronoiMapGen _;
    public int biomeGrid;
    [Tooltip("Wiggly")]
    public float noiseMult;
    [Tooltip("Jagged")]
    public float noiseDist;

    public int seed;
    public BiomeType[] biomes;
    public RectInt mapSize;
    public MeshRenderer map;
    public bool autoUpdate = false;
    public Dictionary<veci2, Region> tiles = new();
    public bool isGened = false;
    public Region[] regions = new Region[0];

    private void Awake()
    {
        _ = this;
    }

    private void Start()
    {
        GenerateMap();
    }

    public Region GetRegion(Vector2 loc)
    {
        var mapScalar = new Vector2(map.transform.localScale.x, map.transform.localScale.y) / mapSize.size;
        var newLoc =  loc * mapScalar;
        var tileLoc = new veci2(Mathf.RoundToInt(newLoc.x), Mathf.RoundToInt(newLoc.y));
        tiles.TryGetValue(tileLoc, out Region foundReg);
        return foundReg;
    }

    public void GenerateMap()
    {
        tiles.Clear();
        int[,] biomesMap = new int[mapSize.width, mapSize.height];
        Color[] biomesColorMap = new Color[mapSize.width * mapSize.height];

        System.Random prng = new System.Random(seed);
        SeedRandom.SetSeed(seed);

        int xS = prng.Next(10, 20);
        int yS = prng.Next(10, 20);

        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);
        Dictionary<veci2, Region> regionGrid = new();


        for (int gridX = 0; gridX < mapSize.width/biomeGrid; gridX++)
        {
            for (int gridY = 0; gridY < mapSize.height/biomeGrid; gridY++)
            {
                var gridVec = new veci2(gridX, gridY);
                //This algo of voronoi generation will make center always be in top right or bottom left quadrants of the gridCell
                int centerScalar = SeedRandom.Get(gridX, gridY) % biomeGrid;
                veci2 center = new veci2(centerScalar + gridX * biomeGrid, centerScalar + gridY * biomeGrid);
                BiomeType selectedBiome = biomes[SeedRandom.Get(gridX, gridY) % biomes.Length];
                var region = new Region() { biome = selectedBiome, Center = center };
                regionGrid.Add(gridVec, region);
            }
        }

        for (int x = 0; x < mapSize.width; x++)
        {
            for (int y = 0; y < mapSize.height; y++)
            {
                int gridX = (int)Mathf.Floor(x / biomeGrid);
                int gridY = (int)Mathf.Floor(y / biomeGrid);
                ////This algo of voronoi generation will make center always be in top right or bottom left quadrants of the gridCell
                //int centerScalar = SeedRandom.Get(gridX, gridY) % biomeGrid;
                //veci2 center = new veci2(centerScalar + gridX * biomeGrid, centerScalar + gridY * biomeGrid);

                //int x1 = gridX * biomeGrid - mapSize.xMin;
                //int y1 = gridY * biomeGrid - mapSize.yMin;
                //Debug.DrawLine(new Vector2(x1, 0), new Vector2(x1, 100000), Color.blue, 50f);
                //Debug.DrawLine(new Vector2(0, y1), new Vector2(100000, y1), Color.blue, 50f);
                //int loc = SeedRandom.Get(gridX, gridY) % biomeGrid;
                //KongrooUtils.DrawDebugCircle(new Vector2(loc + x1, loc + y1), 1f, Color.black, 10, 50f);
               
                

                if (x / biomeGrid - gridX > 0.5f)
                    gridX -= 2;
                else
                    gridX -= 1;

                if (y / biomeGrid - gridY > 0.5f)
                    gridY -= 2;
                else
                    gridY -= 1;


                int closest = 0;
                int closestDist = int.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        
                        #region Voronoi
                        int curBiome = i * 4 + j;
                        int biomeX = SeedRandom.Get(gridX + i, gridY + j) % biomeGrid;
                        int biomeY = SeedRandom.Get(gridX + i, gridY + j) % biomeGrid;

                        int dist = ((gridX + i) * biomeGrid + biomeX - x) * ((gridX + i) * biomeGrid + biomeX - x) +
                                   ((gridY + j) * biomeGrid + biomeY - y) * ((gridY + j) * biomeGrid + biomeY - y);
                        #endregion

                        #region Perlin
                        dist += (int)(Mathf.PerlinNoise(noiseDist * ((gridX + i) * biomeGrid + biomeX - x + offsetX) / 100f,
                                                         noiseDist * ((gridY + j) * biomeGrid + biomeY - y + offsetY) / 100f) * noiseMult);
                        #endregion
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = curBiome;
                        }
                    }
                }

                //biomesMap[x, y] = SeedRandom.Get(gridX + closest / 4, gridY + closest % 4) % biomes.Length;
                //biomesColorMap[y * mapSize.height + x] = biomes[biomesMap[x, y]].colour;
                var selectedGridCell = new veci2(gridX + closest / 4, gridY + closest % 4);
                if (regionGrid.TryGetValue(selectedGridCell, out var selectedRegion))
                {
                    biomesColorMap[y * mapSize.height + x] = selectedRegion.biome.colour;
                    tiles.Add(new veci2(x, y), selectedRegion);

                }

            }
        }

        regions = regionGrid.Values.ToArray();
        isGened = true;
        var imageTex = new Texture2D(mapSize.width, mapSize.height);
        imageTex.filterMode = FilterMode.Point;
        imageTex.wrapMode = TextureWrapMode.Clamp;
        imageTex.SetPixels(biomesColorMap);
        imageTex.Apply();
        map.material.SetTexture("_MainTex", imageTex);
    }

    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var region in regions)
        {
            Gizmos.DrawWireSphere((Vector2)region.Center, 1);
        }

        //for (int x = 0; x < mapSize.width; x++)
        //{
        //    for (int y = 0; y < mapSize.height; y++)
        //    {
        //        int gridX = (int)Mathf.Floor(x / biomeGrid);
        //        int gridY = (int)Mathf.Floor(y / biomeGrid);
        //        int centerScalar = SeedRandom.Get(gridX, gridY) % biomeGrid;
        //        veci2 center = new veci2(centerScalar + gridX * biomeGrid, centerScalar + gridY * biomeGrid);
        //        int x1 = gridX * biomeGrid - mapSize.xMin;
        //        int y1 = gridY * biomeGrid - mapSize.yMin;

        //        Gizmos.color = Color.blue;
        //        Gizmos.DrawLine(new Vector2(x1, 0), new Vector2(x1, 100000));
        //        Gizmos.DrawLine(new Vector2(0, y1), new Vector2(100000, y1));
        //    }
        //}
    }
}
