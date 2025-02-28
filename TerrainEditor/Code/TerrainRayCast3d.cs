using Godot;
using Godot.Collections;
using System;
[GlobalClass]
public partial class TerrainRayCast3d : RayCast3D
{
    int lastcast;

    [Signal]
    public delegate void RayHitEventHandler(Dictionary results, Chunk chunk);


    public Dictionary Summary = new();


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsColliding())
        {
            RaycastGetSummary();
        }
    }
    public void RaycastGetSummary()
    {
        Dictionary summary = new();

        bool success = false;
        if (IsColliding())
        {
            success = true;
            int tri = GetCollisionFaceIndex();
            if (tri == lastcast || tri == -1)
            {
                return;
            }
            Node node = (Node3D)GetCollider();
            if (node == null)
            {
                success = false;

            }
            else
            {
                node = ((Node)GetCollider()).GetParent();
                if (node is Chunk chunk)
                {
                    chunk.CastRay();
                    summary = chunk.summary;
                    int vert = chunk.getFaceFromIndex(tri / 3);
                    int face = vert / 3;
                    summary["vertices"] = vert;
                    summary["face"] = face;

                    summary["point"] = GetCollisionPoint();
                    summary["highlighted"] = face;
                    summary["triangleHighlight"] = tri;
                    Vector3 collisionpoint = GetCollisionPoint();
                    Vector3 normal = GetCollisionNormal();
                    summary["normal"] = normal;
                    Vector3I facing = (Vector3I)GetViewport().GetCamera3D().Position.DirectionTo(collisionpoint);
                    summary["voxel"] = chunk.GetVoxelPositionAtPosition(GetCollisionPoint() - normal / 2);
                    if ((Vector3)summary["voxel"] == new Vector3(-1, -1, -1))
                    {
                        success = false;
                    }
                    else
                    {
                        success = true;
                    }
                    summary["success"] = success;
                    EmitSignal(SignalName.RayHit, summary, chunk);
                }
            }
        }
        summary["success"] = success;
        Summary = summary;

    }
}
