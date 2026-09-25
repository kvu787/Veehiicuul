#if TOOLS
using Godot;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Disables specular on embedded mesh materials before an imported scene is saved.</summary>
[Tool]
public partial class DisableSpecularImport : EditorScenePostImport {
    public override GodotObject _PostImport(Node scene) {
        DisableSpecular(scene);
        return scene;
    }

    private static void DisableSpecular(Node node) {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh is Mesh mesh) {
            for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++) {
                if (meshInstance.GetActiveMaterial(surface) is BaseMaterial3D material) {
                    material.MetallicSpecular = 0.0f;
                }
            }
        }

        foreach (Node child in node.GetChildren()) {
            DisableSpecular(child);
        }
    }
}
#endif
