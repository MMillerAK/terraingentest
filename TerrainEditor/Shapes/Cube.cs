using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Cube : Shape
{

    public Cube(MeshData meshData)
    {
        this.data = meshData;
    }
    public override MeshData Begin(int vx, int vy, int vz, List<List<List<int>>> voxels, int maxHeight, Chunk chunk)
    {
        bool success = false;
        textureUnits = chunk.TextureUnits;
        tsize = (Vector2)chunk.TextureDimensions / (float)textureUnits;
        results = new MeshData();
        int existsAsVoxel = chunk.GetVoxel(new Vector3I(vx, vy, vz));
        if (existsAsVoxel == -1)
        {
            results.success = false;
            return results;
        }
        success = Make(vx, vy, vz, voxels, maxHeight, chunk);
        if (!success)
        {
            results.success = false;
            return results;
        }
        results.success = true;
        return results;
    }
    private bool Make(int vx, int vy, int vz, List<List<List<int>>> voxels, int maxHeight, Chunk chunk)
    {
        results.success = false;
        Vector3I voxel = new Vector3I(vx, vy, vz);
        if (vy > maxHeight)
        {
            return false;
        }
        if (vx < 0 || vy < 0 || vz < 0)
        {
            return false;
        }
        if (vx >= voxels.Count || vy >= voxels[0].Count || vz >= voxels[0][0].Count)
        {
            return false;
        }
        if (voxels[vx][vy][vz] == -1)
        {
            //no block present
            return false;
        }

        MakeCubeFace(voxel, Chunk.Orientation.up, chunk);

        MakeCubeFace(voxel, Chunk.Orientation.down, chunk);

        MakeCubeFace(voxel, Chunk.Orientation.west, chunk);

        MakeCubeFace(voxel, Chunk.Orientation.east, chunk);

        MakeCubeFace(voxel, Chunk.Orientation.north, chunk);

        MakeCubeFace(voxel, Chunk.Orientation.south, chunk);

        results.success = true;
        return true;
    }

    private void MakeCubeFace(Vector3I voxel, Chunk.Orientation orientation, Chunk chunk)
    {
        Resolution = chunk.Resolution;
        if (!chunk.VoxelExists(voxel))
        {
            return;
        }
        if (chunk.GetVoxel(voxel) == -1)
            return;

        float x = voxel.X * Resolution.X;
        float y = voxel.Y * Resolution.Y;
        float z = voxel.Z * Resolution.Z;


        //actual faces
        switch (orientation)
        {
            case Chunk.Orientation.north:
                MakeNorth(x, y, z);
                break;
            case Chunk.Orientation.south:
                MakeSouth(x, y, z);
                break;
            case Chunk.Orientation.east:
                MakeEast(x, y, z);
                break;
            case Chunk.Orientation.west:
                MakeWest(x, y, z);
                break;
            case Chunk.Orientation.up:
                MakeUp(x, y, z);
                break;
            case Chunk.Orientation.down:
                MakeDown(x, y, z);
                break;


        }
        Vector2 off = Vector2.Zero;


        float startX = 0;
        float startY = 0;
        float endX = 0;
        float endY = 0;

        int textureUnits = chunk.TextureUnits;
        Vector3 dim = chunk.Dimensions; ;
        dim.Y = tsize.Y;
        dim.X = tsize.X;
        dim.Z = tsize.X;

        float vx = voxel.X;
        float vy = voxel.Y;
        float vz = voxel.Z;

        int mdindex = 0;
        if (orientation == Chunk.Orientation.down)
        {

            mdindex = 1;
            startX = (float)vx / (float)textureUnits;
            startY = (float)vz / (float)textureUnits;
            endX = startX + (float)(1 / dim.X);
            endY = startY + (float)(1 / dim.Z);



        }
        if (orientation == Chunk.Orientation.up)
        {

            mdindex = 1;
            startX = (float)vx / (float)textureUnits;
            startY = (float)vz / (float)textureUnits;
            endX = startX + dim.X;
            endY = startY + dim.Z;



        }


        if (orientation == Chunk.Orientation.east)
        {

            startX = (float)vz / (float)tsize.X;
            startY = (float)vy / (float)tsize.Y;
            endX = startX + (float)(1.0f / tsize.X);
            endY = startY + (float)(1.0f / dim.Y);


        }
        if (orientation == Chunk.Orientation.west)
        {

            startX = (float)vz / (float)tsize.X;
            startY = (float)vy / (float)tsize.Y;
            endX = startX + (float)(1 / tsize.X);
            endY = startY + (float)(1 / dim.Y);

        }

        if (orientation == Chunk.Orientation.north)
        {

            startX = (float)vx / (float)tsize.X;
            startY = (float)vy / (float)tsize.Y;
            endX = startX + 1.0f / tsize.X;
            endY = startY + 1.0f / dim.Y;


        }
        if (orientation == Chunk.Orientation.south)
        {

            startX = (float)vx / (float)tsize.X;
            startY = (float)vy / (float)textureUnits;
            endX = startX + 1.0f / tsize.X;
            endY = startY + (float)(1.0f / dim.Y);

        }


        var data = results.GetData(mdindex);

        // startX = +indX / denom;
        // startY = indY / denom;
        // endX = startX + (1 / denom);
        // endY = startY + () / denom;

        List<Vector2> uv = (List<Vector2>)data["uvs"];
        List<int[]> faces = (List<int[]>)data["faces"];
        //  int index = ((List<Vector3>)data["vertices"]).Count() + chunk.GetVertCount();

        results.uvs[mdindex].Add(new Vector2(startX, endY));
        results.uvs[mdindex].Add(new Vector2(endX, endY));
        results.uvs[mdindex].Add(new Vector2(endX, startY));
        results.uvs[mdindex].Add(new Vector2(startX, startY));
        // uvs.Add(new Vector2(0, 1));
        // uvs.Add(new Vector2(1, 1));
        // uvs.Add(new Vector2(1, 0));
        // uvs.Add(new Vector2(0, 0));



        //  List<int> indices = (List<int>)data["indices"];
        int index = this.data.vertices[mdindex].Count + results.vertices[mdindex].Count;
        results.indices[mdindex].Add(index - 4);
        results.indices[mdindex].Add(index - 3);
        results.indices[mdindex].Add(index - 1);
        results.indices[mdindex].Add(index - 3);
        results.indices[mdindex].Add(index - 2);
        results.indices[mdindex].Add(index - 1);
        int faceindex = results.indices.Count - 6;
        int[] face = { faceindex, faceindex + 3 };
    }
    private void MakeUp(float vx, float vy, float vz)
    {

        Vector3[] face =
        {
            new Vector3(vx, vy + Resolution.Y, vz),
            new Vector3(vx + Resolution.X, vy + Resolution.Y, vz),
            new Vector3(vx + Resolution.X, vy + Resolution.Y, vz + Resolution.Z),
            new Vector3(vx, vy + Resolution.Y, vz + Resolution.Z)
        };
        int mdindex = 1;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);
        //    ((List<Vector3>)data["vertices"]).AddRange(face);


        results.normals[mdindex].Add(new Vector3(0, 1, 0));
        results.normals[mdindex].Add(new Vector3(0, 1, 0));
        results.normals[mdindex].Add(new Vector3(0, 1, 0));
        results.normals[mdindex].Add(new Vector3(0, 1, 0));
    }
    private void MakeDown(float vx, float vy, float vz)
    {
        Vector3[] face =
    {
        new Vector3(vx, vy, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy, vz),
        new Vector3(vx, vy , vz)
     };


        int mdindex = 1;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);
        results.normals[mdindex].Add(new Vector3(0, -1, 0));
        results.normals[mdindex].Add(new Vector3(0, -1, 0));
        results.normals[mdindex].Add(new Vector3(0, -1, 0));
        results.normals[mdindex].Add(new Vector3(0, -1, 0));

    }
    private void MakeNorth(float vx, float vy, float vz)
    {
        Vector3[] face =
    {
        new Vector3(vx + Resolution.X, vy, vz + Resolution.Z),
        new Vector3(vx, vy, vz + Resolution.Z),
        new Vector3(vx, vy + Resolution.Y, vz + Resolution.Z),
        new Vector3(vx + Resolution.Z, vy + Resolution.Y, vz + Resolution.Z)
     };




        int mdindex = 0;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);

        results.normals[mdindex].Add(new Vector3(0, 0, 1));
        results.normals[mdindex].Add(new Vector3(0, 0, 1));
        results.normals[mdindex].Add(new Vector3(0, 0, 1));
        results.normals[mdindex].Add(new Vector3(0, 0, 1));
    }
    private void MakeSouth(float vx, float vy, float vz)
    {
        Vector3[] face =
        {
        new Vector3(vx, vy, vz),
        new Vector3(vx + Resolution.X, vy, vz),
        new Vector3(vx + Resolution.X, vy + Resolution.Y, vz),
        new Vector3(vx, vy + Resolution.Y, vz)
        };

        int mdindex = 0;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);


        List<Vector3> normals = (List<Vector3>)data["normals"];
        results.normals[mdindex].Add(new Vector3(0, 0, -1));
        results.normals[mdindex].Add(new Vector3(0, 0, -1));
        results.normals[mdindex].Add(new Vector3(0, 0, -1));
        results.normals[mdindex].Add(new Vector3(0, 0, -1));

    }
    private void MakeEast(float vx, float vy, float vz)
    {
        Vector3[] face =
        {
        new Vector3(vx + Resolution.X, vy, vz),
        new Vector3(vx + Resolution.X, vy, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy + Resolution.Y, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy + Resolution.Y, vz)
        };
        int mdindex = 0;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);

        results.normals[mdindex].Add(new Vector3(1, 0, 0));
        results.normals[mdindex].Add(new Vector3(1, 0, 0));
        results.normals[mdindex].Add(new Vector3(1, 0, 0));
        results.normals[mdindex].Add(new Vector3(1, 0, 0));
    }
    private void MakeWest(float vx, float vy, float vz)
    {
        Vector3[] face =
               {
        new Vector3(vx, vy, vz + Resolution.Z),
        new Vector3(vx, vy, vz),
        new Vector3(vx, vy + Resolution.Y, vz),
        new Vector3(vx, vy + Resolution.Y, vz + Resolution.Z)
        };



        int mdindex = 0;
        var data = results.GetData(mdindex);
        results.vertices[mdindex].AddRange(face);

        results.normals[mdindex].Add(new Vector3(-1, 0, 0));
        results.normals[mdindex].Add(new Vector3(-1, 0, 0));
        results.normals[mdindex].Add(new Vector3(-1, 0, 0));
        results.normals[mdindex].Add(new Vector3(-1, 0, 0));

    }

}


