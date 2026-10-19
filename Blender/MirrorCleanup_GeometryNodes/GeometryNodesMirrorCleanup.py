"""Build a real Mirror -> Geometry Nodes -> Subdivision stack in Blender 4.5.14.

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
VERSION = 1


class Graph:
    """Small construction helper; the resulting .blend needs none of this code."""

    def __init__(self, name, inputs):
        self.tree = bpy.data.node_groups.new(name, 'GeometryNodeTree')
        self.tree.interface.new_socket(name='Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
        for name, kind, default in inputs:
            socket = self.tree.interface.new_socket(name=name, in_out='INPUT', socket_type=kind)
            if default is not None:
                socket.default_value = default
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
        return self.operation('FunctionNodeCompare', label or operation, {0: a, 1: b},
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
    edge_degree = g.node('GeometryNodeInputMeshVertexNeighbors', 'Original vertex degree').outputs['Vertex Count']
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
    groups = g.operation('GeometryNodeEdgesToFaceGroups', 'Regions separated by retained edges',
                         {'Boundary Edges': g.boolean('NOT', g.named(REMOVE, 'BOOLEAN'))}, output='Face Group ID')
    geometry = g.store(geometry, GROUP, groups, 'FACE', 'INT')
    return g.finish(geometry)


def build_reconstruction():
    g = Graph('Mirror Cleanup - Rebuild Polygons', [('Geometry', 'NodeSocketGeometry', None),
                                                   ('Direction Tolerance', 'NodeSocketFloat', 0.0001)])
    geometry = g.input.outputs['Geometry']
    g.stage('1. Iterate over connected face regions', 0)
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
    g.put(partition, 'Geometry', geometry)
    g.put(partition, 'Selection', affected)
    compact_groups = g.operation('GeometryNodeEdgesToFaceGroups', 'Compact affected region indices',
                                 {'Boundary Edges': g.boolean('NOT', g.named(REMOVE, 'BOOLEAN'))}, output='Face Group ID')
    affected_geometry = g.store(partition.outputs['Selection'], GROUP, compact_groups, 'FACE', 'INT')
    region = g.named(GROUP, 'INT')
    maximum = g.operation('GeometryNodeAttributeStatistic', 'Last face region',
                          {'Geometry': affected_geometry, 'Attribute': region}, output='Max', domain='FACE', data_type='FLOAT')
    iterations = g.math('ADD', maximum, 1)
    iterations = g.switch(g.compare('GREATER_THAN', g.size(affected_geometry, 'Face Count'), 0), 0, iterations, 'INT')
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

    g.stage('2. Select marked, straight boundary points of degree two', 2400)
    index = g.node('GeometryNodeInputIndex', 'Boundary point index').outputs[0]
    position = g.node('GeometryNodeInputPosition', 'Boundary position').outputs[0]
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

    g.stage('3. Make one ngon from the ordered boundary', 4200)
    count = g.size(curves, 'Point Count', 'CURVE')
    circle = g.operation('GeometryNodeMeshCircle', 'Ngon with the same corner count',
                         {'Vertices': count}, fill_type='NGON')
    vertex = g.sample(curves, g.named(VERTEX, 'INT'), index, kind='INT', label='Original vertex identity')
    polygon = g.store(circle, VERTEX, vertex, kind='INT')
    points = g.sample(curves, position, index, kind='FLOAT_VECTOR', label='Exact boundary positions')
    polygon = g.operation('GeometryNodeSetPosition', 'Reconstruct the original boundary',
                          {'Geometry': polygon, 'Position': points})
    normal = g.node('GeometryNodeInputNormal', 'Face normal').outputs[0]
    target_normal = g.sample(selected, normal, 0, 'FACE', 'FLOAT_VECTOR', 'Preserve source winding')
    flip = g.compare('LESS_THAN', g.vector('DOT_PRODUCT', normal, target_normal), 0)
    polygon = g.operation('GeometryNodeFlipFaces', 'Correct boundary orientation',
                          {'Mesh': polygon, 'Selection': flip})
    # A rebuilt region inherits the material and smoothing of its first face.
    for name, kind in (('material_index', 'INT'), ('sharp_face', 'BOOLEAN')):
        value = g.sample(selected, g.named(name, kind), 0, 'FACE', kind)
        polygon = g.store(polygon, PREFIX + 'Material' if name == 'material_index' else name, value, 'FACE', kind)
    valid = g.compare('EQUAL', g.size(curves, 'Spline Count', 'CURVE'), 1)
    cyclic = g.node('GeometryNodeInputSplineCyclic', 'Closed boundary').outputs[0]
    closed = g.sample(curves, cyclic, 0, 'CURVE', 'BOOLEAN')
    valid = g.boolean('AND', valid, closed)
    valid = g.boolean('AND', valid, g.compare('GREATER_EQUAL', count, 3))
    g.put(end, 'Source', source)
    g.put(end, 'Polygons', g.join(start.outputs['Polygons'], polygon))
    g.put(end, 'Valid', g.boolean('AND', start.outputs['Valid'], valid))

    g.stage('4. Keep loose geometry and weld by original vertex identity', 6500)
    faces = g.node('GeometryNodeInputMeshEdgeNeighbors', 'Loose edges').outputs['Face Count']
    loose_edges = g.operation('GeometryNodeSeparateGeometry', 'Retain wire edges',
                              {'Geometry': geometry, 'Selection': g.compare('EQUAL', faces, 0)},
                              domain='EDGE', output='Selection')
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
    g.put(warning, 'Message', 'Mirror Cleanup: a region has holes or a branching boundary. Passing the entire mirrored mesh through unchanged.')
    result = g.switch(warning.outputs['Show'], combined, geometry, label='Safe fallback for unsupported regions')
    return g.finish(result)


def build_group(source):
    g = Graph('Selective Mirror Cleanup', INPUTS)
    g.tree.is_modifier = True
    g.tree['MirrorCleanupVersion'] = VERSION
    g.stage('Analyze mirrored mesh', 0)
    analysis = g.node('GeometryNodeGroup', 'Identify seam edges and keep/remove choices', node_tree=build_analysis())
    for socket in analysis.inputs:
        g.put(analysis, socket.name, g.input.outputs[socket.name])
    g.stage('Reconstruct control mesh before Subdivision', 900)
    rebuild = g.node('GeometryNodeGroup', 'Rebuild polygons and remove chosen midpoints', node_tree=build_reconstruction())
    g.put(rebuild, 'Geometry', analysis.outputs['Geometry'])
    g.put(rebuild, 'Direction Tolerance', g.input.outputs['Direction Tolerance'])
    geometry = rebuild.outputs['Geometry']
    g.stage('Restore point attributes', 1700)
    original = g.input.outputs['Geometry']
    # Point attributes are sampled by identity, never by nearest spatial point.
    attributes = {'crease_vert': 'FLOAT', MARK: 'FLOAT'}
    attributes.update({group.name: 'FLOAT' for group in source.vertex_groups})
    supported = {'FLOAT', 'INT', 'FLOAT_VECTOR', 'FLOAT_COLOR', 'BOOLEAN', 'QUATERNION', 'FLOAT2', 'INT8'}
    attributes.update({attr.name: attr.data_type for attr in source.data.attributes
                       if attr.domain == 'POINT' and attr.data_type in supported
                       and attr.name != 'position' and not attr.name.startswith('.')})
    for name, kind in attributes.items():
        if kind in ('FLOAT2', 'INT8'):
            continue
        value = g.sample(original, g.named(name, kind), g.named(VERTEX, 'INT'), kind=kind)
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
    for index, material in enumerate(source.data.materials):
        if material is not None:
            geometry = g.operation('GeometryNodeSetMaterial', 'Restore material ' + material.name,
                                   {'Geometry': geometry, 'Selection': g.compare('EQUAL', g.named(PREFIX + 'Material', 'INT'), index),
                                    'Material': material})
    g.stage('No enabled mirror axes means no cleanup', 5200)
    active = g.boolean('OR', g.input.outputs['Mirror X'], g.input.outputs['Mirror Y'])
    active = g.boolean('OR', active, g.input.outputs['Mirror Z'])
    if any(attribute.domain == 'CORNER' and not attribute.name.startswith('.')
           for attribute in source.data.attributes):
        warning = g.node('GeometryNodeWarning', 'Prototype attribute limitation', warning_type='WARNING')
        g.put(warning, 'Show', True)
        g.put(warning, 'Message', 'Mirror Cleanup prototype: rebuilt faces do not retain UV maps or other corner attributes.')
        active = g.boolean('AND', active, warning.outputs['Show'])
    for name in (VERTEX, GROUP, REMOVE, DEGREE, PREFIX + 'OnPlane', PREFIX + 'CanRemove', PREFIX + 'Material'):
        geometry = g.operation('GeometryNodeRemoveAttribute', 'Remove temporary attribute',
                               {'Geometry': geometry, 'Name': name})
    geometry = g.switch(active, original, geometry)
    return g.finish(geometry)


def setup(source):
    """Install/replace cleanup immediately after the last Mirror on this object."""
    if source is None or source.type != 'MESH':
        raise ValueError('Select the editable mesh object first.')
    mirrors = [modifier for modifier in source.modifiers if modifier.type == 'MIRROR']
    if not mirrors:
        raise ValueError('Add a Mirror modifier to the selected mesh first.')
    mirror = mirrors[-1]
    if not mirror.use_mirror_merge:
        raise ValueError('Enable Merge on the Mirror modifier before setting up cleanup.')
    source.vertex_groups.get(MARK) or source.vertex_groups.new(name=MARK)
    existing = [modifier for modifier in source.modifiers if modifier.type == 'NODES'
                and modifier.node_group and modifier.node_group.get('MirrorCleanupVersion')]
    for modifier in existing:
        if source.animation_data:
            path = modifier.path_from_id()
            for curve in list(source.animation_data.drivers):
                if curve.data_path.startswith(path + '['):
                    source.animation_data.drivers.remove(curve)
        old_group = modifier.node_group
        children = [node.node_tree for node in old_group.nodes if node.type == 'GROUP']
        source.modifiers.remove(modifier)
        if old_group.users == 0:
            bpy.data.node_groups.remove(old_group)
            for child in children:
                if child.users == 0:
                    bpy.data.node_groups.remove(child)
    modifier = source.modifiers.new('Selective Mirror Cleanup', 'NODES')
    modifier.node_group = build_group(source)
    modifier.show_in_editmode = True
    source.modifiers.move(list(source.modifiers).index(modifier), list(source.modifiers).index(mirror) + 1)
    for socket in modifier.node_group.interface.items_tree:
        if socket.item_type != 'SOCKET' or socket.in_out != 'INPUT':
            continue
        if socket.name.startswith('Mirror ') and socket.name[-1] in 'XYZ':
            axis = 'XYZ'.index(socket.name[-1])
            curve = modifier.driver_add('["' + socket.identifier + '"]')
            curve.driver.type = 'AVERAGE'
            variable = curve.driver.variables.new()
            variable.name = 'EnabledAxis'
            variable.type = 'SINGLE_PROP'
            variable.targets[0].id = source
            variable.targets[0].data_path = mirror.path_from_id('use_axis') + '[' + str(axis) + ']'
        elif socket.name == 'Use Mirror Object':
            modifier[socket.identifier] = mirror.mirror_object is not None
        elif socket.name == 'Mirror Object':
            modifier[socket.identifier] = mirror.mirror_object
    source.update_tag()
    return modifier


if __name__ == '__main__':
    modifier = setup(bpy.context.object)
    print('Installed', modifier.name, 'on', bpy.context.object.name)
