using System;
using System.IO;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast;
using DotRecast.Recast.Toolset;
using DotRecast.Detour;
using DotRecast.Recast.Geom;
using System.Linq.Expressions;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using System.Reflection;
using DotRecast.Recast.Demo.UI;
public class Detour
{
    public Detour()
    {

    }



    
    
    public void run()
    {


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
        recastNavbuildSet.cellSize = 0.05f;
        //recastNavbuildSet.cellHeight = 0.2f;
        recastNavbuildSet.cellHeight = 0.09f;
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
        ///recastNavbuildSet.tiled = true;
        //recastNavbuildSet.tileSize = 32;
        //IInputGeomProvider geo = new InputGeomProvider(vertices, indices);

        NavMeshBuildResult result = GenerateNavMesh("./bin/debug/net9.0/map.obj", recastNavbuildSet);
        if (result.Success)
        {
            Console.WriteLine("Success generete NavMesh");
            var navMesh = result.NavMesh;
            int tileCount = navMesh.GetTileCount();

            int polyCount = 0;
            for (int i = 0; i < navMesh.GetTileCount(); i++)
            {
                var tile = navMesh.GetTile(i);
                if (tile?.data?.header != null)
                {
                    polyCount += tile.data.header.polyCount;
                }
            }

            Console.WriteLine($"nombre de tile: {navMesh.GetTileCount()}");
            Console.WriteLine($"nombre de poly: {polyCount}");

            //detection test

            var query = new DtNavMeshQuery(navMesh);
            var startPos = new RcVec3f(1.8f, 0f, 1.2f);
            var endPos = new RcVec3f(-2.2f, 0f, -2.1f);
             var halfExtents = new RcVec3f(0.1f,0.1f, 0.1f); // Extent for search area
            var filter = new DtQueryDefaultFilter();
            var startPolyResult = query.FindNearestPoly(startPos, halfExtents, filter, out var startPolyRef, out _, out _);

            if (!startPolyResult.Succeeded())
            {
                Console.WriteLine($"Failed to find start position reference: {startPolyResult}");


            }else{
                Console.WriteLine($"find start position reference: {startPolyResult}");

            }
            var endPolyResult = query.FindNearestPoly(endPos, halfExtents, filter, out var endPolyRef, out _, out _);
            if (!endPolyResult.Succeeded())
            {
                Console.WriteLine($"Failed to find start position reference: {endPolyResult}");

            }
            //var path = new List<long>();
            var _pathcache = new List<long>();
            var status = query.FindPath(startPolyRef, endPolyRef, startPos, endPos, filter, ref _pathcache, DtFindPathOption.AnyAngle);
            if (status.Succeeded())
            {
                Console.WriteLine($"Chemin trouvé ! {_pathcache.Count} polygones dans le chemin");

                // Optionnel : Convertir les références en positions
                var detailedPath = new List<RcVec3f>();
                var straightPath = new DtStraightPath[256];
                int straightPathCount;
                query.FindStraightPath(startPos, endPos, _pathcache, _pathcache.Count, straightPath, out straightPathCount, 256, 0);
                detailedPath.AddRange(straightPath.Take(straightPathCount).Select(p => new RcVec3f(p.pos.X, p.pos.Y, p.pos.Z)));
                Console.WriteLine($"Points détaillés : {detailedPath.Count}");
                Console.WriteLine($"Points détaillés : {detailedPath[0]}");
            }
            else{
                Console.WriteLine($"Chemin pas trouvé ! ");
                Console.WriteLine($"{_pathcache.Count}");
            }

        }
        else
        {
            Console.WriteLine("failed generete NavMesh");
        }


    }

