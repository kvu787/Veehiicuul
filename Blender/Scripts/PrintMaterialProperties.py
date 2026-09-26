import bpy


def find_shader_nodes(node_tree, prefix=""):
    for node in node_tree.nodes:
        node_path = prefix + node.name

        if node.type == "GROUP" and node.node_tree is not None:
            yield from find_shader_nodes(node.node_tree, node_path + " / ")
        elif any(socket.type == "SHADER" for socket in node.outputs):
            yield node_path, node


for material in sorted(bpy.data.materials, key=lambda item: item.name.casefold()):
    print(f"\nMaterial: {material.name}")

    if not material.use_nodes or material.node_tree is None:
        print("  Type: No node shader")
        continue

    shader_nodes = list(find_shader_nodes(material.node_tree))

    if not shader_nodes:
        print("  Type: No shader nodes found")
        continue

    for node_path, node in shader_nodes:
        print(f"  Type: {node.bl_label} | Node: {node_path}")

        if node.type == "BSDF_PRINCIPLED":
            roughness = node.inputs["Roughness"]

            if roughness.is_linked:
                print(
                    f"    Roughness: LINKED "
                    f"(stored slider value: {roughness.default_value:.6g}; "
                    "connected nodes are not evaluated)"
                )
            else:
                print(f"    Roughness: {roughness.default_value:.6g}")
