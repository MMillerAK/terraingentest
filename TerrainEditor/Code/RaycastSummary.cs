using Godot;
using Godot.Collections;
using System;

public partial class RaycastSummary : PanelContainer
{
    [Export]
    Label raycastLabel;

    public void ConnectRay(TerrainRayCast3d terrainRayCast3D)
    {
        terrainRayCast3D.RayHit += update;
    }
    public void update(Dictionary results, Chunk chunk)
    {
        if (results == null)
            return;
        if (results.Count == 0)
            return;

        if ((bool)results["success"] == false)
        {
            return;
        }
        int verts = (int)results["vertices"];
        int edges = (int)results["edges"];
        int triangles = (int)results["triangles"];

        int highlightedFace = (int)results["highlighted"];
        int triangleHighlight = (int)results["triangleHighlight"];


        string summary = @$"

        Summary:
        verts: {verts};
        edges: {edges};
        triangles: {triangles};
        

        highlighted:
        normal = 
      
        mouse: {highlightedFace};
        
        tr: {triangleHighlight};

        voxel: {(Vector3)results["voxel"]}
        ";

        raycastLabel.Text = summary;
        //  GD.Print(summary);
    }

}
