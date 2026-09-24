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
