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
public class Detour
{
    public CNaveMesh CNaveMesh = new CNaveMesh();
    public DtNavMesh mesh = new CNaveMesh().ImpNavMesh("./public/landO_20250305_154249.navmesh");
    public Detour()
    {
        
    }





    public void run()
    {

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

        

        //NavMeshBuildResult result = GenerateNavMesh("./bin/debug/net9.0/map.obj", recastNavbuildSet);
        //NavMeshBuildResult result = GenerateNavMesh("./public/landO.obj", recastNavbuildSet);

        //NavMeshBuildResult result = new CNaveMesh().GenerateNavMesh("./public/landO.obj", recastNavbuildSet);
        
       // if (result.Success)
        if (mesh != null)
        {
            Console.WriteLine("Success generete NavMesh");
            //var navMesh = result.NavMesh;
            var navMesh = mesh;
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
            var startPos = new RcVec3f(76.2f, -0.1f, 78.9f);
            var endPos = new RcVec3f(-72.3f, -0.1f, -72.7f);
            var halfExtents = new RcVec3f(0.1f, 0.1f, 0.1f); // Extent for search area

            Console.WriteLine($"pathfinder 1 done");
            CNaveMesh.PathFinderTest(query, startPos, endPos, halfExtents);
            for(int i = 0; i < 5000; i++)
            {
                Console.WriteLine($"pathfinder {i + 1} done");
                CNaveMesh.PathFinderTest(query, startPos, endPos, halfExtents);
            }
            

        }
        else
        {
            Console.WriteLine("failed generete NavMesh");
        }
        


    }

    public void find(){

    }

    











}

