using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;


public partial class Chunk : MeshInstance3D
{

    MeshData Top;
    MeshData Sides;


    List<List<List<int>>> voxels = new();

    public int TextureUnits { get => world.textureUnits; }
    public Vector3I Dimensions { get => world.ChunkVoxelDimensions; }
    public Vector3 Resolution { get => world.VoxelDimensions; }
    public Vector2I TextureDimensions { get => textureDimensions; set => textureDimensions = value; }
    private Vector2I textureDimensions;
    [Signal]
    public delegate void MouseChunkUpdateEventHandler(int faceID);

    MeshData meshData = new();

    Vector2 chunkCoordinates;
    Terrain world;
    SurfaceTool surfaceTool;


    bool transparent;

    Godot.Collections.Dictionary<int, int> triToFaceIndex = new();

    List<int[]> faces = new();
    List<Vector3> vertices = new();

    List<Vector3> normals = new();
    List<int> indices = new();
    List<Vector2> uvs = new();

    MeshDataTool[] globalMeshTool = new MeshDataTool[2];
    StandardMaterial3D material;

    public List<StaticBody3D> colliders;


    //default rotation for new voxels
    static Orientation constructOrientation = Orientation.east;
    System.Collections.Generic.Dictionary<(int, int, int), Dictionary> voxelMeta = new();


    public Dictionary summary = new();



    public int GetVertCount()
    {
        return vertices.Count;
    }
    public static void Rotate(bool clockwise)
    {
        switch (constructOrientation)
        {

            case Orientation.east:
                if (clockwise)
                {
                    constructOrientation = Orientation.south;
                }
                else
                {
                    constructOrientation = Orientation.north;
                }
                break;
            case Orientation.west:
                if (clockwise)
                {
                    constructOrientation = Orientation.north;
                }
                else
                {
                    constructOrientation = Orientation.south;
                }
                break;
            case Orientation.north:
                if (clockwise)
                {
                    constructOrientation = Orientation.east;
                }
                else
                {
                    constructOrientation = Orientation.west;
                }
                break;
            case Orientation.south:
                if (clockwise)
                {
                    constructOrientation = Orientation.west;
                }
                else
                {
                    constructOrientation = Orientation.east;
                }
                break;
            case Orientation.down:
            case Orientation.up:
            default:
                break;



        }
    }


    Dictionary GetIntersectionPoint(Vector3 from, Vector3 direction, Vector3[] triangle, Dictionary results, int surfaceindex = 0)
    {
        //boilerplate
        Dictionary output = new();
        if (results != null)
            output = results;
        var meshDataTool = new MeshDataTool();
        meshDataTool.CreateFromSurface((ArrayMesh)Mesh, surfaceindex);

        for (int i = 0; i < meshDataTool.GetVertexCount() - 2; i += 3)
        {
            Vector3 vertex = meshDataTool.GetVertex(i);
            int face_index = i / 3;
            var a = ToGlobal(meshDataTool.GetVertex(i));
            var b = ToGlobal(meshDataTool.GetVertex(i + 1));
            var c = ToGlobal(meshDataTool.GetVertex(i + 2));

            Vector3 direction = from.DirectionTo(end);

            Vector3? inttri = (Vector3)Geometry3D.RayIntersectsTriangle(from, direction, a, b, c);
            if (inttri != Vector3.Zero)
            {

                ImmediateMesh immediateMesh = new ImmediateMesh();
                MeshInstance3D line = new();

                float angle = Mathf.RadToDeg(direction.AngleTo(meshDataTool.GetFaceNormal(face_index)));
                if (angle >= 90 || angle <= 180)
                {

                    results["angle"] = angle;
                    results["face_index"] = face_index;

                }
            }
        }
        return output;
    }

    public Dictionary CastRay()
    {
        const int RAY_LENGTH = 10000000;
        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        Camera3D camera = GetViewport().GetCamera3D();
        Vector2 mousepos = GetViewport().GetMousePosition();


        Vector3 from = camera.ProjectRayOrigin(mousepos);
        Vector3 end = from + camera.ProjectRayNormal(mousepos) * RAY_LENGTH;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, end);

        query.CollideWithBodies = true; query.HitFromInside = true;

        Dictionary results = spaceState.IntersectRay(query);
        for (int s = 0; s < Mesh.GetSurfaceCount(); s++)
        {
            var meshDataTool = new MeshDataTool();
            meshDataTool.CreateFromSurface((ArrayMesh)Mesh, s);

            for (int i = 0; i < meshDataTool.GetVertexCount() - 2; i += 3)
            {
                Vector3 vertex = meshDataTool.GetVertex(i);
                int face_index = i / 3;
                var a = ToGlobal(meshDataTool.GetVertex(i));
                var b = ToGlobal(meshDataTool.GetVertex(i + 1));
                var c = ToGlobal(meshDataTool.GetVertex(i + 2));

                Vector3 direction = from.DirectionTo(end);

                Vector3? inttri = (Vector3)Geometry3D.RayIntersectsTriangle(from, direction, a, b, c);
                if (inttri != Vector3.Zero)
                {

                    ImmediateMesh immediateMesh = new ImmediateMesh();
                    MeshInstance3D line = new();

                    float angle = Mathf.RadToDeg(direction.AngleTo(meshDataTool.GetFaceNormal(face_index)));
                    if (angle >= 90 || angle <= 180)
                    {

                        results["angle"] = angle;
                        results["face_index"] = face_index;

                    }
                }
            }
        }

