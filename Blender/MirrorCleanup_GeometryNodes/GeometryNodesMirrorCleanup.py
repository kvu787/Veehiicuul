"""Build Mirror -> Geometry Nodes cleanup, with optional later Subdivision.

Run this file in Blender's Text Editor with the editable mesh selected. Python
only constructs the node groups and ordinary property drivers. Mesh evaluation
uses built-in Geometry Nodes, with no handlers, timers, or Python callbacks.
"""

import bpy


PREFIX = '__MirrorCleanup'
VERTEX = PREFIX + 'Vertex'
GROUP = PREFIX + 'Region'
REMOVE = PREFIX + 'RemoveEdge'
DEGREE = PREFIX + 'Degree'
MARK = 'RemoveCenter'
VERSION = 4

# Named Attribute nodes share Blender's attribute namespace with vertex groups.
# In particular, storing a float vertex group called position can corrupt the
# mesh's built-in position layer and crash Blender during evaluation.
MESH_ATTRIBUTES = {'position': ('POINT', 'FLOAT_VECTOR'),
                   'material_index': ('FACE', 'INT'),
                   'sharp_face': ('FACE', 'BOOLEAN'),
                   'sharp_edge': ('EDGE', 'BOOLEAN'),
                   'crease_vert': ('POINT', 'FLOAT'),
                   'crease_edge': ('EDGE', 'FLOAT')}


def validate_attribute_names(source):
    """Reject ambiguous inputs before constructing or replacing any node group."""
    for group in source.vertex_groups:
        if (group.name in MESH_ATTRIBUTES or group.name.startswith(('.', PREFIX))
                or source.data.attributes.get(group.name) is not None):
            raise ValueError('Rename vertex group ' + repr(group.name)
                             + ': its name conflicts with a mesh or cleanup attribute.')
    for attribute in source.data.attributes:
        if attribute.name == MARK or attribute.name.startswith(PREFIX):
            raise ValueError('Rename mesh attribute ' + repr(attribute.name)
                             + ': this name is reserved for mirror cleanup.')
        expected = MESH_ATTRIBUTES.get(attribute.name)
        if expected and (attribute.domain, attribute.data_type) != expected:
            raise ValueError('Mesh attribute ' + repr(attribute.name) + ' must use '
                             + expected[0] + ' domain and ' + expected[1] + ' type.')


