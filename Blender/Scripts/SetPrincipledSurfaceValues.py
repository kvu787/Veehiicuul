"""Run in Blender 4.5.x's Text Editor to update all materials in the file.

Includes nested node groups and removes links feeding the two target inputs.
The file is not automatically saved.
"""

import bpy


def SetSurfaceValues(NodeTree, Visited):
    if NodeTree in Visited:
        return 0
    Visited.add(NodeTree)

    if not NodeTree.is_editable:
        print(f"Skipped read-only node tree: {NodeTree.name}")
        return 0

    Updated = 0
    for Node in NodeTree.nodes:
        if Node.type == 'BSDF_PRINCIPLED':
            for InputName, Value in (
                ('Roughness', 0.5),
                ('Specular IOR Level', 0.0),
            ):
                Socket = Node.inputs[InputName]
                for Link in list(Socket.links):
                    NodeTree.links.remove(Link)
                Socket.default_value = Value
            Updated += 1
        elif Node.type == 'GROUP' and Node.node_tree is not None:
            Updated += SetSurfaceValues(Node.node_tree, Visited)
    return Updated


Visited = set()
Updated = 0
for Material in bpy.data.materials:
    if Material.use_nodes and Material.node_tree is not None:
        Updated += SetSurfaceValues(Material.node_tree, Visited)

print(f"Updated {Updated} Principled BSDF nodes.")
