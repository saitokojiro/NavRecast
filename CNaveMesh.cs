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
using System.Reflection;
using DotRecast.Recast.Demo.UI;
public class CNaveMesh
{



    public static RcObjImporterContext LoadObject(string path)
    {
        byte[] navData = File.ReadAllBytes(path);
        //Console.WriteLine("navData.Length: " + navData);
        if(navData == null)
        {
            throw new ArgumentOutOfRangeException("Load Object is null");
        }
        return RcObjImporter.LoadContext(navData);
    }

    public DtNavMesh ImpNavMesh(string filePath)
    {
        try
        {

            Console.WriteLine("bin loaded");
            DtNavMesh mesh = null;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);
            DtMeshSetReader reader = new DtMeshSetReader();
            mesh = reader.Read(br, 6);
            if (mesh == null)
            {
                Console.WriteLine("mesh is null");
            }
            else
            {
                Console.WriteLine("mesh not null");
            }

            return mesh;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public NavMeshBuildResult GenerateNavMesh(string pathObj, RcNavMeshBuildSettings config)
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
        /*for (int i = 0; i < context.vertexPositions.Count; i += 3)
        {
            if (i + 2 >= context.vertexPositions.Count)
            {
                throw new Exception(
                    $"Vertex incomplet à l'index {i} : " +
                    $"{context.vertexPositions.Count - i} composants trouvés (3 requis)."
                );
            }
        }*/
        if (context.vertexPositions.Count % 3 != 0)
            throw new Exception("Nombre invalide de coordonnées...");


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
        Console.WriteLine($"[NAVMESH] Vertices : {vertices.Count} (attendu : 1 089)");
        Console.WriteLine($"[NAVMESH] Triangles : {indices.Length / 3} (attendu : 2 048)");
        Console.WriteLine($"Premier vertex : {vertices.First()}");
        Console.WriteLine($"Dernier indice : {indices.Last()}");



        //var geomProvider = new SimpleInputGeomProvider(vertexFloats, indexList);
        var geomProvider = new DemoInputGeomProvider(vertexFloats, indexList);


        //var recastBuilderConfig = new RcBuilderConfig(config, geomProvider.GetMeshBoundsMin(), geomProvider.GetMeshBoundsMax());

        //RcBuilder rcBuilder = new RcBuilder();
        //RcBuilderResult rcResult = rcBuilder.Build(geomProvider, recastBuilderConfig, true);
        var build = new TileNavMeshBuilder();
        var result = build.Build(geomProvider, config);

        //build.Update(geomProvider, config, result, null);
        //RcPolyMesh polyMesh = rcResult.Mesh;
        //RcPolyMeshDetail polyMeshDetail = rcResult.MeshDetail;

        if (result == null || !result.Success)
        {

            throw new Exception("Échec de la génération du NavMesh");
        }

        return result;
    }


    public void PathFinderTest(DtNavMeshQuery query, RcVec3f startPos, RcVec3f endPos, RcVec3f halfExtents)
    {
        var filter = new DtQueryDefaultFilter();
        var startPolyResult = query.FindNearestPoly(startPos, halfExtents, filter, out var startPolyRef, out _, out _);

        if (!startPolyResult.Succeeded())
        {
            Console.WriteLine($"Failed to find start position reference: {startPolyResult}");


        }
        else
        {
            Console.WriteLine($"find start position reference: {startPolyResult}");

        }
        var endPolyResult = query.FindNearestPoly(endPos, halfExtents, filter, out var endPolyRef, out _, out _);
        if (!endPolyResult.Succeeded())
        {
            Console.WriteLine($"Failed to find start position reference: {endPolyResult}");

        }
        //var path = new List<long>();
        var _pathcache = new List<long>();
        var status = query.FindPath(startPolyRef, endPolyRef, startPos, endPos, filter, ref _pathcache, DtFindPathOption.NoOption);
        
        if (status.Succeeded())
        {
            Console.WriteLine($"Chemin trouvé ! {_pathcache.Count} polygones dans le chemin");

            // Optionnel : Convertir les références en positions
            var detailedPath = new List<RcVec3f>();
            var straightPath = new DtStraightPath[256];
            int straightPathCount;
            query.FindStraightPath(startPos, endPos, _pathcache, _pathcache.Count, straightPath, out straightPathCount, 256, 0);
            detailedPath.AddRange(straightPath.Take(straightPathCount).Select(p => new RcVec3f(p.pos.X, p.pos.Y, p.pos.Z)));
            /*Console.WriteLine($"Points nombre : {detailedPath.Count}");
            Console.WriteLine($"Points détaillés : {detailedPath[0]}");
            Console.WriteLine($"Points détaillés : {detailedPath[1]}");
            Console.WriteLine($"Points détaillés : {detailedPath[2]}");*/
        }
        else
        {
            Console.WriteLine($"Chemin pas trouvé ! ");
            Console.WriteLine($"{_pathcache.Count}");
        }

    }

    public List<RcVec3f> PathFinder(DtNavMeshQuery query, RcVec3f startPos, RcVec3f endPos, RcVec3f halfExtents)
    {
        var filter = new DtQueryDefaultFilter();
        var startPolyResult = query.FindNearestPoly(startPos, halfExtents, filter, out var startPolyRef, out _, out _);
        var endPolyResult = query.FindNearestPoly(endPos, halfExtents, filter, out var endPolyRef, out _, out _);
        var _pathcache = new List<long>();
        var status = query.FindPath(startPolyRef, endPolyRef, startPos, endPos, filter, ref _pathcache, DtFindPathOption.AnyAngle);
        
        if(status.Succeeded())
        {
            //Console.WriteLine($"Chemin trouvé ! {_pathcache.Count} polygones dans le chemin");
            var detailedPath = new List<RcVec3f>();
            var straightPath = new DtStraightPath[256];
            int straightPathCount;
            query.FindStraightPath(startPos, endPos, _pathcache, _pathcache.Count, straightPath, out straightPathCount, 256, 0);
            detailedPath.AddRange(straightPath.Take(straightPathCount).Select(p => new RcVec3f(p.pos.X, p.pos.Y, p.pos.Z)));
            return detailedPath;
        }
        else
        {
            return new List<RcVec3f>();
        }
    }

}