        //EmitSignal(SignalName.TerrainRayCastResults, results);
        return results;
    }


    public Vector3 OriginOffset(Vector3 voxel)
    {
        //this is how big each chunk is
        Vector3 off = Resolution * Dimensions;

        //chunk coordinates are a vector 2d, since chunks are not measured in the y dimension
        return new Vector3(off.X * chunkCoordinates.X, 0, off.Z * chunkCoordinates.Y);
    }
    public Chunk(Terrain world, Vector2 chunkCoordinates)
    {

        this.world = world;

        this.chunkCoordinates = chunkCoordinates;
        voxels = new List<List<List<int>>>();
        TextureDimensions = world.TextureDimensions;

        Image image = Image.CreateEmpty(TextureDimensions.X, TextureDimensions.Y * world.MaxHeight, false, Image.Format.Rgb8);
        for (int i = 0; i < TextureDimensions.X; i++)
            for (int j = 0; j < TextureDimensions.Y * world.MaxHeight; j++)
            {
                image.SetPixel(i, j, Colors.White);
            }

        TextureDimensions = new Vector2I(TextureDimensions.X, TextureDimensions.Y * world.MaxHeight);
        ImageTexture tex = new ImageTexture();
        tex.SetImage(image);
        material = (StandardMaterial3D)world.baseMat.Duplicate(true);
        material.AlbedoTexture = tex;
    }

    public override void _Ready()
    {
        base._Ready();
        surfaceTool = new();




        float x = chunkCoordinates.X * Dimensions.X * Resolution.X;
        float y = chunkCoordinates.Y * Dimensions.Z * Resolution.Z;

        GlobalPosition = new Vector3(x, 0, y);





        MakeFlat();
    }


    public void MakeFlat()
    {
        for (int x = 0; x < Dimensions.X; x++)
        {
            voxels.Add(new List<List<int>>());
            for (int y = 0; y < world.MaxHeight; y++)
            {
                voxels[x].Add(new List<int>());
                for (int z = 0; z < Dimensions.Z; z++)
                {

                    if (y == 0)
                    {
                        voxels[x][y].Add(0);
                    }
                    else
                    {
                        voxels[x][y].Add(-1);
                    }
                }
            }
        }

        CreateMesh();

    }


    private void InterpretGeometry(MeshData md)
    {
        if (!md.success)
        {
            return;
        }



        //   Vector3[] newnormals = (Vector3[])dictionary["normals"];
        //    Vector2[] newUV = (Vector2[])dictionary["uvs"];


        //     List<int[]> newfaces = (List<int[]>)dictionary["faces"];
        //     Vector3[] newvert = (Vector3[])dictionary["vertices"];
        //     int[] newindex = (int[])dictionary["indices"];


        //     normals.AddRange(newnormals);
        //     uvs.AddRange(newUV);
        //     faces.AddRange(newfaces);
        //     vertices.AddRange(newvert);
        //     indices.AddRange(newindex);



    }
    private void CreateGeometry(int vx, int vy, int vz)
    {

        int type = voxels[vx][vy][vz];
        if (type == -1)
        {
            return;
        }

        if (type == 0)
        {
            Cube cube = new(meshData);
            meshData.Append(cube.Begin(vx, vy, vz, voxels, world.MaxHeight, this));
            //    InterpretGeometry(cube.Begin(vx, vy, vz, voxels, world.MaxHeight, this));
            //MakeCube(vx, vy, vz);
        }
        if (type == 1)
        {
            Orientation orientation = Orientation.east;
            int ratio = 1;
            if (voxelMeta.ContainsKey((vx, vy, vz)))
            {
                Dictionary meta = voxelMeta[(vx, vy, vz)];
                if (meta.ContainsKey("orientation"))
                    orientation = (Orientation)(int)meta["orientation"];
                if (meta.ContainsKey("slope"))
                    ratio = (int)meta["slope"];

                MakeRamp(vx, vy, vz, ratio, orientation);
            }


        }


    }

    public void Erase(int vx, int vy, int vz)
    {
        vx = vx % Dimensions.X;
        vz = vz % Dimensions.Z;
        voxels[vx][vy][vz] = -1;
        CreateMesh();

    }

    public void CreateMesh()
    {
        meshData = new();
        //generate the vertices and vertex data
        for (int x = 0; x < Dimensions.X; x++)
        {
            for (int y = 0; y < world.MaxHeight; y++)
            {
                for (int z = 0; z < Dimensions.Z; z++)
                {
                    int type = voxels[x][y][z];
                    CreateGeometry(x, y, z);

                }
            }
        }

        //delete old meshes
        Node[] todelete = GetChildren().ToArray();
        for (int i = 0; i < todelete.Count(); i++)
        {
            todelete[i].QueueFree();
        }

        Godot.Collections.Array[] arr = meshData.construct();

        ArrayMesh arrayMesh = new ArrayMesh();
        Mesh = arrayMesh;
        for (int i = 0; i < arr.Length; i++)
        {
            ((ArrayMesh)Mesh).AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr[i]);
        }

        var trimesh = Mesh.CreateTrimeshShape();
        this.colliders = new();

        StaticBody3D staticBody3D = new();
        AddChild(staticBody3D);
        CollisionShape3D collisionShape = new();
        staticBody3D.AddChild(collisionShape);
        ConcavePolygonShape3D shapePoly = new();
        collisionShape.Shape = trimesh;

        staticBody3D.Owner = this;
        collisionShape.Owner = this;
        var ind = Mesh.GetFaces();

        colliders.Add(staticBody3D);








        int vertCount = 0;
        int edgeCount = 0;

        int triCount = 0;


        MeshDataTool mdt = new();
        mdt.CreateFromSurface((ArrayMesh)Mesh, 0);

        vertCount += mdt.GetVertexCount();
        edgeCount += mdt.GetEdgeCount();
        triCount += mdt.GetFaceCount();








        summary["vertices"] = vertCount;
        summary["edges"] = edgeCount;
        summary["triangles"] = triCount;


        globalMeshTool = new MeshDataTool[Mesh.GetSurfaceCount()];
        for (int i = 0; i < globalMeshTool.Length; i++)
        {
            globalMeshTool[i] = new();
            globalMeshTool[i].CreateFromSurface((ArrayMesh)Mesh, i);

        }
        SetSurfaceOverrideMaterial(0, material);
        SetSurfaceOverrideMaterial(1, material);

    }



    public void UpdateMesh()
    {
        //   CreateMesh();
        SurfaceTool surfaceTool = new();
        surfaceTool.CreateFrom((ArrayMesh)Mesh, 0);
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        int currentVertexCount = Mesh.GetFaces().Count(); ;
        int newCount = vertices.Count;

        for (int i = currentVertexCount; i < newCount; i++)
        {
            surfaceTool.SetNormal(normals[i]);
            surfaceTool.SetUV(uvs[i]);
            //surfaceTool.SetColor(colors[i]);
            surfaceTool.AddVertex(vertices[i]);
        }

        int currentindices = ((Godot.Collections.Array)Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Index]).Count;
        newCount = this.indices.Count();
        surfaceTool.Index();

        surfaceTool.Commit((ArrayMesh)Mesh);
        PackedScene packedScene = new PackedScene();
        packedScene.Pack(this);

        //   var err = ResourceSaver.Save(packedScene, "out.tscn");
        //   Console.Write(err);

        summary = new();

        MeshDataTool mdt = new();
        mdt.CreateFromSurface((ArrayMesh)Mesh, 0);


        summary["vertices"] = Mesh.GetFaces();
        summary["edges"] = mdt.GetEdgeCount();
        summary["triangles"] = mdt.GetFaceCount();
        //summary["faces"] = faces.Count;

        //  globalMeshTool = new();
        //  globalMeshTool.CreateFromSurface((ArrayMesh)Mesh, 0);


    }

    //returns the normalized coordinates of point P relative to the triangle formed by points
    // A, B, and C
    /// <summary>
    ///returns the normalized coordinates of point P relative to the triangle formed by points
    /// A, B, and C
    /// </summary>
    /// <param name="P">point that is presumably within the triangle, other functions can confirm this</param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <param name="C"></param>
    private Vector3 GetBarycentricCoordinate(Vector3 P, Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 v0 = B - A;
        Vector3 v1 = C - A;
        Vector3 v2 = P - A;

        float d00 = v0.Dot(v0);
        float d01 = v0.Dot(v1);
        float d11 = v1.Dot(v1);
        float d20 = v2.Dot(v0);
        float d21 = v2.Dot(v1);

        float denominator = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denominator;
        float w = (d00 * d21 - d01 * d20) / denominator;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);




    }

    private bool IsPointInTriangle(Vector3 P, Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 barycentric = GetBarycentricCoordinate(P, A, B, C);

        if (barycentric.X < 0 || barycentric.X > 1)
            return false;
        if (barycentric.Y < 0 || barycentric.Y > 1)
        {
            return false;
        }
        if (barycentric.Z < 0 || barycentric.Z > 1)
        {
            return false;
        }


        return true;
    }

    private bool equalsWithEpsilon(Vector3 a, Vector3 b, float epsilon = 0.02f)
    {
        return a.DistanceTo(b) < epsilon;

    }

    public (int, int) GetFace(Vector3 point, Vector3 normal, float epsilon = 0.002f)
    {
        for (int f = 0; f < globalMeshTool.Length; f++)
        {
            int faceCount = globalMeshTool[f].GetFaceCount();
            for (int i = 0; i < faceCount; i++)
            {
                if (!equalsWithEpsilon(globalMeshTool[f].GetFaceNormal(i), normal, epsilon))
                    continue;

                Vector3 A = globalMeshTool[f].GetVertex(globalMeshTool[f].GetFaceVertex(i, 0));
                Vector3 B = globalMeshTool[f].GetVertex(globalMeshTool[f].GetFaceVertex(i, 1));
                Vector3 C = globalMeshTool[f].GetVertex(globalMeshTool[f].GetFaceVertex(i, 2));

                if (IsPointInTriangle(point, A, B, C))
                {

                    return (i, f);
                }

            }
        }

        return (-1, -1);
    }

    Vector2 getUVCoordinates(Vector3 point, Vector3 normal)
    {
        (int face, int surface) f = GetFace(point, normal);
        if (f.face == -1)
        {
            return Vector2.Inf;
        }
        int face = f.face;
        int surface = f.surface;
        Vector3 A = globalMeshTool[surface].GetVertex(globalMeshTool[surface].GetFaceVertex(face, 0));
        Vector3 B = globalMeshTool[surface].GetVertex(globalMeshTool[surface].GetFaceVertex(face, 1));
        Vector3 C = globalMeshTool[surface].GetVertex(globalMeshTool[surface].GetFaceVertex(face, 2));

        A = GlobalTransform * A;
        B = GlobalTransform * B;
        C = GlobalTransform * C;
        Vector3 barycentricCoords = GetBarycentricCoordinate(point, A, B, C);

        Vector2 uv1 = globalMeshTool[surface].GetVertexUV(globalMeshTool[surface].GetFaceVertex(face, 0));
        Vector2 uv2 = globalMeshTool[surface].GetVertexUV(globalMeshTool[surface].GetFaceVertex(face, 1));
        Vector2 uv3 = globalMeshTool[surface].GetVertexUV(globalMeshTool[surface].GetFaceVertex(face, 2));

        //not really x, y and z, but its fine
        Vector2 x = uv1 * barycentricCoords.X;
        Vector2 y = uv2 * barycentricCoords.Y;
        Vector2 z = uv3 * barycentricCoords.Z;

        return x + y + z;

    }

    public void Paint(Vector3 point, Vector3 normal, Godot.Color paintColor)
    {

        Vector3 pointtile = point.PosMod(new Vector3(Dimensions.X * Resolution.X, point.Y + 1, Dimensions.Z * Resolution.Z));


        int BrushSize = world.BrushSize;
        Console.WriteLine($"clicked: {point} tile: {pointtile}");

        Vector2 UV = getUVCoordinates(pointtile, normal);
        if (UV == Vector2.Inf)
            return;
        if (UV.X == float.NaN || UV.Y == float.NaN)
        {
            return;
        }





        Texture2D texture = (Texture2D)material.AlbedoTexture;

        Image textureImage = (Image)texture.GetImage().Duplicate();

        Vector2I imageSize = textureImage.GetSize();

        Vector2I coord = new Vector2I((int)(UV.X * imageSize.X), (int)(UV.Y * imageSize.Y));



        coord = new Vector2I(Mathf.PosMod(coord.X, imageSize.X), Mathf.PosMod(coord.Y, imageSize.Y));
        Console.WriteLine($"uv: {coord} TILE: {coord % imageSize}");
        Console.WriteLine($"coord: {coord}");
        Console.WriteLine($"Dim: {Dimensions}");
        textureImage.SetPixelv(coord, paintColor);


        ((ImageTexture)texture).SetImage(textureImage);
        material.AlbedoTexture = texture;
        SetSurfaceOverrideMaterial(0, material);

        CreateMesh();
    }

    public int getFaceFromIndex(int triFaceID)
    {
        if (triToFaceIndex.Count > 0)
            return triToFaceIndex[triFaceID];

        return 0;
    }

    public Vector3I GetVoxelPositionAtPosition(Vector3 position)
    {


        Vector3 newpos = GlobalTransform * position;
        float x = position.X / Resolution.X;
        float y = position.Y / Resolution.Y;
        float z = position.Z / Resolution.Z;
        return (Vector3I)new Vector3(x, y, z);




    }
    public int GetVoxelAtPosition(Vector3 position)
    {
        if (IsPositionInChunk(position))
        {

            position = position * GlobalTransform;

            position.X = Mathf.Floor(position.X / Resolution.X);
            position.Y = Mathf.Floor(position.Y / Resolution.Y);
            position.Z = Mathf.Floor(position.Z / Resolution.Z);

            return voxels[(int)position.X][(int)position.Y][(int)position.Z];
        }

        return -1;
    }
    public bool IsVoxelInChunk(float x, float y, float z)
    {


        if (x < 0 || z < 0 || y < 0)
        {
            return false;
        }
        bool xout = x >= Dimensions.X;
        //bool yout = y >= dimensions.Y;
        bool zout = z >= Dimensions.Z;

        if (xout || zout)
        {
            return false;
        }
        return true;
    }

    public bool IsPositionInChunk(Vector3 position)
    {
        bool X = position.X < GlobalPosition.X + Dimensions.X * Resolution.X && position.X > GlobalPosition.X;
        //bool Y = position.Y < GlobalPosition.Y + dimensions.X * Resolution.Y && position.Y > GlobalPosition.Y;
        bool Z = position.Z < GlobalPosition.Z + Dimensions.X * Resolution.Z && position.Z > GlobalPosition.Z;
        if (X && Z)
            return true;

        return false;
    }

    public bool DoesVoxelAllowRender(int x, int y, int z)
    {
        if (!IsVoxelInChunk(x, y, z))
            return true;
        if (y >= voxels[0].Count())
        {
            return true;
        }
        if (voxels[x][y][z] == -1)
            return true;


        return false;

    }

    public enum Orientation
    {
        up,
        down,
        east,
        west,
        north,
        south
    }

    public Vector3 VoxelCoordinates(Vector3 voxel)
    {
        Vector3 results = voxel * Resolution;
        results = results + new Vector3(chunkCoordinates.X, 0, chunkCoordinates.Y);
        return results;
    }
    public bool AddLayer()
    {
        if (Dimensions.Y >= world.MaxHeight)
            return false;

        int y = Dimensions.Y;
        for (int x = 0; x < Dimensions.X; x++)
        {



            voxels[x].Add(new());
            for (int z = 0; z < Dimensions.Z; z++)
            {

                voxels[x][y].Add(-1);

            }


        }

        //  Dimensions = Dimensions + Vector3I.Up;


        return true;
    }

    public void Wall(Vector3I voxel)
    {
        // Vector3 offset = OriginOffset(voxel);
        // offset = Vector3.Zero;
        if (voxel.X < 0 || voxel.Y < 0 || voxel.Z < 0)
        {
            return;
        }
        if (voxel.Y >= world.MaxHeight)
        {
            return;
        }

        int x = voxel.X % (Dimensions.X);
        int y = voxel.Y;
        int z = voxel.Z % (Dimensions.Z);
        Vector3I res = new Vector3I(x, y, z);
        Vector3I highest = res;


        bool foundHigher = true;

        //we burrow up, looking for either air, or not a block
        //  highest = highest + Vector3I.Up;
        while (foundHigher)
        {

            Vector3I next = new Vector3I(res.X, highest.Y + Vector3I.Up.Y, res.Z);
            if (next.Y >= world.MaxHeight)
            {

                return;
            }
            int n = voxels[next.X][next.Y][next.Z];


            if (!VoxelExists(highest))
            {
                foundHigher = false;
            }
            else if (voxels[next.X][next.Y][next.Z] == -1)
            {
                highest = next;
                foundHigher = false;
            }
            else
            {
                highest = highest + Vector3I.Up;
            }
        }

        //if the found block is not air, it is undefined, and we need to add a layer

        // MakeCube(res.X, highest.Y, res.Z);

        //  voxels[res.X][highest.Y][res.Z] = 0;
        //  Cube cube = new();
        //   cube.Begin(res.X, highest.Y, res.Z, voxels, world.MaxHeight, this);
        voxels[res.X][highest.Y][res.Z] = 0;
        //        MakeCube(voxel.X, highest.Y, voxel.Z);

        CreateMesh();



    }

    public void BasicRamp(Vector3I voxel, int length = 1)
    {
        if (voxel.X < 0 || voxel.Y < 0 || voxel.Z < 0)
        {
            return;
        }

        int x = voxel.X % (Dimensions.X);
        int y = voxel.Y + 1;
        int z = voxel.Z % (Dimensions.Z);
        Vector3I res = new Vector3I(x, y, z);
        Vector3I highest = res;


        while (y >= voxels[0].Count)
        {

            AddLayer();
        }




        //if the found block is not air, it is undefined, and we need to add a layer

        //MakeCube(res.X, highest.Y, res.Z);
        Dictionary meta = new();
        meta["orientation"] = (int)constructOrientation;
        meta["slope"] = length;
        voxelMeta[(res.X, highest.Y, res.Z)] = meta;
        voxels[res.X][highest.Y][res.Z] = 1;

        UpdateMesh();
    }
    bool VoxelIsAir(Vector3I voxel)
    {
        if (!VoxelExists(voxel))
            return false;
        if (voxels[voxel.X][voxel.Y][voxel.Z] == -1)
        {
            return true; ;
        }

        return false;
    }
    public bool VoxelExists(Vector3I voxel)
    {
        if (voxel.X >= voxels.Count)
            return false;
        if (voxel.X >= voxels[0].Count)
        {
            return false;
        }
        if (voxel.Z >= voxels[0][0].Count)
        {
            return false;
        }


        return true;
    }

    public int GetVoxel(Vector3I voxel)
    {
        if (VoxelExists(voxel))
            return voxels[voxel.X][voxel.Y][voxel.Z];
        else
            return -1;
    }
    /// <summary>
    /// Adds a ramp to the geometry
    /// </summary>
    /// <param name="vx">voxel coordinate</param>
    /// <param name="vy">voxel coordinate</param>
    /// <param name="vz">voxel coordinates</param>   /// 
    /// <param name="slopeRatio">this determines how steep the slope of the ramp is. when positive, the slope ratio is the number of voxels in a row to go up 1 block, if the slope ratio is negative, the slope ratio is how high
    // the block would need to be stacked for the ramp to go forward 1 block</param>
    /// <param name="slopeFacing">this is what direction the face of the ramp is facing(north south,east,west are valid, up and down will skip the creation of the ramp</param>
    /// <param name="inverse">this will turn the ramp upside down, allowing for overhangs</param>
    /// <param name="step">for multiblock ramps, this is how high up the base of the ramp starts</param>
    /// <param name="thickness">for multiblock ramps, determines how thick underneath the ramp is. if  slopeheight is equaL TO 0(the voxel is directly on top of the voxel below) this does nothing</param>
    private void MakeRamp(int vx, int vy, int vz, int slopeRatio, Orientation slopeFacing, bool inverse = false, int step = 0, int thickness = 0)
    {
        float rampHeight = (step + 1) / slopeRatio;
        if (slopeFacing == Orientation.up || slopeFacing == Orientation.down)
        {
            return;
        }




        //for testing, we render all faces, add allow render later
        MakeRampSlope(vx, vy, vz, rampHeight, 0, slopeFacing);
        MakeRampBottom(vx, vy, vz, rampHeight, 0, slopeFacing);
        MakeRampBack(vx, vy, vz, rampHeight, 0, slopeFacing);
        if (slopeFacing == Orientation.east)
        {
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.north, true);
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.south, true);

        }
        if (slopeFacing == Orientation.west)
        {
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.north, false);
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.south, false);

        }
        if (slopeFacing == Orientation.north)
        {
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.east, false);
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.west, false);

        }
        if (slopeFacing == Orientation.south)
        {
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.east, true);
            MakeRampSide(vx, vy, vz, rampHeight, 0, Orientation.west, true);
        }




    }

    private void MakeRampBack(int vx, int vy, int vz, float height, float baseh, Orientation slopeFacing)
    {
        float x = vx * Resolution.X;
        float y = vy * Resolution.Y;
        float z = vz * Resolution.Z;
        Vector3[] v = new Vector3[0];
        bool inverse = false;
        switch (slopeFacing)
        {
            case Orientation.east:
                v = [
                new Vector3(x+Resolution.X, y, z + Resolution.Z),
            new Vector3(x+Resolution.X, y, z),
            new Vector3(x+Resolution.X, y + Resolution.Y, z),
            new Vector3(x+Resolution.X, y + Resolution.Y, z + Resolution.Z)
                ];

                normals.Add(new Vector3(-1, 0, 0));
                normals.Add(new Vector3(-1, 0, 0));
                normals.Add(new Vector3(-1, 0, 0));
                normals.Add(new Vector3(-1, 0, 0));

                inverse = true;
                break;

            case Orientation.west:
                v = [
                new Vector3(x, y, z + Resolution.Z),
                new Vector3(x, y, z),
                new Vector3(x, y + Resolution.Y, z),
                new Vector3(x, y + Resolution.Y, z + Resolution.Z)


                ];
                normals.Add(new Vector3(1, 0, 0));
                normals.Add(new Vector3(1, 0, 0));
                normals.Add(new Vector3(1, 0, 0));
                normals.Add(new Vector3(1, 0, 0));
                break;



            case Orientation.south:
                v = [
                    new Vector3(    x+Resolution.X,     y,         z+Resolution.Z),
                     new Vector3(    x,     y ,                     z+Resolution.Z),
                     new Vector3(    x,                  y+Resolution.Y,                      z+Resolution.Z),
                    new Vector3(    x+Resolution.X,                  y+Resolution.Y,         z+Resolution.Z)



                ];

                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));

                break;

            //south face for north ramp
            case Orientation.north:
                v = [
                new Vector3(x+ Resolution.X, y+ Resolution.Y, z ),
                 new Vector3(x, y+ Resolution.Y, z),
                 new Vector3(x, y , z),
                 new Vector3(x+ Resolution.X, y , z )
                ];
                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));
                normals.Add(new Vector3(0, 0, 1));
                break;
            default:
                break;
        }



        vertices.Add(v[0]);
        vertices.Add(v[1]);
        vertices.Add(v[2]);
        vertices.Add(v[3]);


        Vector2I size = TextureDimensions;
        uvs.Add(new Vector2(0, size.Y));
        uvs.Add(new Vector2(size.X, size.Y));
        uvs.Add(new Vector2(size.X, 0));
        uvs.Add(new Vector2(0, 0));

        int index = vertices.Count;
        if (!inverse)
        {
            indices.Add(index - 4);
            indices.Add(index - 3);
            indices.Add(index - 1);
            indices.Add(index - 3);
            indices.Add(index - 2);
            indices.Add(index - 1);
        }
        else
        {
            indices.Add(index - 1);
            indices.Add(index - 2);
            indices.Add(index - 3);
            indices.Add(index - 1);
            indices.Add(index - 3);
            indices.Add(index - 4);


        }

        int faceindex = indices.Count - 6;
        int[] face = { faceindex, faceindex + 3 };


        triToFaceIndex[triToFaceIndex.Count] = faces.Count;
        triToFaceIndex[triToFaceIndex.Count] = faces.Count;
        faces.Add(face);

    }
    private void MakeRampSlope(int vx, int vy, int vz, float height, float baseh, Orientation slopeFacing)
    {
        bool inverse = false;
        if (vx < 0 || vy < 0 || vz < 0)
            return;
        if (vx > voxels.Count || vy > voxels[0].Count || vz > voxels[0][0].Count)
        {
            if (voxels[vx][vy][vz] == -1)
            {
                return;
            }
        }
        Vector3[] arr = [];
        float x = vx * Resolution.X;
        float y = vy * Resolution.Y;
        float z = vz * Resolution.Z;

        switch (slopeFacing)
        {
            case Orientation.east:

                arr = [


                    new Vector3(x, y , z), //010  a
                        new Vector3(x + Resolution.X, y+ height, z), //10 b
                        new Vector3(x + Resolution.X, y+ height, z + Resolution.Z),//11 d
                        new Vector3(x, y , z + Resolution.Z)//01 //c

               ];



                vertices.AddRange(arr);
                break;

            case Orientation.west:

                //  inverse = true;
                arr = [


                   new Vector3(x, y + height, z), //010  a
                        new Vector3(x + Resolution.X, y, z), //10 b
                        new Vector3(x + Resolution.X, y , z + Resolution.Z),//11 d
                        new Vector3(x, y + height, z + Resolution.Z)//01 //c

                ];

                vertices.AddRange(arr);
                break;

            case Orientation.north:

                //  inverse = true;
                arr = [


                       new Vector3(x, y + height, z), //010  a
                        new Vector3(x + Resolution.X, y + height, z), //10 b
                        new Vector3(x + Resolution.X, y , z + Resolution.Z),//11 d
                        new Vector3(x, y, z + Resolution.Z)//01 //c

                ];

                vertices.AddRange(arr);
                break;


            case Orientation.south:

                //  inverse = true;
                arr = [


                      new Vector3(x, y, z), //010  a
                        new Vector3(x + Resolution.X, y , z), //10 b
                        new Vector3(x + Resolution.X, y+ height , z + Resolution.Z),//11 d
                        new Vector3(x, y+ height, z + Resolution.Z)//01 //c

                ];

                vertices.AddRange(arr);
                break;
        }
        //calculate normals
        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            float nx = normal.X;
            float ny = normal.Y;
            float nz = normal.Z;
            nx += (vertices[i].Y - vertices[j].Y)
                     * vertices[i].Z + vertices[j].Z;

            ny += (vertices[i].Z - vertices[j].Z)
             * vertices[i].X + vertices[j].X;

            nx += (vertices[i].X - vertices[j].X)
            * vertices[i].Y + vertices[j].Y;

            normal = new Vector3(nx, ny, nz);
        }

        normal = normal.Normalized();
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        Vector2I size = TextureDimensions;
        uvs.Add(new Vector2(0, size.Y));
        uvs.Add(new Vector2(size.X, size.Y));
        uvs.Add(new Vector2(size.X, 0));
        uvs.Add(new Vector2(0, 0));


        int index = vertices.Count;

        indices.Add(index - 4);
        indices.Add(index - 3);
        indices.Add(index - 1);
        indices.Add(index - 3);
        indices.Add(index - 2);
        indices.Add(index - 1);









        int faceindex = indices.Count - 6;
        int[] face = { faceindex, faceindex + 3 };


        triToFaceIndex[triToFaceIndex.Keys.Count] = faces.Count;
        triToFaceIndex[triToFaceIndex.Keys.Count] = faces.Count;
        faces.Add(face);

    }

    private void MakeRampSide(int vx, int vy, int vz, float height, float baseh, Orientation sidefacing, bool inverse)
    {
        if (vx < 0 || vy < 0 || vz < 0)
            return;
        if (vx > voxels.Count || vy > voxels[0].Count || vz > voxels[0][0].Count)
        {
            if (voxels[vx][vy][vz] == -1)
            {
                return;
            }
        }
        float x = vx * Resolution.X;
        float y = vy * Resolution.Y;
        float z = vz * Resolution.Z;


        Vector3 p1 = Vector3.Zero;
        Vector3 p2 = Vector3.Zero;
        Vector3 p3 = Vector3.Zero;
        //need a triangle east and west
        //north
        if (!inverse)
            switch (sidefacing)
            {

                case Orientation.south:


                    p1 = new Vector3(x, y + Resolution.Y, z + Resolution.Z);

                    p2 = new Vector3(x + Resolution.X, y, z + Resolution.Z); //10 b
                    p3 = new Vector3(x, y, z + Resolution.Z);//11 d

                    vertices.AddRange([p1, p2, p3]);
                    break;

                case Orientation.north:



                    p1 = new Vector3(x, y, z);

                    p2 = new Vector3(x + Resolution.X, y, z);
                    p3 = new Vector3(x, y + Resolution.Y, z);


                    vertices.AddRange([p1, p2, p3]);
                    break;


                case Orientation.east:



                    p1 = new Vector3(x + Resolution.X, y + Resolution.Y, z);
                    p2 = new Vector3(x + Resolution.X, y, z);
                    p3 = new Vector3(x + Resolution.X, y, z + Resolution.Z);

                    vertices.AddRange([p1, p2, p3]);


                    break;

                case Orientation.west:
                    p1 = new Vector3(x, y, z + Resolution.Z);

                    p2 = new Vector3(x, y, z);
                    p3 = new Vector3(x, y + Resolution.Y, z);

                    vertices.AddRange([p1, p2, p3]);


                    break;


            }
        if (inverse)
            switch (sidefacing)
            {

                case Orientation.north:



                    p3 = new Vector3(x + Resolution.X, y + Resolution.Y, z); //00  a
                    p2 = new Vector3(x, y, z); //10 b
                    p1 = new Vector3(x + Resolution.X, y, z);//11 d
                    vertices.AddRange([p1, p2, p3]);
                    break;

                case Orientation.south:


                    Vector3[] arr =
                    {

                      p1 =new Vector3(x+Resolution.X, y + Resolution.Y, z+Resolution.Z),
                      p2 = new Vector3(x, y, z+Resolution.Z),
                      p3 = new Vector3(x+Resolution.X, y, z+Resolution.Z)
                      };
                    vertices.AddRange([p1, p2, p3]);
                    break;

                case Orientation.east:



                    p1 = new Vector3(x + Resolution.X, y, z);
                    p2 = new Vector3(x + Resolution.X, y + Resolution.Y, z + Resolution.Z);
                    p3 = new Vector3(x + Resolution.X, y, z + Resolution.Z);

                    vertices.AddRange([p1, p2, p3]);


                    break;

                case Orientation.west:
                    p1 = new Vector3(x, y, z + Resolution.Z);

                    p2 = new Vector3(x, y + Resolution.Y, z + Resolution.Z);
                    p3 = new Vector3(x, y, z);

                    vertices.AddRange([p1, p2, p3]);


                    break;



            }

        Vector3 A = p2 - p1;
        Vector3 B = p3 - p1;


        float nx = A.Y * B.Z * A.Z * B.Y;
        float ny = A.Z * B.X - A.X * B.Z;
        float nz = A.X * B.Y - A.Y - B.X;

        Vector3 normal = new Vector3(nx, ny, nz);


        normal = normal.Normalized();

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        Vector2I size = TextureDimensions;
        uvs.Add(new Vector2(0, size.Y));
        uvs.Add(new Vector2(size.X, size.Y));
        uvs.Add(new Vector2(size.X, 0));


        int index = vertices.Count;
        if (!inverse)
        {
            indices.Add(index - 3);
            indices.Add(index - 2);
            indices.Add(index - 1);
        }
        else
        {
            indices.Add(index - 1);
            indices.Add(index - 2);
            indices.Add(index - 3);
            //indices.Add(index - 1);
            //  indices.Add(index - 2);
            // indices.Add(index - 3);
        }

        int faceindex = indices.Count - 3;
        int[] face = { faceindex };


        triToFaceIndex[triToFaceIndex.Keys.Count] = faces.Count;

        faces.Add(face);

    }

    private void MakeRampBottom(int vx, int vy, int vz, float height, float baseh, Orientation slopeFacing)
    {

        float x = vx * Resolution.X;
        float y = vy * Resolution.Y;
        float z = vz * Resolution.Z;

        Vector3[] v =
           {
        new Vector3(vx, vy, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy, vz + Resolution.Z),
        new Vector3(vx + Resolution.X, vy, vz),
        new Vector3(vx, vy , vz)
     };


        vertices.Add(v[0]);
        vertices.Add(v[1]);
        vertices.Add(v[2]);
        vertices.Add(v[3]);

        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));
        normals.Add(new Vector3(0, -1, 0));



        Vector2I size = TextureDimensions;
        uvs.Add(new Vector2(0, size.Y));
        uvs.Add(new Vector2(size.X, size.Y));
        uvs.Add(new Vector2(size.X, 0));
        uvs.Add(new Vector2(0, 0));

        int index = vertices.Count;
        indices.Add(index - 4);
        indices.Add(index - 3);
        indices.Add(index - 1);
        indices.Add(index - 3);
        indices.Add(index - 2);
        indices.Add(index - 1);

        int faceindex = indices.Count - 6;
        int[] face = { faceindex, faceindex + 3 };
        triToFaceIndex[triToFaceIndex.Keys.Count] = faces.Count;
        triToFaceIndex[triToFaceIndex.Keys.Count] = faces.Count;
        faces.Add(face);
    }


    public Chunk() { }


}


