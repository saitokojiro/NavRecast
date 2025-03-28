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
/*
static RcVec3f Lerp(RcVec3f a, RcVec3f b, float t)
    {
        // Interpolation linéaire entre a et b
        float x = a.X + (b.X - a.X) * t;
        float y = a.Y + (b.Y - a.Y) * t;
        float z = a.Z + (b.Z - a.Z) * t;
        
        return new RcVec3f(x, y, z);
    }*/