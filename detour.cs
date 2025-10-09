using System;
using System.IO;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Demo;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using DotRecast.Recast.Geom;
using System.Linq.Expressions;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using DotRecast.Recast.Demo.UI;
using Silk.NET.OpenGL;
public class Detour
{
    public CNaveMesh CNaveMesh = new CNaveMesh();
    private DtNavMesh navData;
    //public DtNavMesh mesh = new CNaveMesh().ImpNavMesh("./public/landO_20250305_154249.navmesh");
    public Detour()
    {

    }





    public void run()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        //  var CNaveMesh = new CNaveMesh();

        /* var config = new RcConfig(
         RcPartition.WATERSHED,
         cellSize: 0.3f,
         cellHeight: 0.2f,
         agentMaxSlope: 45.0f,
         agentHeight: 2.0f,
         agentRadius: 0.6f,
         agentMaxClimb: 0.9f,
         regionMinSize: 8,         // Valeur par défaut
         regionMergeSize: 20,      // Valeur par défaut
         edgeMaxLen: 12.0f,        // 12.0f = 12 * cellSize
         edgeMaxError: 1.3f,
         vertsPerPoly: 6,
         detailSampleDist: 6.0f,
         detailSampleMaxError: 1.0f,
         filterLowHangingObstacles: true,
         filterLedgeSpans: true,
         filterWalkableLowHeightSpans: true,
         walkableAreaMod: SampleAreaModifications.SAMPLE_AREAMOD_GROUND,
         buildMeshDetail: true
         );*/

        var recastNavbuildSet = new RcNavMeshBuildSettings();
        //recastNavbuildSet.cellSize = 0.3f;
        //recastNavbuildSet.cellSize = 0.3f;
        recastNavbuildSet.cellSize = 0.5f;
        //recastNavbuildSet.cellHeight = 0.2f;
        //recastNavbuildSet.cellHeight = 0.09f;
        recastNavbuildSet.cellHeight = recastNavbuildSet.cellSize * 0.5f;
        recastNavbuildSet.agentMaxSlope = 45.0f;
        recastNavbuildSet.agentHeight = 2.0f;
        recastNavbuildSet.agentRadius = 0.6f;
        recastNavbuildSet.agentMaxClimb = 0.9f;
        recastNavbuildSet.minRegionSize = 8;         // Valeur par défaut
        recastNavbuildSet.mergedRegionSize = 20;      // Valeur par défaut
        recastNavbuildSet.edgeMaxLen = 12.0f;        // 12.0f = 12 * cellSize
        recastNavbuildSet.edgeMaxError = 1.3f;
        recastNavbuildSet.vertsPerPoly = 6;
        recastNavbuildSet.detailSampleDist = 6.0f;
        recastNavbuildSet.detailSampleMaxError = 1.0f;
        recastNavbuildSet.filterLowHangingObstacles = true;
        recastNavbuildSet.filterLedgeSpans = true;
        recastNavbuildSet.filterWalkableLowHeightSpans = true;
        recastNavbuildSet.tiled = false;
        recastNavbuildSet.tileSize = 256;

        //IInputGeomProvider geo = new InputGeomProvider(vertices, indices);



        //NavMeshBuildResult result = GenerateNavMesh("./bin/debug/net9.0/map.obj", recastNavbuildSet);
        //NavMeshBuildResult result = GenerateNavMesh("./public/landO.obj", recastNavbuildSet);

