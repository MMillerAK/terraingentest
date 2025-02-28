using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public abstract class Shape
{
    protected MeshData data;
    // protected List<int[]> faces = new();
    //protected List<Vector3> vertices = new();
    //  protected List<Vector2> uvs = new();
    // protected List<Vector3> normals = new();
    //   protected List<int> indices = new();


    // protected Godot.Collections.Dictionary<int, int> triToFaceIndex = new();

    public MeshData results;

    protected MeshDataTool globalMeshTool = new();

    protected static Vector3 Resolution;

    protected float textureUnits;
    protected Vector2 tsize;


    public abstract MeshData Begin(int vx, int vy, int vz, List<List<List<int>>> voxels, int maxHeight, Chunk chunk);


}