class Graph:
    """Small construction helper; the resulting .blend needs none of this code."""

    def __init__(self, name, inputs):
        self.tree = bpy.data.node_groups.new(name, 'GeometryNodeTree')
        self.tree.interface.new_socket(name='Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
        for name, kind, default in inputs:
            socket = self.tree.interface.new_socket(name=name, in_out='INPUT', socket_type=kind)
            if default is not None:
                socket.default_value = default
            if name.endswith('Tolerance'):
                socket.min_value = 0
        self.frame = None
        self.number = 0
        self.input = self.node('NodeGroupInput', 'Inputs')
        self.output = self.node('NodeGroupOutput', 'Result')

    def stage(self, title, x, y=0):
        self.frame = self.tree.nodes.new('NodeFrame')
        self.frame.label = title
        self.frame.name = title
        self.frame.location = (x, y)
        self.number = 0

    def node(self, kind, label, **properties):
        node = self.tree.nodes.new(kind)
        node.label = label
        node.name = label
        for key, value in properties.items():
            setattr(node, key, value)
        if self.frame:
            node.parent = self.frame
        node.location = ((self.number // 5) * 230, -(self.number % 5) * 190)
        self.number += 1
        node.width = 200
        return node

    def put(self, node, key, value):
        socket = node.inputs[key]
        if isinstance(value, bpy.types.NodeSocket):
            self.tree.links.new(value, socket)
        else:
            socket.default_value = value
        return node

    def operation(self, kind, label, inputs, output=0, **properties):
        node = self.node(kind, label, **properties)
        for key, value in inputs.items():
            self.put(node, key, value)
        return node.outputs[output]

    def math(self, operation, a, b=0, label=None):
        return self.operation('ShaderNodeMath', label or operation, {0: a, 1: b}, operation=operation)

    def boolean(self, operation, a, b=False, label=None):
        return self.operation('FunctionNodeBooleanMath', label or operation,
                              {0: a, 1: b}, operation=operation)

    def compare(self, operation, a, b, label=None):
        values = {0: a, 1: b}
        # Blender defaults to an epsilon of 0.001, which would erase small
        # creases. Geometric tolerances use explicit inequalities instead.
        if operation in ('EQUAL', 'NOT_EQUAL'):
            values['Epsilon'] = 0
        return self.operation('FunctionNodeCompare', label or operation, values,
                              data_type='FLOAT', operation=operation)

    def vector(self, operation, a, b=None, label=None):
        values = {0: a}
        if b is not None:
            values[1] = b
        return self.operation('ShaderNodeVectorMath', label or operation, values,
                              output='Value' if operation in ('LENGTH', 'DISTANCE', 'DOT_PRODUCT') else 'Vector',
                              operation=operation)

    def named(self, name, kind='FLOAT'):
        return self.operation('GeometryNodeInputNamedAttribute', name, {'Name': name},
                              output='Attribute', data_type=kind)

    def store(self, geometry, name, value, domain='POINT', kind='FLOAT'):
        return self.operation('GeometryNodeStoreNamedAttribute', 'Store ' + name,
                              {'Geometry': geometry, 'Name': name, 'Value': value},
                              domain=domain, data_type=kind)

    def sample(self, geometry, value, index, domain='POINT', kind='FLOAT', label='Sample'):
        return self.operation('GeometryNodeSampleIndex', label,
                              {'Geometry': geometry, 'Value': value, 'Index': index},
                              domain=domain, data_type=kind, clamp=False)

    def on_domain(self, value, domain, kind='FLOAT'):
        return self.operation('GeometryNodeFieldOnDomain', 'Evaluate on ' + domain,
                              {'Value': value}, domain=domain, data_type=kind)

    def switch(self, condition, false, true, kind='GEOMETRY', label='Choose'):
        return self.operation('GeometryNodeSwitch', label,
                              {'Switch': condition, 'False': false, 'True': true}, input_type=kind)

    def size(self, geometry, output, component='MESH'):
        return self.operation('GeometryNodeAttributeDomainSize', 'Count ' + output,
                              {'Geometry': geometry}, output=output, component=component)

    def join(self, *geometries):
        node = self.node('GeometryNodeJoinGeometry', 'Collect geometry')
        for geometry in geometries:
            self.put(node, 'Geometry', geometry)
        return node.outputs['Geometry']

    def finish(self, geometry):
        self.put(self.output, 'Geometry', geometry)
        self.output.location = (max(node.location.x for node in self.tree.nodes) + 1200, 100)
        self.input.location = (-400, 100)
        return self.tree


INPUTS = [('Geometry', 'NodeSocketGeometry', None),
          ('Mirror X', 'NodeSocketBool', True),
          ('Mirror Y', 'NodeSocketBool', False),
          ('Mirror Z', 'NodeSocketBool', False),
          ('Use Mirror Object', 'NodeSocketBool', False),
          ('Mirror Object', 'NodeSocketObject', None),
          ('Plane Tolerance', 'NodeSocketFloat', 0.00001),
          ('Direction Tolerance', 'NodeSocketFloat', 0.0001)]


def build_analysis():
    g = Graph('Mirror Cleanup - Identify Boundaries', INPUTS)
    geometry = g.input.outputs['Geometry']
    index = g.node('GeometryNodeInputIndex', 'Element index').outputs[0]
    position = g.node('GeometryNodeInputPosition', 'Position').outputs[0]
    g.stage('1. Identify vertices and enabled mirror planes', 0)
    geometry = g.store(geometry, VERTEX, index, kind='INT')
    geometry = g.store(geometry, PREFIX + 'Material', g.named('material_index', 'INT'), 'FACE', 'INT')
    info = g.operation('GeometryNodeObjectInfo', 'Mirror frame relative to this object',
                       {'Object': g.input.outputs['Mirror Object']}, output='Transform', transform_space='RELATIVE')
    inverse = g.operation('FunctionNodeInvertMatrix', 'Object to mirror frame', {'Matrix': info})
    in_frame = g.operation('FunctionNodeTransformPoint', 'Position in mirror coordinates',
                          {'Vector': position, 'Transform': inverse})
    plane_position = g.switch(g.input.outputs['Use Mirror Object'], position, in_frame, 'VECTOR')
    components = g.node('ShaderNodeSeparateXYZ', 'Mirror plane coordinates')
    g.put(components, 'Vector', plane_position)
    edge_vertices = g.node('GeometryNodeInputMeshEdgeVertices', 'Edge endpoints')
    common = None
    on_any = None
    for axis in 'XYZ':
        near = g.compare('LESS_EQUAL', g.math('ABSOLUTE', components.outputs[axis]),
                         g.input.outputs['Plane Tolerance'], 'On ' + axis + ' plane')
        active = g.boolean('AND', near, g.input.outputs['Mirror ' + axis])
        on_any = active if on_any is None else g.boolean('OR', on_any, active)
        ends = [g.sample(geometry, active, edge_vertices.outputs['Vertex Index ' + str(i)],
                         kind='BOOLEAN', label=axis + ' plane at endpoint ' + str(i)) for i in (1, 2)]
        both = g.boolean('AND', *ends, label='Both endpoints on ' + axis)
        common = both if common is None else g.boolean('OR', common, both)
    geometry = g.store(geometry, PREFIX + 'OnPlane', on_any, kind='BOOLEAN')

    g.stage('2. Only coplanar, two-face seam edges', 2400)
    edge_index = g.node('GeometryNodeInputIndex', 'Edge index').outputs[0]
    normal = g.node('GeometryNodeInputNormal', 'Source face normal').outputs[0]
    normals = []
    materials = []
    smooth = []
    for side in (0, 1):
        corner = g.operation('GeometryNodeCornersOfEdge', 'Adjacent corner ' + str(side),
                             {'Edge Index': edge_index, 'Sort Index': side}, output='Corner Index')
        face = g.operation('GeometryNodeFaceOfCorner', 'Adjacent face ' + str(side),
                           {'Corner Index': corner}, output='Face Index')
        normals.append(g.sample(geometry, normal, face, 'FACE', 'FLOAT_VECTOR', 'Adjacent normal'))
        materials.append(g.sample(geometry, g.named('material_index', 'INT'), face, 'FACE', 'INT'))
        smooth.append(g.sample(geometry, g.named('sharp_face', 'BOOLEAN'), face, 'FACE', 'BOOLEAN'))
    parallel = g.compare('LESS_EQUAL', g.vector('DISTANCE', *normals),
                         g.input.outputs['Direction Tolerance'], 'Coplanar face normals')
    face_count = g.node('GeometryNodeInputMeshEdgeNeighbors', 'Faces sharing edge').outputs['Face Count']
    eligible = g.boolean('AND', common, g.compare('EQUAL', face_count, 2))
    eligible = g.boolean('AND', eligible, parallel)
    eligible = g.boolean('AND', eligible, g.compare('EQUAL', *materials))
    eligible = g.boolean('AND', eligible, g.boolean('XNOR', *smooth))
    # Explicit creases and marked sharp edges are topology boundaries.
    for name, kind in (('crease_edge', 'FLOAT'), ('sharp_edge', 'BOOLEAN')):
        eligible = g.boolean('AND', eligible, g.compare('EQUAL', g.named(name, kind), 0))
    geometry = g.store(geometry, REMOVE, eligible, 'EDGE', 'BOOLEAN')

    g.stage('3. Preserve kept junctions and subdivision creases', 4800)
    neighbors = g.node('GeometryNodeInputMeshVertexNeighbors', 'Original vertex connections')
    edge_degree = neighbors.outputs['Vertex Count']
    def remaining_degree(selection):
        keep = g.math('SUBTRACT', 1, selection)
        average = g.on_domain(keep, 'EDGE')
        average = g.on_domain(average, 'POINT')
        return g.math('ROUND', g.math('MULTIPLY', edge_degree, average))
    marked = g.compare('GREATER_THAN', g.named(MARK), 0.5)
    internal = g.compare('LESS_THAN', remaining_degree(g.named(REMOVE, 'BOOLEAN')), 0.5)
    removable_junction = g.boolean('AND', marked, g.compare('EQUAL', g.named('crease_vert'), 0))
    protected = g.boolean('AND', g.boolean('NOT', removable_junction), internal)
    protected_ends = [g.sample(geometry, protected, edge_vertices.outputs['Vertex Index ' + str(i)],
                                kind='BOOLEAN') for i in (1, 2)]
    eligible = g.boolean('AND', g.named(REMOVE, 'BOOLEAN'),
                         g.boolean('NOT', g.boolean('OR', *protected_ends)))
    geometry = g.store(geometry, REMOVE, eligible, 'EDGE', 'BOOLEAN')
    geometry = g.store(geometry, DEGREE, remaining_degree(g.named(REMOVE, 'BOOLEAN')))
    # Preserve marked edge features when removing a midpoint would replace its
    # incident edges and lose their original identity/attributes.
    edge_feature = g.math('MAXIMUM', g.named('crease_edge'), g.named('sharp_edge', 'BOOLEAN'))
    feature_average = g.on_domain(edge_feature, 'EDGE')
    feature_average = g.on_domain(feature_average, 'POINT')
    can_remove = g.boolean('AND', marked, g.compare('EQUAL', g.named('crease_vert'), 0))
    can_remove = g.boolean('AND', can_remove, g.compare('EQUAL', feature_average, 0))
    can_remove = g.boolean('AND', can_remove, g.named(PREFIX + 'OnPlane', 'BOOLEAN'))
    geometry = g.store(geometry, PREFIX + 'CanRemove', can_remove, kind='BOOLEAN')
    # Use original connectivity here, before face seams disappear. A wire point
    # attached to a face or branch must remain even if a curve splits there.
    wire_point = g.boolean('AND', can_remove, g.compare('EQUAL', edge_degree, 2))
    wire_point = g.boolean('AND', wire_point, g.compare('EQUAL', neighbors.outputs['Face Count'], 0))
    geometry = g.store(geometry, PREFIX + 'WireCandidate', wire_point, kind='BOOLEAN')
    return g.finish(geometry)


def rebuild_wires(g, geometry):
    """Build wire chains as poly curves, then reconnect retained points as edges."""
    faces = g.node('GeometryNodeInputMeshEdgeNeighbors', 'Wire edge face count').outputs['Face Count']
    curves = g.operation('GeometryNodeMeshToCurve', 'Order loose edge chains and loops',
                         {'Mesh': geometry, 'Selection': g.compare('EQUAL', faces, 0)}, mode='EDGES')
    index = g.node('GeometryNodeInputIndex', 'Wire point index').outputs[0]
    position = g.node('GeometryNodeInputPosition', 'Wire point position').outputs[0]
    removable = g.named(PREFIX + 'WireCandidate', 'BOOLEAN')
    directions = []
    for offset in (-1, 1):
        neighbor = g.node('GeometryNodeOffsetPointInCurve', 'Adjacent wire point')
        g.put(neighbor, 'Point Index', index)
        g.put(neighbor, 'Offset', offset)
        point = g.sample(curves, position, neighbor.outputs['Point Index'], kind='FLOAT_VECTOR')
        delta = g.vector('SUBTRACT', point, position)
        directions.append(g.vector('NORMALIZE', delta))
        removable = g.boolean('AND', removable, neighbor.outputs['Is Valid Offset'])
        removable = g.boolean('AND', removable, g.compare('GREATER_THAN', g.vector('LENGTH', delta), 0))
    straight = g.compare('LESS_EQUAL', g.vector('LENGTH', g.vector('ADD', *directions)),
                         g.input.outputs['Direction Tolerance'])
    removable = g.boolean('AND', removable, straight)
    # Keep a valid chain/loop even with unusually permissive direction tolerance.
    count = g.node('GeometryNodeSplineLength', 'Points in this wire chain').outputs['Point Count']
    kept_fraction = g.on_domain(g.on_domain(g.math('SUBTRACT', 1, removable), 'POINT'), 'CURVE')
    kept_count = g.math('ROUND', g.math('MULTIPLY', kept_fraction, count))
    cyclic = g.node('GeometryNodeInputSplineCyclic', 'Closed wire loop').outputs[0]
    minimum = g.switch(cyclic, 2, 3, 'INT')
    removable = g.boolean('AND', removable, g.compare('GREATER_EQUAL', kept_count, minimum))
    removed_count = g.operation('GeometryNodeAttributeStatistic', 'Removed wire point count',
                               {'Geometry': curves, 'Attribute': removable},
                               output='Sum', domain='POINT', data_type='FLOAT')
    curves = g.operation('GeometryNodeDeleteGeometry', 'Dissolve marked straight wire points',
                         {'Geometry': curves, 'Selection': removable}, domain='POINT')
    wires = g.operation('GeometryNodeCurveToMesh', 'Reconnect retained wire points', {'Curve': curves})
    return wires, g.compare('GREATER_THAN', removed_count, 0)


def build_reconstruction():
    g = Graph('Mirror Cleanup - Rebuild Polygons', [('Geometry', 'NodeSocketGeometry', None),
                                                   ('Plane Tolerance', 'NodeSocketFloat', 0.00001),
                                                   ('Direction Tolerance', 'NodeSocketFloat', 0.0001)])
    g.tree.interface.new_socket(name='Valid', in_out='OUTPUT', socket_type='NodeSocketBool')
    g.tree.interface.new_socket(name='Changed', in_out='OUTPUT', socket_type='NodeSocketBool')
    g.tree.interface.new_socket(name='Rebuilt Faces', in_out='OUTPUT', socket_type='NodeSocketBool')
    geometry = g.input.outputs['Geometry']
    position = g.node('GeometryNodeInputPosition', 'Position').outputs[0]
    normal = g.node('GeometryNodeInputNormal', 'Face normal').outputs[0]
    g.stage('1. Iterate over connected face regions', 0)
    # Keep wires out of the face pass, including its no-affected-faces shortcut.
    # Otherwise the original wire vertices could be joined back after removal.
    edge_faces = g.node('GeometryNodeInputMeshEdgeNeighbors', 'Surface edge faces').outputs['Face Count']
    surfaces = g.operation('GeometryNodeSeparateGeometry', 'Faces and their edges only',
                           {'Geometry': geometry, 'Selection': g.compare('GREATER_THAN', edge_faces, 0)},
                           domain='EDGE', output='Selection')
    # Most control faces do not touch the seam. Pass those through directly;
    # looping over every face would make even modest control grids expensive.
    edge_fraction = g.on_domain(g.math('ADD', g.named(REMOVE, 'BOOLEAN'), 0), 'EDGE')
    edge_fraction = g.on_domain(edge_fraction, 'FACE')
    point_candidate = g.boolean('AND', g.named(PREFIX + 'CanRemove', 'BOOLEAN'),
                                g.compare('EQUAL', g.named(DEGREE), 2))
    point_fraction = g.on_domain(g.math('ADD', point_candidate, 0), 'POINT')
    point_fraction = g.on_domain(point_fraction, 'FACE')
    affected = g.boolean('OR', g.compare('GREATER_THAN', edge_fraction, 0),
                         g.compare('GREATER_THAN', point_fraction, 0))
    partition = g.node('GeometryNodeSeparateGeometry', 'Rebuild only affected faces', domain='FACE')
    g.put(partition, 'Geometry', surfaces)
    g.put(partition, 'Selection', affected)
    compact_groups = g.operation('GeometryNodeEdgesToFaceGroups', 'Compact affected region indices',
                                 {'Boundary Edges': g.boolean('NOT', g.named(REMOVE, 'BOOLEAN'))}, output='Face Group ID')
    affected_geometry = g.store(partition.outputs['Selection'], GROUP, compact_groups, 'FACE', 'INT')
    region = g.named(GROUP, 'INT')
    maximum = g.operation('GeometryNodeAttributeStatistic', 'Last face region',
                          {'Geometry': affected_geometry, 'Attribute': region}, output='Max', domain='FACE', data_type='FLOAT')
    iterations = g.math('ADD', maximum, 1)
    rebuilt_faces = g.compare('GREATER_THAN', g.size(affected_geometry, 'Face Count'), 0)
    iterations = g.switch(rebuilt_faces, 0, iterations, 'INT')
    end = g.node('GeometryNodeRepeatOutput', 'Collected polygons')
    end.repeat_items.clear()
    for kind, name in (('GEOMETRY', 'Source'), ('GEOMETRY', 'Polygons'), ('BOOLEAN', 'Valid')):
        end.repeat_items.new(kind, name)
    start = g.node('GeometryNodeRepeatInput', 'For each region')
    start.pair_with_output(end)
    g.put(start, 'Iterations', iterations)
    g.put(start, 'Source', affected_geometry)
    g.put(start, 'Polygons', partition.outputs['Inverted'])
    g.put(start, 'Valid', True)
    source = start.outputs['Source']
    selected = g.operation('GeometryNodeSeparateGeometry', 'Current region only',
                           {'Geometry': source, 'Selection': g.compare('EQUAL', region, start.outputs['Iteration'])},
                           domain='FACE', output='Selection')
    boundary = g.compare('EQUAL', g.node('GeometryNodeInputMeshEdgeNeighbors', 'Region edge faces').outputs['Face Count'], 1)
    curves = g.operation('GeometryNodeMeshToCurve', 'Ordered boundary loops',
                         {'Mesh': selected, 'Selection': boundary}, mode='EDGES')
    g.stage('2. Validate the complete region before rebuilding', 2400)
    # Face groups describe connectivity, not a guaranteed disk. In particular,
    # a path around a retained edge can put both of its faces in the same group.
    # Rebuilding just the exterior would silently discard that edge. Interior
    # vertices attached to loose edges also cannot be represented by one ngon.
    boundary_fraction = g.on_domain(g.on_domain(boundary, 'EDGE'), 'POINT')
    required = g.boolean('OR', g.boolean('NOT', g.named(PREFIX + 'CanRemove', 'BOOLEAN')),
                         g.compare('GREATER_THAN', g.named(DEGREE), 0))
    enclosed = g.boolean('AND', g.compare('EQUAL', boundary_fraction, 0), required)
    enclosed_count = g.operation('GeometryNodeAttributeStatistic', 'Required interior vertices',
                                 {'Geometry': selected, 'Attribute': enclosed},
                                 output='Sum', domain='POINT', data_type='FLOAT')
    retained_inside = g.boolean('AND', g.boolean('NOT', boundary),
                                g.boolean('NOT', g.named(REMOVE, 'BOOLEAN')))
    retained_count = g.operation('GeometryNodeAttributeStatistic', 'Retained interior edges',
                                 {'Geometry': selected, 'Attribute': retained_inside},
                                 output='Sum', domain='EDGE', data_type='FLOAT')
    valid = g.compare('EQUAL', g.size(curves, 'Spline Count', 'CURVE'), 1)
    cyclic = g.node('GeometryNodeInputSplineCyclic', 'Closed boundary').outputs[0]
    valid = g.boolean('AND', valid, g.sample(curves, cyclic, 0, 'CURVE', 'BOOLEAN'))
    valid = g.boolean('AND', valid, g.compare('EQUAL', enclosed_count, 0))
    valid = g.boolean('AND', valid, g.compare('EQUAL', retained_count, 0))
    # Matching average face normals do not prove that ngons are planar. A pair
    # of mirrored, warped quads can have equal normals but different surfaces.
    target_normal = g.sample(selected, normal, 0, 'FACE', 'FLOAT_VECTOR', 'Preserve source winding')
    anchor = g.sample(selected, position, 0, kind='FLOAT_VECTOR')
    distance = g.math('ABSOLUTE', g.vector('DOT_PRODUCT', g.vector('SUBTRACT', position, anchor), target_normal))
    maximum_distance = g.operation('GeometryNodeAttributeStatistic', 'Distance from the region plane',
                                   {'Geometry': selected, 'Attribute': distance},
                                   output='Max', domain='POINT', data_type='FLOAT')
    valid = g.boolean('AND', valid, g.compare('LESS_EQUAL', maximum_distance, g.input.outputs['Plane Tolerance']))

    g.stage('3. Select marked, straight boundary points of degree two', 5000)
    index = g.node('GeometryNodeInputIndex', 'Boundary point index').outputs[0]
    directions = []
    for offset in (-1, 1):
        neighbor = g.operation('GeometryNodeOffsetPointInCurve', 'Neighbor along boundary',
                               {'Point Index': index, 'Offset': offset}, output='Point Index')
        point = g.sample(curves, position, neighbor, kind='FLOAT_VECTOR')
        directions.append(g.vector('NORMALIZE', g.vector('SUBTRACT', point, position)))
    straight = g.compare('LESS_EQUAL', g.vector('LENGTH', g.vector('ADD', *directions)),
                         g.input.outputs['Direction Tolerance'])
    removable = g.boolean('AND', g.named(PREFIX + 'CanRemove', 'BOOLEAN'), straight)
    removable = g.boolean('AND', removable, g.compare('EQUAL', g.named(DEGREE), 2))
    curves = g.operation('GeometryNodeDeleteGeometry', 'Remove selected boundary midpoints',
                         {'Geometry': curves, 'Selection': removable}, domain='POINT')

    g.stage('4. Make one ngon from the ordered boundary', 6800)
    count = g.size(curves, 'Point Count', 'CURVE')
    circle = g.operation('GeometryNodeMeshCircle', 'Ngon with the same corner count',
                         {'Vertices': count}, fill_type='NGON')
    vertex = g.sample(curves, g.named(VERTEX, 'INT'), index, kind='INT', label='Original vertex identity')
    polygon = g.store(circle, VERTEX, vertex, kind='INT')
    points = g.sample(curves, position, index, kind='FLOAT_VECTOR', label='Exact boundary positions')
    polygon = g.operation('GeometryNodeSetPosition', 'Reconstruct the original boundary',
                          {'Geometry': polygon, 'Position': points})
    flip = g.compare('LESS_THAN', g.vector('DOT_PRODUCT', normal, target_normal), 0)
    polygon = g.operation('GeometryNodeFlipFaces', 'Correct boundary orientation',
                          {'Mesh': polygon, 'Selection': flip})
    # A rebuilt region inherits the material and smoothing of its first face.
    for name, kind in (('material_index', 'INT'), ('sharp_face', 'BOOLEAN')):
        value = g.sample(selected, g.named(name, kind), 0, 'FACE', kind)
        polygon = g.store(polygon, PREFIX + 'Material' if name == 'material_index' else name, value, 'FACE', kind)
    valid = g.boolean('AND', valid, g.compare('GREATER_EQUAL', count, 3))
    g.put(end, 'Source', source)
    g.put(end, 'Polygons', g.join(start.outputs['Polygons'], polygon))
    g.put(end, 'Valid', g.boolean('AND', start.outputs['Valid'], valid))

    g.stage('5. Clean wire chains while preserving branches and face connections', 9100)
    loose_edges, changed_wires = rebuild_wires(g, geometry)
    g.stage('6. Keep isolated points and weld by original vertex identity', 12400)
    degree = g.node('GeometryNodeInputMeshVertexNeighbors', 'Isolated vertices').outputs['Vertex Count']
    loose_points = g.operation('GeometryNodeSeparateGeometry', 'Retain isolated points',
                               {'Geometry': geometry, 'Selection': g.compare('EQUAL', degree, 0)},
                               domain='POINT', output='Selection')
    combined = g.join(end.outputs['Polygons'], loose_edges, loose_points)
    identity = g.operation('ShaderNodeCombineXYZ', 'Separate coordinates for each source identity',
                           {'X': g.named(VERTEX, 'INT')})
    combined = g.operation('GeometryNodeSetPosition', 'Weld matching IDs only',
                           {'Geometry': combined, 'Position': identity})
    combined = g.operation('GeometryNodeMergeByDistance', 'Join copies of the same source vertex',
                           {'Geometry': combined, 'Distance': 0.1}, mode='ALL')
    points = g.sample(geometry, position, g.named(VERTEX, 'INT'), kind='FLOAT_VECTOR')
    combined = g.operation('GeometryNodeSetPosition', 'Restore exact mesh coordinates',
                           {'Geometry': combined, 'Position': points})
    warning = g.node('GeometryNodeWarning', 'Unsupported boundary warning', warning_type='WARNING')
    g.put(warning, 'Show', g.boolean('NOT', end.outputs['Valid']))
    g.put(warning, 'Message', 'Mirror Cleanup: a region is nonplanar, has holes, a branching boundary, or retained interior geometry. Passing the entire mirrored mesh through unchanged.')
    g.put(g.output, 'Valid', g.boolean('NOT', warning.outputs['Show']))
    g.put(g.output, 'Changed', g.boolean('OR', rebuilt_faces, changed_wires))
    g.put(g.output, 'Rebuilt Faces', rebuilt_faces)
    result = g.switch(warning.outputs['Show'], combined, geometry, label='Safe fallback for unsupported regions')
    return g.finish(result)


def build_group(source):
    validate_attribute_names(source)
    g = Graph('Selective Mirror Cleanup', INPUTS + [
        ('Mirror in Viewport', 'NodeSocketBool', True),
        ('Mirror in Render', 'NodeSocketBool', True)])
    g.tree.is_modifier = True
    g.tree['MirrorCleanupVersion'] = VERSION
    g.stage('Analyze mirrored mesh', 0)
    analysis = g.node('GeometryNodeGroup', 'Identify seam edges and keep/remove choices', node_tree=build_analysis())
    for socket in analysis.inputs:
        g.put(analysis, socket.name, g.input.outputs[socket.name])
    g.stage('Reconstruct faces and wire chains', 900)
    rebuild = g.node('GeometryNodeGroup', 'Rebuild polygons and remove chosen midpoints', node_tree=build_reconstruction())
    g.put(rebuild, 'Geometry', analysis.outputs['Geometry'])
    g.put(rebuild, 'Plane Tolerance', g.input.outputs['Plane Tolerance'])
    g.put(rebuild, 'Direction Tolerance', g.input.outputs['Direction Tolerance'])
    geometry = rebuild.outputs['Geometry']
    g.stage('Restore point attributes', 1700)
    original = g.input.outputs['Geometry']
    # Point attributes are sampled by identity, never by nearest spatial point.
    attributes = {'crease_vert': 'FLOAT', MARK: 'FLOAT'}
    attributes.update({group.name: 'FLOAT' for group in source.vertex_groups})
    supported = {'FLOAT', 'INT', 'FLOAT_VECTOR', 'FLOAT_COLOR', 'BYTE_COLOR',
                 'BOOLEAN', 'QUATERNION', 'FLOAT2', 'INT8'}
    attributes.update({attr.name: attr.data_type for attr in source.data.attributes
                       if attr.domain == 'POINT' and attr.data_type in supported
                       and attr.name != 'position' and not attr.name.startswith('.')})
    for name, kind in attributes.items():
        # Compact storage types do not have their own field socket types. Read
        # and sample through the corresponding full type, then store exactly.
        field_kind = {'BYTE_COLOR': 'FLOAT_COLOR', 'FLOAT2': 'FLOAT_VECTOR', 'INT8': 'INT'}.get(kind, kind)
        value = g.sample(original, g.named(name, field_kind), g.named(VERTEX, 'INT'), kind=field_kind)
        geometry = g.store(geometry, name, value, kind=kind)

    g.stage('Restore crease and sharpness on surviving edges', 3000)
    endpoints = g.node('GeometryNodeInputMeshEdgeVertices', 'Original edge endpoint indices')
    def edge_key(a, b):
        return g.operation('ShaderNodeCombineXYZ', 'Endpoint identity pair',
                           {'X': g.math('MINIMUM', a, b), 'Y': g.math('MAXIMUM', a, b)})
    original_key = edge_key(endpoints.outputs['Vertex Index 1'], endpoints.outputs['Vertex Index 2'])
    edge_index = g.node('GeometryNodeInputIndex', 'Original edge identity').outputs[0]
    indexed_edges = g.store(original, PREFIX + 'Edge', edge_index, 'EDGE', 'INT')
    edge_points = g.operation('GeometryNodeMeshToPoints', 'Original edges indexed by endpoints',
                              {'Mesh': indexed_edges, 'Position': original_key}, mode='EDGES')
    # Sample reserved edge attributes on their original domain: attributes such
    # as sharp_edge cannot be propagated onto the point domain of this lookup.
    vertices = [g.operation('GeometryNodeFieldAtIndex', 'Rebuilt edge endpoint identity',
                            {'Index': endpoints.outputs['Vertex Index ' + str(i)], 'Value': g.named(VERTEX, 'INT')},
                            domain='POINT', data_type='INT') for i in (1, 2)]
    key = edge_key(*vertices)
    nearest = g.operation('GeometryNodeSampleNearest', 'Find matching original edge',
                          {'Geometry': edge_points, 'Sample Position': key}, domain='POINT')
    position = g.node('GeometryNodeInputPosition', 'Encoded edge identity').outputs[0]
    sampled_key = g.sample(edge_points, position, nearest, kind='FLOAT_VECTOR')
    matching = g.compare('LESS_THAN', g.vector('DISTANCE', key, sampled_key), 0.1)
    source_edge = g.sample(edge_points, g.named(PREFIX + 'Edge', 'INT'), nearest, kind='INT')
    for name, kind in (('crease_edge', 'FLOAT'), ('sharp_edge', 'BOOLEAN')):
        value = g.sample(original, g.named(name, kind), source_edge, 'EDGE', kind)
        value = g.switch(matching, 0 if kind == 'FLOAT' else False, value, kind)
        geometry = g.store(geometry, name, value, 'EDGE', kind)
    # Primitive ngons have no material slots of their own. Restore actual slots,
    # not merely the material_index numbers copied from the input faces.
    for index, slot in enumerate(source.material_slots):
        material = slot.material
        if material is not None:
            geometry = g.operation('GeometryNodeSetMaterial', 'Restore material ' + material.name,
                                   {'Geometry': geometry, 'Selection': g.compare('EQUAL', g.named(PREFIX + 'Material', 'INT'), index),
                                    'Material': material})
    g.stage('Honor Mirror visibility and enabled axes', 5200)
    active = g.boolean('OR', g.input.outputs['Mirror X'], g.input.outputs['Mirror Y'])
    active = g.boolean('OR', active, g.input.outputs['Mirror Z'])
    viewport = g.node('GeometryNodeIsViewport', 'Current evaluation context').outputs[0]
    enabled = g.switch(viewport, g.input.outputs['Mirror in Render'],
                       g.input.outputs['Mirror in Viewport'], 'BOOLEAN', 'Mirror enabled in this context')
    active = g.boolean('AND', active, enabled)
    if any(attribute.domain == 'CORNER' and not attribute.name.startswith('.')
           for attribute in source.data.attributes):
        warning = g.node('GeometryNodeWarning', 'Prototype attribute limitation', warning_type='WARNING')
        g.put(warning, 'Show', g.boolean('AND', rebuild.outputs['Valid'], rebuild.outputs['Rebuilt Faces']))
        g.put(warning, 'Message', 'Mirror Cleanup prototype: rebuilt faces do not retain UV maps or other corner attributes.')
        # The warning must be connected for evaluation without gating wire-only
        # cleanup on the presence of reconstructed faces.
        geometry = g.switch(warning.outputs['Show'], geometry, geometry)
    geometry = g.operation('GeometryNodeRemoveAttribute', 'Remove temporary attributes',
                           {'Geometry': geometry, 'Name': PREFIX + '*'}, pattern_mode='WILDCARD')
    # Bypass restoration as well as reconstruction on failure, so the fallback
    # preserves all input attributes and material slots without modification.
    active = g.boolean('AND', active, rebuild.outputs['Valid'])
    active = g.boolean('AND', active, rebuild.outputs['Changed'])
    geometry = g.switch(active, original, geometry)
    return g.finish(geometry)


def remove_cleanup_modifier(source, modifier):
    """Remove only this modifier's drivers and its unused generated groups."""
    if source.animation_data:
        path = modifier.path_from_id()
        for curve in list(source.animation_data.drivers):
            if curve.data_path.startswith((path + '[', path + '.')):
                source.animation_data.drivers.remove(curve)
    tree = modifier.node_group
    children = {node.node_tree for node in tree.nodes if node.type == 'GROUP' and node.node_tree} if tree else set()
    source.modifiers.remove(modifier)
    if tree and tree.users == 0:
        bpy.data.node_groups.remove(tree)
        for child in children:
            if child.users == 0:
                bpy.data.node_groups.remove(child)


def setup(source):
    """Install/replace cleanup immediately after the last Mirror on this object."""
    if source is None or source.type != 'MESH':
        raise ValueError('Select the editable mesh object first.')
    if source.mode != 'OBJECT':
        raise ValueError('Switch to Object Mode before setting up cleanup.')
    mirrors = [modifier for modifier in source.modifiers if modifier.type == 'MIRROR']
    if not mirrors:
        raise ValueError('Add a Mirror modifier to the selected mesh first.')
    mirror = mirrors[-1]
    if not mirror.use_mirror_merge:
        raise ValueError('Enable Merge on the Mirror modifier before setting up cleanup.')
    existing = [modifier for modifier in source.modifiers if modifier.type == 'NODES'
                and modifier.node_group and modifier.node_group.get('MirrorCleanupVersion')]
    settings = {}
    display = {}
    if existing:
        current = existing[-1]
        for socket in current.node_group.interface.items_tree:
            if socket.item_type == 'SOCKET' and socket.in_out == 'INPUT' and socket.name.endswith('Tolerance'):
                settings[socket.name] = current.get(socket.identifier, socket.default_value)
        display = {name: getattr(current, name) for name in
                   ('show_viewport', 'show_render', 'show_in_editmode', 'show_on_cage', 'show_expanded')}
    # Prepare and configure the replacement before removing a working setup.
    # Construction can fail, for example when Blender cannot create a node.
    previous_groups = set(bpy.data.node_groups)
    modifier = None
    created_mark = None
    try:
        tree = build_group(source)
        if source.vertex_groups.get(MARK) is None:
            created_mark = source.vertex_groups.new(name=MARK)
        modifier = source.modifiers.new('Selective Mirror Cleanup', 'NODES')
        modifier.node_group = tree
        modifier.show_in_editmode = True
        for name, value in display.items():
            setattr(modifier, name, value)
        source.modifiers.move(list(source.modifiers).index(modifier), list(source.modifiers).index(mirror) + 1)
        for socket in tree.interface.items_tree:
            if socket.item_type != 'SOCKET' or socket.in_out != 'INPUT':
                continue
            if socket.name in ('Mirror X', 'Mirror Y', 'Mirror Z'):
                axis = 'XYZ'.index(socket.name[-1])
                data_path = mirror.path_from_id('use_axis') + '[' + str(axis) + ']'
            elif socket.name in ('Mirror in Viewport', 'Mirror in Render'):
                data_path = mirror.path_from_id('show_viewport' if socket.name == 'Mirror in Viewport' else 'show_render')
            else:
                data_path = None
            if data_path is not None:
                curve = modifier.driver_add('["' + socket.identifier + '"]')
                curve.driver.type = 'AVERAGE'
                variable = curve.driver.variables.new()
                variable.name = 'MirrorSetting'
                variable.type = 'SINGLE_PROP'
                variable.targets[0].id = source
                variable.targets[0].data_path = data_path
            elif socket.name == 'Use Mirror Object':
                modifier[socket.identifier] = mirror.mirror_object is not None
            elif socket.name == 'Mirror Object':
                modifier[socket.identifier] = mirror.mirror_object
            elif socket.name in settings:
                modifier[socket.identifier] = settings[socket.name]
    except Exception:
        if modifier is not None:
            remove_cleanup_modifier(source, modifier)
        if created_mark is not None:
            source.vertex_groups.remove(created_mark)
        for tree in set(bpy.data.node_groups) - previous_groups:
            bpy.data.node_groups.remove(tree)
        raise
    for old_modifier in existing:
        remove_cleanup_modifier(source, old_modifier)
    modifier.name = 'Selective Mirror Cleanup'
    source.update_tag()
    return modifier


if __name__ == '__main__':
    modifier = setup(bpy.context.object)
    print('Installed', modifier.name, 'on', bpy.context.object.name)
