using Godot;
using System;
using System.Collections.Generic;

public class Ramp : Shape
{
    public Ramp(MeshData meshData, Chunk.Orientation orientation)
    {
        this.data = meshData;
    }

    public override MeshData Begin(int vx, int vy, int vz, List<List<List<int>>> voxels, int maxHeight, Chunk chunk)
    {
        throw new NotImplementedException();
    }

}