        //NavMeshBuildResult result = new CNaveMesh().GenerateNavMesh("./public/landO.obj", recastNavbuildSet);
        NavMeshBuildResult result = CNaveMesh.GenerateNavMesh("./public/landO.obj", recastNavbuildSet);
        //var NameMesh = "landO_20250305_154249";
        var NameMesh = "landO";
        var PathMesh = "./public/NavMesh/" + NameMesh + ".navmesh";
        var PathMeshGen = "";
        if (result.Success)
        { // OfFline Generate NavMesh
            var navMesh = result.NavMesh;
            if (null == navMesh)
            {
                Console.WriteLine("navmesh is null");
                return;
            }

            int polyCount = 0;
            for (int i = 0; i < navMesh.GetTileCount(); i++)
            {
                var tile = navMesh.GetTile(i);
                Console.WriteLine($"Tile {i}:");
                if (tile?.data?.header != null)
                {
                    polyCount += tile.data.header.polyCount;
                }
            }

            Console.WriteLine($"nombre: {navMesh.IsAvailableTileCount()}");
            Console.WriteLine($"nombre de tile: {navMesh.GetTileCount()}");
            Console.WriteLine($"nombre de poly: {polyCount}");

            // Vérification des tuiles
            var param = navMesh.GetParams();
            Console.WriteLine("Paramètres du NavMesh:");
            Console.WriteLine($"Origin: {param.orig.X}, {param.orig.Y}, {param.orig.Z}");
            Console.WriteLine($"Tile Size: {param.tileWidth}x{param.tileHeight}");
            Console.WriteLine($"Max Tiles: {param.maxTiles}");
            Console.WriteLine($"Max Polys: {param.maxPolys}");

            // Analyse des tuiles
            int validTileCount = 0;
            int totalPolyCount = 0;

            for (int i = 0; i < navMesh.GetMaxTiles(); i++)
            {
                DtMeshTile tile = navMesh.GetTile(i);
                if (tile == null || tile.data == null || tile.data.header == null)
                    continue;

                validTileCount++;
                var header = tile.data.header;
                totalPolyCount += header.polyCount;

                Console.WriteLine($"Tuile {i}:");
                Console.WriteLine($"  Polys: {header.polyCount}");
                Console.WriteLine($"  Verts: {header.vertCount}");
                Console.WriteLine($"  Bounds: {header.bmin} - {header.bmax}");
            }

            Console.WriteLine($"Tuiles valides: {validTileCount}");
            Console.WriteLine($"Polys totaux: {totalPolyCount}");

            if (validTileCount == 0)
            {
                Console.WriteLine("ERREUR: Aucune tuile valide trouvée après la génération !");
                // Ajoutez ici des vérifications supplémentaires sur la géométrie source
                return;
            }

            DateTime now = DateTime.Now;
            string ymdhms = $"{now:yyyyMMdd_HHmmss}";
            var filename = Path.GetFileNameWithoutExtension(PathMesh);
            Console.WriteLine("filename :" + filename);
            var navmeshFilePath = $"./public/NavMesh/{filename}_{ymdhms}.navmesh";
            PathMeshGen = navmeshFilePath;
            using var fs = new FileStream(navmeshFilePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            var writer = new DtMeshSetWriter();
            writer.Write(bw, navMesh, RcByteOrder.LITTLE_ENDIAN, false);



            Console.WriteLine("NavMesh saved to " + filename);
        }





        //DtNavMesh mesh = null;
        // DtNavMesh mesh = new CNaveMesh().ImpNavMesh("./public/NavMesh/testland.navmesh");
        DtNavMesh mesh = CNaveMesh.ImpNavMesh("./public/NavMesh/landO_20250819_102554.navmesh");
        /*using (var fs = new FileStream(PathMesh, FileMode.Open, FileAccess.Read))
            {
                using (var br = new BinaryReader(fs))
                {
                    DtMeshSetReader reader = new DtMeshSetReader();
                    mesh = reader.Read(br, 7);
                    if (mesh != null)
                    {
                        Console.WriteLine("NavMesh loaded from " + PathMesh);
                    }
                    else
                    {
                        Console.WriteLine("Failed to load NavMesh from " + PathMesh);
                    }
                }
            }*/

        //if (result.Success)
        if (mesh != null)
        {
            Console.WriteLine("Success read NavMesh");
            //var navMesh = result.NavMesh;
            var navMesh = mesh;
            int tileCount = navMesh.GetTileCount();
            Console.WriteLine($"TtileCount: {tileCount}");
            int polyCount = 0;
            for (int i = 0; i < navMesh.GetTileCount(); i++)
            {
                var tile = navMesh.GetTile(i);
                Console.WriteLine($"Tile {i}:");
                if (tile?.data?.header != null)
                {
                    polyCount += tile.data.header.polyCount;
                }
            }

            Console.WriteLine($"nombre: {navMesh.IsAvailableTileCount()}");
            Console.WriteLine($"nombre de tile: {navMesh.GetTileCount()}");
            Console.WriteLine($"nombre de tile: {navMesh.GetMaxTiles()}");
            Console.WriteLine($"nombre de poly: {polyCount}");
            navData = navMesh;
            sw.Stop();
            Console.WriteLine($"[PERF] Build + Save NavMesh : {sw.ElapsedMilliseconds} ms");

        }
        else
        {
            Console.WriteLine("failed generete NavMesh");
            sw.Stop();
            Console.WriteLine($"[PERF] faild try build: {sw.ElapsedMilliseconds} ms");
        }


        
    }

    //detection test
    public void find()
    {
        var query = new DtNavMeshQuery(navData);
        var startPos = new RcVec3f(76.2f, 0.1f, 78.9f);
        var endPos = new RcVec3f(-72.3f, 0.1f, -72.7f);
        var halfExtents = new RcVec3f(0.1f, 0.1f, 0.1f); // Extent for search area

        //Console.WriteLine($"pathfinder 1 done");
        //CNaveMesh.PathFinder(query, startPos, endPos, halfExtents);
        //permet de lancer plusieurs pathfinder, test de rapidité
        for (int i = 0; i < 1; i++)
        {
            Console.WriteLine($"pathfinder {i + 1} done");
            var path = CNaveMesh.PathFinder(query, startPos, endPos, halfExtents);

            //test des paths 
            Console.WriteLine($"Points nombre : {path.Count}");
            //utiliser de donner test donc a constament 14
            if (path.Count >= 3)
            {
                Console.WriteLine($"Points détaillés : {path[0]}");
                Console.WriteLine($"Points détaillés : {path[1]}");
                Console.WriteLine($"Points détaillés : {path[2]}");
            }
            else
            {
                Console.WriteLine($"Points détaillés : {path[0]}");
            }


        }
    }













}

