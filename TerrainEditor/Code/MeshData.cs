using Godot;
using System;

using System.Collections.Generic;
public partial class MeshData
{



    public bool success = false;
    public List<List<int[]>> faces = new();
    public List<List<Vector3>> vertices = new();
    public List<List<Vector2>> uvs = new();
    public List<List<Vector3>> normals = new();
    public List<List<int>> indices = new();

    int channels = 2;
    //    List<Godot.Collections.Dictionary<int, int>> triToFaceIndex = new();

    private List<Dictionary<string, Object>> data = new();

    public MeshData()
    {
        Clear();
    }


    public List<Vector3> combinedMesh()
    {
        List<Vector3> combined = new();
        for (int i = 0; i < vertices.Count; i++)
        {
            for (int j = 0; j < vertices[i].Count; i++)
            {
                combined.Add(vertices[i][j]);
            }
        }

        return combined;
    }

    public Godot.Collections.Array[] construct()
    {

        Godot.Collections.Array[] arr = new Godot.Collections.Array[data.Count];

        ArrayMesh meshes = new();
        for (int d = 0; d < channels; d++)
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = vertices[d].ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices[d].ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = uvs[d].ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = normals[d].ToArray();

            arr[d] = arrays;



        }


        return arr;
    }
    public void Clear()
    {
        faces = new();

        vertices = new();
        uvs = new();
        normals = new();
        indices = new();

        data = new();

        Add();
        Add();



        //  triToFaceIndex = new();
    }

    public void Add()
    {
        faces.Add(new());
        vertices.Add(new());
        uvs.Add(new());
        normals.Add(new());
        indices.Add(new());
        Dictionary<string, Object> newdata = new();
        newdata["vertices"] = vertices[vertices.Count - 1];
        newdata["normals"] = normals[normals.Count - 1];
        newdata["uvs"] = uvs[uvs.Count - 1];
        newdata["faces"] = faces[faces.Count - 1];
        newdata["indices"] = indices[indices.Count - 1];
        data.Add(newdata);


    }

    public void Append(MeshData newdata)
    {

        appendIndex(newdata.data[0], 0);
        appendIndex(newdata.data[1], 1);

    }

    private void appendIndex(Dictionary<string, Object> newData, int index)
    {
        while (index >= data.Count)
        {
            Add();
        }
        vertices[index].AddRange((IEnumerable<Vector3>)newData["vertices"]);
        normals[index].AddRange((IEnumerable<Vector3>)newData["normals"]);
        uvs[index].AddRange((IEnumerable<Vector2>)newData["uvs"]);
        faces[index].AddRange((IEnumerable<int[]>)newData["faces"]);
        indices[index].AddRange((IEnumerable<int>)newData["indices"]);

        data[index]["vertices"] = vertices;
        data[index]["normals"] = normals;
        data[index]["uvs"] = uvs;
        data[index]["faces"] = faces;
        data[index]["indices"] = indices;

    }
    public Dictionary<string, Object> GetData(int index)
    {
        while (index >= data.Count)
        {
            Add();


        }

        return data[index];
    }
    public void SetData(int index, Dictionary<string, Object> data)
    {
        if (index >= this.data.Count)
        {
            index = data.Count;
            this.data.Add(data);

        }
        else
        {
            this.data[index] = data;
            vertices[index].AddRange((IEnumerable<Vector3>)data["vertices"]);
            normals[index].AddRange((IEnumerable<Vector3>)data["normals"]);
            uvs[index].AddRange((IEnumerable<Vector2>)data["uvs"]);
            faces[index].AddRange((IEnumerable<int[]>)data["faces"]);
            indices[index].AddRange((IEnumerable<int>)data["indices"]);
            //  triToFaceIndex[index] = (Godot.Collections.Dictionary<int, int>)data["triface"];
        }

    }


    public List<Vector3> GetNormals(int index)
    {
        if (index >= vertices.Count)
        {
            return null;
        }

        return normals[index];
    }
    public List<Vector2> GetUvs(int index)
    {
        if (index >= uvs.Count)
        {
            return null;
        }

        return uvs[index];
    }
    public List<int[]> GetFaces(int index)
    {
        if (index >= faces.Count)
        {
            return null;
        }

        return faces[index];
    }
    public List<Vector3> GetVertices(int index)
    {
        if (index >= vertices.Count)
        {
            return null;
        }

        return vertices[index];
    }

    // public int GetFaceFromTri(int tri, int index)
    //   {
    //      return triToFaceIndex[index][tri];
    //   }
    public List<int> GetIndices(int index)
    {
        if (index >= indices.Count)
        {
            return null;
        }

        return indices[index];
    }

}
