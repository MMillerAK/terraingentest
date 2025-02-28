using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class Display : Control
{

    [Export]
    Label Faces;
    [Export]
    Label Edges;
    [Export]
    Label Verts;

    [Export]
    Label raycast;
    public MeshDataTool meshDataTool;


    public override void _Ready()
    {
        base._Ready();
    }

    public void Loaddata(MeshDataTool meshDataTool)
    {
        this.meshDataTool = meshDataTool;
        Refresh();
    }


    void Refresh()
    {
        Faces.Text = $"Tris: {meshDataTool.GetFaceCount()}";

        Edges.Text = $"Edges: {meshDataTool.GetEdgeCount()}";
        Verts.Text = $"verts: {meshDataTool.GetVertexCount()}";

    }

    public void ShowCast(Dictionary query)
    {
        Node3D collider = (Node3D)query["collider"];
        string collidername = collider.Name;
        int colliderID = (int)query["collider_id"];
        Vector3 normal = (Vector3)query["normal"];
        Vector3 pos = (Vector3)query["position"];
        int faceID = (int)query["face_index"];

        string summary = @$"Raycast:
        Object: {collidername}
        ID: {colliderID}
        Normal: {normal}
        intersect @{pos};
        face: {faceID};
        ";

        raycast.Text = summary;
        GD.Print(summary);
    }

}
