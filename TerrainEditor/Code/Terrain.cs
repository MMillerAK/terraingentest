using Godot;
using Godot.Collections;
using System;

public partial class Terrain : Node3D
{
    [Export]
    public int MaxHeight = 2;
    [Export]
    public int BrushSize = 50;

    //Determines number of chunks to generate in x and y
    [Export]
    public Vector2I ChunksGenerated;
    //Size of chunks in x y and z
    [Export]
    public Vector3I ChunkVoxelDimensions;


    //Pixel dimensions of the textures in each voxel
    [Export]
    public Vector2I TextureDimensions = new Vector2I(10, 10);

    //number of sections(squared) each voxel has in regards to the texture.  10 texture units means that a voxel face has a 10x10 grid of voxels
    [Export]
    public int textureUnits = 10;
    //size of individual voxels, defaults to 1
    [Export]
    public Vector3 VoxelDimensions = new Vector3(1, 1, 1);


    [Export]
    Node3D ChunksContainer;

    [Export]
    public StandardMaterial3D baseMat;
    [Export]
    RaycastSummary raycastSummary;



    TerrainRayCast3d ray;
    Dictionary<Vector2I, Chunk> Chunks = new();

    bool escaped = false;
    int terrainMode = 0;

    public override void _Ready()
    {
        base._Ready();
        ray = (TerrainRayCast3d)GetNode("%Ray");
        raycastSummary.ConnectRay(ray);
        generateFlat();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                if (ray.IsColliding())
                {
                    Chunk chunk = (Chunk)((Node3D)(ray.GetCollider())).GetParent();
                    Dictionary summary = ray.Summary;
                    if (summary.Count != 0)
                    {
                        Vector3 voxel = (Vector3)summary["voxel"];
                        if (terrainMode == 0)
                            chunk.Wall((Vector3I)voxel);
                        if (terrainMode == 1)
                            chunk.BasicRamp((Vector3I)voxel);

                        if (terrainMode == 2)
                        {
                            chunk.Paint((Vector3)summary["point"], (Vector3)summary["normal"], Colors.Red);
                        }
                    }
                }
            }
            if (eventMouseButton.ButtonIndex == MouseButton.Right)
            {

                if (ray.IsColliding())
                {
                    Chunk chunk = (Chunk)((Node3D)(ray.GetCollider())).GetParent();
                    Dictionary summary = ray.Summary;
                    if (summary.Count != 0)
                    {
                        Vector3I voxel = (Vector3I)summary["voxel"];

                        if (terrainMode == 2)
                        {
                            chunk.Paint((Vector3)summary["point"], (Vector3)summary["normal"], Colors.White);
                        }
                        else
                        {
                            chunk.Erase(voxel.X, voxel.Y, voxel.Z);
                        }


                    }
                }
            }
        }


        if (@event is InputEventKey keypress && keypress.Pressed)
        {
            if (keypress.KeyLabel == Key.Key1)
            {
                terrainMode = 0;
                Console.Write("Wall");
            }
            if (keypress.KeyLabel == Key.Key2)
            {
                terrainMode = 1;
                Console.Write("1xSlope");
            }
            if (keypress.KeyLabel == Key.Key3)
            {
                terrainMode = 2;
                Console.Write("Paint");
            }
            if (keypress.KeyLabel == Key.Tab)
            {
                escaped = !escaped;
                Console.Write("mouse mode toggled");
            }
            if (keypress.KeyLabel == Key.R)
                if (Input.IsKeyPressed(Key.Shift))
                {
                    Chunk.Rotate(false);
                    Console.Write("rotate Backwards");
                }
                else
                {
                    Chunk.Rotate(true);
                    Console.Write("rotate");

                }
        }

    }




    int last = -2;





    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!escaped)
            Input.MouseMode = Input.MouseModeEnum.Captured;



    }
    public void generateFlat()
    {

        foreach (var node in ChunksContainer.GetChildren())
        {
            node.QueueFree();
        }

        for (int x = 0; x < ChunksGenerated.X; x++)
        {
            for (int z = 0; z < ChunksGenerated.Y; z++)
            {

                //origin
                float oX = ChunkVoxelDimensions.X * VoxelDimensions.X * x;
                float oZ = ChunkVoxelDimensions.Y * VoxelDimensions.Z * z;
                float oY = 0;

                Vector3 origin = new Vector3(oX, oY, oZ);


                Chunk chunk = new Chunk(this, new Vector2(x, z));
                ChunksContainer.AddChild(chunk);
                // chunk.Initialize();
                Chunks[new Vector2I(x, z)] = chunk;



            }
        }




    }


    public Chunk coordinateToChunk(Vector3 coordinate)
    {
        Vector3I vector3ICoordinate = (Vector3I)coordinate;

        int X = Mathf.FloorToInt(vector3ICoordinate.X / VoxelDimensions.X);
        int Z = Mathf.FloorToInt(vector3ICoordinate.Z / VoxelDimensions.Z);

        Chunk result = Chunks[new Vector2I(X, Z)];

        return result;

    }

    public Vector3 ChunkOrigin(Vector2I chunkIndex)
    {


        float X = chunkIndex.X * ChunkVoxelDimensions.X * VoxelDimensions.X;
        float y = 0;
        float Z = chunkIndex.Y * ChunkVoxelDimensions.Z * VoxelDimensions.Z;

        return new Vector3(X, y, Z);
    }








}