    private static NavMeshBuildResult GenerateNavMesh(string pathObj, RcNavMeshBuildSettings config)
    {

        //var context = LoadObject("./bin/debug/net9.0/map.obj");
        var context = LoadObject(pathObj);
        var vertices = new List<RcVec3f>();
        var vertexFloats = new List<float>();
        // var face = new List<int>();
        for (int i = 0; i < context.vertexPositions.Count; i += 3)
        {
            float x = context.vertexPositions[i];
            float y = context.vertexPositions[i + 1];
            float z = context.vertexPositions[i + 2];
            vertices.Add(new RcVec3f(x, y, z));
        }


        foreach (var v in vertices)
        {
            vertexFloats.Add(v.X);
            vertexFloats.Add(v.Y);
            vertexFloats.Add(v.Z);
        }
        /* var indices = context.meshFaces
             .Select(i => (int)i <= 0 ? throw new Exception($"Indice invalide : {i}") : (int)i - 1)
             .ToArray();*/
        var indices = context.meshFaces
        .Select(i => i)
        .ToArray();

        // Vérifier les indices négatifs
        if (indices.Any(idx => idx < 0))
        {
            var invalidIndices = indices
                .Select((val, idx) => new { val, idx })
                .Where(x => x.val < 0)
                .Take(5);

            throw new Exception(
                "Indices de face négatifs trouvés (lignes OBJ avec 'f 0...') :\n" +
                string.Join("\n", invalidIndices.Select(x => $"- Index {x.val} à la position {x.idx}"))
            );
        }
        if (context.vertexPositions.Count % 3 != 0)
        {
            throw new Exception(
                $"Nombre invalide de coordonnées : {context.vertexPositions.Count}\n" +
                "Le fichier OBJ doit contenir des triplets (x,y,z)."
            );
        }
        // Vérifier que chaque vertex est complet
        for (int i = 0; i < context.vertexPositions.Count; i += 3)
        {
            if (i + 2 >= context.vertexPositions.Count)
            {
                throw new Exception(
                    $"Vertex incomplet à l'index {i} : " +
                    $"{context.vertexPositions.Count - i} composants trouvés (3 requis)."
                );
            }
        }


        var indexList = indices.ToList();
        if (indices.Length % 3 != 0)
        {
            throw new Exception($"Nombre d'indices invalide : {indices.Length} (doit être multiple de 3)");
        }

        int maxIndex = indices.Max();
        if (maxIndex >= vertices.Count)
        {
            throw new Exception($"Indice {maxIndex} hors limites (max {vertices.Count - 1})");
        }

        // 5. Logs de vérification
        Console.WriteLine($"=== STATISTIQUES DU MESH ===");
        Console.WriteLine($"Vertices : {vertices.Count} (attendu : 1 089)");
        Console.WriteLine($"Triangles : {indices.Length / 3} (attendu : 2 048)");
        Console.WriteLine($"Premier vertex : {vertices.First()}");
        Console.WriteLine($"Dernier indice : {indices.Last()}");



        //var geomProvider = new SimpleInputGeomProvider(vertexFloats, indexList);
        var geomProvider = new DemoInputGeomProvider(vertexFloats, indexList);


        //var recastBuilderConfig = new RcBuilderConfig(config, geomProvider.GetMeshBoundsMin(), geomProvider.GetMeshBoundsMax());

        //RcBuilder rcBuilder = new RcBuilder();
        //RcBuilderResult rcResult = rcBuilder.Build(geomProvider, recastBuilderConfig, true);
        var build = new TileNavMeshBuilder();
        var result = build.Build(geomProvider, config);
        //RcPolyMesh polyMesh = rcResult.Mesh;
        //RcPolyMeshDetail polyMeshDetail = rcResult.MeshDetail;

        if (result == null || !result.Success)
        {

            throw new Exception("Échec de la génération du NavMesh");
        }

        return result;
    }
    private static RcObjImporterContext LoadObject(string path)
    {
        byte[] navData = File.ReadAllBytes(path);
        Console.WriteLine("navData.Length: " + navData);
        return RcObjImporter.LoadContext(navData);
    }




}

