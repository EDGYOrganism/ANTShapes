using MIConvexHull;
using UnityEngine;
using System.Collections.Generic;

public class ConvexVertex : IVertex
{
    public double[] Position { get; set; }

    public ConvexVertex(Vector3 point)
    {
        Position = new double[] { point.x, point.y, point.z };
    }
}

public static class ConvexHullUtil
{
    private const int MaxTriangles = 256;
    private const int MaxInputVertices = 256;

    public static Mesh GenerateConvexHullMesh(Mesh sourceMesh)
    {
        if (sourceMesh == null)
        {
            Debug.LogError("Source mesh is null.");
            return null;
        }

        Vector3[] originalVertices = sourceMesh.vertices;

        // Sample input vertices down to MaxInputVertices
        List<ConvexVertex> inputPoints = new List<ConvexVertex>();
        int step = Mathf.Max(1, originalVertices.Length / MaxInputVertices);
        for (int i = 0; i < originalVertices.Length; i += step)
        {
            inputPoints.Add(new ConvexVertex(originalVertices[i]));
        }

        // Compute the convex hull
        var result = ConvexHull.Create<ConvexVertex, DefaultConvexFace<ConvexVertex>>(inputPoints);
        if (result == null || result.Result == null)
        {
            Debug.LogError("Convex hull generation failed.");
            return null;
        }

        var hull = result.Result;

        // Copy result to indexable lists
        List<ConvexVertex> hullVerticesRaw = new List<ConvexVertex>(hull.Points);
        List<DefaultConvexFace<ConvexVertex>> faces = new List<DefaultConvexFace<ConvexVertex>>(hull.Faces);

        // Create vertex list
        List<Vector3> hullVertices = new List<Vector3>();
        for (int i = 0; i < hullVerticesRaw.Count; i++)
        {
            double[] pos = hullVerticesRaw[i].Position;
            hullVertices.Add(new Vector3((float)pos[0], (float)pos[1], (float)pos[2]));
        }

        // Create triangle index list
        List<int> triangles = new List<int>();
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];

            int ia = FindIndex(hullVerticesRaw, face.Vertices[0]);
            int ib = FindIndex(hullVerticesRaw, face.Vertices[1]);
            int ic = FindIndex(hullVerticesRaw, face.Vertices[2]);

            if (ia >= 0 && ib >= 0 && ic >= 0)
            {
                triangles.Add(ia);
                triangles.Add(ib);
                triangles.Add(ic);
            }
        }

        // If triangle count exceeds limit, warn
        if (triangles.Count / 3 > MaxTriangles)
        {
            Debug.LogWarning("Convex hull exceeds max triangle count. Consider reducing MaxInputVertices.");
        }

        // Final mesh
        Mesh convexHullMesh = new Mesh();
        convexHullMesh.vertices = hullVertices.ToArray();
        convexHullMesh.triangles = triangles.ToArray();
        convexHullMesh.RecalculateNormals();
        convexHullMesh.RecalculateBounds();

        return convexHullMesh;
    }

    private static int FindIndex(List<ConvexVertex> list, ConvexVertex item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == item)
                return i;
        }
        return -1;
    }
}
