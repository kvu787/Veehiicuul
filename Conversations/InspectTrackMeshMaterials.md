# Inspect track mesh materials

## User

C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/Track009_MiniComb4.glb

given that i have this GLB model in a godot scene and i can view it in the godot scene editor, how do i inspect the materials/shaders for each mesh?

## Assistant

I’ll check the project’s Godot setup and the relevant documentation so the steps match your scene.

## Assistant

**Select the mesh node, then inspect the material inside its Mesh resource.** An empty override slot does not mean the mesh has no material.

1. In the **Scene** tree, right-click your `Track009_MiniComb4` instance and enable **Editable Children** if you cannot access its mesh nodes.
2. Expand the instance and select a **MeshInstance3D** child.
3. In the **Inspector**, click its **Mesh** resource (usually an `ArrayMesh`) to expand it.
4. Expand **Surface 0**, **Surface 1**, etc., then click each surface’s **Material** resource.
5. The material’s properties appear: **Albedo**, **Metallic**, **Roughness**, **Shading**, **Transparency**, and so on. A mesh can have multiple surfaces, each with its own material.

Also check these on the mesh node, because they can replace the imported material:

- **GeometryInstance3D → Material Override**: replaces the material for every surface.
- **Surface Material Override → corresponding slot**: replaces that surface’s material.
- **Mesh → Surface → Material**: used when neither override is assigned.

That is their priority order. [Godot material selection documentation](https://docs.godotengine.org/en/4.3/classes/class_meshinstance3d.html#class-meshinstance3d-method-get-active-material).

**For shader code:**

- **ShaderMaterial:** click its **Shader** resource to open the shader editor.
- **StandardMaterial3D:** Godot generates the shader from the material settings, so there is no editable Shader field. To inspect equivalent shader code, work on a duplicate material, choose **Convert to ShaderMaterial** from its resource context menu, then open its **Shader**. Conversion changes the material type; it is unnecessary for inspecting ordinary material settings. [Godot shader documentation](https://docs.godotengine.org/en/stable/tutorials/shaders/your_first_shader/your_first_3d_shader.html).

Your GLB’s import settings currently keep materials embedded, so you do not need to find separate `.tres` files.

## User

where do i find these text names for the glb model in a file/files on the filesystem

![Image #1](InspectTrackMeshMaterialsAssets/SceneNodeNames.png)

## Assistant

I’ll check where those node names are stored in the GLB and the scene files.

## Assistant

Those are **scene node names**, stored inside the [GLB file](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/Track009_MiniComb4.glb) itself, in its embedded JSON under `nodes[].name`.

I checked your file. For example:

- **`BigConeBase_001`** in Godot is **`BigConeBase.001`** in the GLB.
- **`BigCone_001`** is **`BigCone.001`** in the GLB.
- **`SlopeCarBlue`** and **`CheckeredLine`** appear unchanged.

Godot replaces the dots with underscores when importing these node names.

The [TestYo.tscn scene](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/TestYo.tscn:3) only references the GLB instance, so it does **not** list those child names individually. The adjacent `.glb.import` file contains import settings, not the model’s node list.

**To browse the names in a text editor**, you need to extract the GLB’s JSON chunk or convert it to `.gltf`; a GLB is a binary container, not a normal text file. Look for `nodes` and their `name` fields—not `materials` or `meshes`.
