"""Generate the checked-in car mesh and flattened scene backdrop.

Run this script with Blender so that the mesh export uses Blender's evaluated
corner normals and triangulation:

    blender --background --factory-startup --disable-autoexec Blender/Car.blend \
        --python-exit-code 1 --python tools/GenerateAssets.py -- \
        --car-output src/generated/CarMesh.generated.h \
        --background-output assets/SceneBackground.png

The normal game build consumes the generated files and does not require
Blender or Python.
"""

from __future__ import annotations

import argparse
import binascii
import hashlib
import math
from pathlib import Path
import struct
import sys
import zlib

import bpy
import numpy as np


EXPECTED_MATERIALS = (
    "SlopeCarAxles",
    "SlopeCarBodyBlue",
    "SlopeCarCabinBlue",
    "SlopeCarHeadlights",
    "SlopeCarWheels",
)

BACKGROUND_WIDTH = 5120
BACKGROUND_HEIGHT = 1440
BACKGROUND_ASPECT = 32.0 / 9.0


def _script_arguments() -> argparse.Namespace:
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--car-output",
        type=Path,
        default=Path("src/generated/CarMesh.generated.h"),
    )
    parser.add_argument(
        "--background-output",
        type=Path,
        default=Path("assets/SceneBackground.png"),
    )
    return parser.parse_args(arguments)


def _float32(value: float) -> float:
    rounded = struct.unpack("<f", struct.pack("<f", value))[0]
    # Keep the generated literals, deduplication keys, and binary fingerprint
    # byte-for-byte consistent: C++ prints both signs of zero as 0.0f.
    return 0.0 if rounded == 0.0 else rounded


def _cpp_float(value: float) -> str:
    value = _float32(value)
    result = format(value, ".9g")
    if "." not in result and "e" not in result.lower():
        result += ".0"
    return result + "f"


def _engine_vector(vector) -> tuple[float, float, float]:
    # Blender is Z-up. The renderer is Y-up and right-handed.
    return (_float32(vector.x), _float32(vector.z), _float32(-vector.y))


def _generate_car_header(output_path: Path) -> tuple[int, int, str]:
    objects = [item for item in bpy.context.scene.objects if item.type == "MESH"]
    if len(objects) != 1 or objects[0].name != "Car":
        raise RuntimeError("Car.blend must contain exactly one mesh object named 'Car'.")

    source_object = objects[0]
    material_names = tuple(slot.material.name for slot in source_object.material_slots)
    if material_names != EXPECTED_MATERIALS:
        raise RuntimeError(
            f"Unexpected material slots: {material_names!r}; expected {EXPECTED_MATERIALS!r}."
        )

    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = source_object.evaluated_get(dependency_graph)
    mesh = evaluated_object.to_mesh(preserve_all_data_layers=True, depsgraph=dependency_graph)

    try:
        mesh.calc_loop_triangles()
        world = evaluated_object.matrix_world
        normal_matrix = world.to_3x3().inverted_safe().transposed()

        vertices: list[tuple[float, float, float, float, float, float, int]] = []
        indices: list[int] = []
        vertex_lookup: dict[bytes, int] = {}

        triangles = sorted(
            mesh.loop_triangles,
            key=lambda triangle: (
                mesh.polygons[triangle.polygon_index].material_index,
                triangle.polygon_index,
                tuple(triangle.loops),
            ),
        )

        for triangle in triangles:
            material_index = mesh.polygons[triangle.polygon_index].material_index
            if not 0 <= material_index < len(EXPECTED_MATERIALS):
                raise RuntimeError(f"Triangle has invalid material index {material_index}.")

            for loop_index in triangle.loops:
                loop = mesh.loops[loop_index]
                position = _engine_vector(world @ mesh.vertices[loop.vertex_index].co)
                normal = normal_matrix @ mesh.corner_normals[loop_index].vector
                normal.normalize()
                converted_normal = _engine_vector(normal)
                packed = struct.pack("<6fI", *position, *converted_normal, material_index)

                vertex_index = vertex_lookup.get(packed)
                if vertex_index is None:
                    vertex_index = len(vertices)
                    vertex_lookup[packed] = vertex_index
                    vertices.append((*position, *converted_normal, material_index))
                indices.append(vertex_index)

        if len(indices) != 5652:
            raise RuntimeError(f"Expected 5,652 indices, generated {len(indices):,}.")
        if len(vertices) > 65535:
            raise RuntimeError("Generated car mesh no longer fits 16-bit indices.")
        if min(vertex[1] for vertex in vertices) < -1.0e-5:
            raise RuntimeError("Converted car mesh unexpectedly extends below engine Y=0.")

        binary_fingerprint = hashlib.sha256()
        for vertex in vertices:
            binary_fingerprint.update(struct.pack("<6fI", *vertex))
        for index in indices:
            binary_fingerprint.update(struct.pack("<H", index))
        digest = binary_fingerprint.hexdigest()

        lines = [
            "// Generated by tools/GenerateAssets.py from Blender/Car.blend.",
            "// Do not edit this file by hand.",
            f"// Vertex/index SHA-256: {digest}",
            "#pragma once",
            "",
            "#include <cstdint>",
            "",
            "namespace GeneratedCarMesh",
            "{",
            "struct Vertex",
            "{",
            "    float positionX;",
            "    float positionY;",
            "    float positionZ;",
            "    float normalX;",
            "    float normalY;",
            "    float normalZ;",
            "    std::uint32_t materialIndex;",
            "};",
            "",
            f"inline constexpr std::uint32_t MaterialCount = {len(EXPECTED_MATERIALS)}u;",
            f"inline constexpr Vertex Vertices[{len(vertices)}] = {{",
        ]

        for vertex in vertices:
            values = ", ".join(_cpp_float(component) for component in vertex[:6])
            lines.append(f"    {{{values}, {vertex[6]}u}},")

        lines.extend(
            [
                "};",
                "",
                f"inline constexpr std::uint16_t Indices[{len(indices)}] = {{",
            ]
        )
        for offset in range(0, len(indices), 16):
            row = ", ".join(f"{index}u" for index in indices[offset : offset + 16])
            lines.append(f"    {row},")
        lines.extend(["};", "}", ""])

        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text("\n".join(lines), encoding="utf-8", newline="\n")
        return len(vertices), len(indices), digest
    finally:
        evaluated_object.to_mesh_clear()


def _normalize(vector: np.ndarray) -> np.ndarray:
    return vector / np.linalg.norm(vector)


def _view_matrix(eye: np.ndarray, target: np.ndarray, up: np.ndarray) -> np.ndarray:
    backward = _normalize(eye - target)
    right = _normalize(np.cross(up, backward))
    camera_up = np.cross(backward, right)
    return np.array(
        [
            [right[0], camera_up[0], backward[0], 0.0],
            [right[1], camera_up[1], backward[1], 0.0],
            [right[2], camera_up[2], backward[2], 0.0],
            [-np.dot(right, eye), -np.dot(camera_up, eye), -np.dot(backward, eye), 1.0],
        ],
        dtype=np.float64,
    )


def _orthographic_matrix(width: float, height: float, near: float, far: float) -> np.ndarray:
    return np.array(
        [
            [2.0 / width, 0.0, 0.0, 0.0],
            [0.0, 2.0 / height, 0.0, 0.0],
            [0.0, 0.0, 1.0 / (near - far), 0.0],
            [0.0, 0.0, near / (near - far), 1.0],
        ],
        dtype=np.float64,
    )


def _project(vertices: np.ndarray, view_projection: np.ndarray) -> np.ndarray:
    homogeneous = np.concatenate(
        (vertices.astype(np.float64), np.ones((len(vertices), 1), dtype=np.float64)), axis=1
    )
    clip = homogeneous @ view_projection
    normalized = clip[:, :3] / clip[:, 3, np.newaxis]
    result = np.empty_like(normalized)
    result[:, 0] = (normalized[:, 0] * 0.5 + 0.5) * BACKGROUND_WIDTH
    result[:, 1] = (0.5 - normalized[:, 1] * 0.5) * BACKGROUND_HEIGHT
    result[:, 2] = normalized[:, 2]
    return result


def _rasterize_triangle(
    image: np.ndarray,
    depth_buffer: np.ndarray,
    points: np.ndarray,
    color: np.ndarray,
) -> None:
    minimum_x = max(0, int(math.floor(float(np.min(points[:, 0])))))
    maximum_x = min(BACKGROUND_WIDTH - 1, int(math.ceil(float(np.max(points[:, 0])))))
    minimum_y = max(0, int(math.floor(float(np.min(points[:, 1])))))
    maximum_y = min(BACKGROUND_HEIGHT - 1, int(math.ceil(float(np.max(points[:, 1])))))
    if minimum_x > maximum_x or minimum_y > maximum_y:
        return

    x0, y0, z0 = points[0]
    x1, y1, z1 = points[1]
    x2, y2, z2 = points[2]
    denominator = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
    if abs(denominator) < 1.0e-12:
        return

    sample_x = np.arange(minimum_x, maximum_x + 1, dtype=np.float64)[np.newaxis, :] + 0.5
    sample_y = np.arange(minimum_y, maximum_y + 1, dtype=np.float64)[:, np.newaxis] + 0.5
    weight0 = ((y1 - y2) * (sample_x - x2) + (x2 - x1) * (sample_y - y2)) / denominator
    weight1 = ((y2 - y0) * (sample_x - x2) + (x0 - x2) * (sample_y - y2)) / denominator
    weight2 = 1.0 - weight0 - weight1
    inside = (weight0 >= -1.0e-10) & (weight1 >= -1.0e-10) & (weight2 >= -1.0e-10)
    interpolated_depth = weight0 * z0 + weight1 * z1 + weight2 * z2

    depth_region = depth_buffer[minimum_y : maximum_y + 1, minimum_x : maximum_x + 1]
    visible = inside & (interpolated_depth >= 0.0) & (interpolated_depth <= 1.0)
    visible &= interpolated_depth < depth_region
    if not np.any(visible):
        return

    depth_region[visible] = interpolated_depth[visible]
    image_region = image[minimum_y : maximum_y + 1, minimum_x : maximum_x + 1]
    image_region[visible] = color


def _lit_color(base_color: tuple[float, float, float], normal: np.ndarray) -> np.ndarray:
    light = _normalize(np.array((0.5, 0.70710677, 0.5), dtype=np.float64))
    diffuse = max(0.0, float(np.dot(normal, light)))
    brightness = 0.28 + 0.72 * diffuse
    encoded = np.rint(np.clip(np.array(base_color) * brightness, 0.0, 1.0) * 255.0)
    return encoded.astype(np.uint8)


def _png_chunk(chunk_type: bytes, data: bytes) -> bytes:
    checksum = binascii.crc32(chunk_type)
    checksum = binascii.crc32(data, checksum) & 0xFFFFFFFF
    return struct.pack(">I", len(data)) + chunk_type + data + struct.pack(">I", checksum)


def _write_png(output_path: Path, image: np.ndarray) -> None:
    scanlines = b"".join(b"\x00" + image[row].tobytes() for row in range(image.shape[0]))
    header = struct.pack(">IIBBBBB", image.shape[1], image.shape[0], 8, 2, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n"
    png += _png_chunk(b"IHDR", header)
    png += _png_chunk(b"IDAT", zlib.compress(scanlines, level=9))
    png += _png_chunk(b"IEND", b"")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(png)


def _generate_background(output_path: Path) -> str:
    image = np.empty((BACKGROUND_HEIGHT, BACKGROUND_WIDTH, 3), dtype=np.uint8)
    image[:, :] = np.rint(np.array((0.3, 0.3, 0.3)) * 255.0).astype(np.uint8)
    depth_buffer = np.full((BACKGROUND_HEIGHT, BACKGROUND_WIDTH), np.inf, dtype=np.float64)

    eye = np.array((1.6889755, 3.6863865, 2.9253915), dtype=np.float64)
    target = np.zeros(3, dtype=np.float64)
    up = np.array((-0.36863866, 0.6755902, -0.63850087), dtype=np.float64)
    view = _view_matrix(eye, target, up)
    projection = _orthographic_matrix(5.0 * BACKGROUND_ASPECT, 5.0, 1.0, 20.0)
    view_projection = view @ projection

    ground_vertices = np.array(
        ((-5.0, 0.0, -2.5), (-5.0, 0.0, 2.5), (5.0, 0.0, 2.5), (5.0, 0.0, -2.5)),
        dtype=np.float64,
    )
    projected_ground = _project(ground_vertices, view_projection)
    ground_color = _lit_color((0.08900002, 0.89, 0.48950002), np.array((0.0, 1.0, 0.0)))
    for triangle in ((0, 1, 2), (0, 2, 3)):
        _rasterize_triangle(image, depth_buffer, projected_ground[list(triangle)], ground_color)

    half = 0.5
    cube_faces = (
        ((0.0, 0.0, 1.0), ((-half, -half, half), (half, -half, half), (half, half, half), (-half, half, half))),
        ((0.0, 0.0, -1.0), ((half, -half, -half), (-half, -half, -half), (-half, half, -half), (half, half, -half))),
        ((1.0, 0.0, 0.0), ((half, -half, half), (half, -half, -half), (half, half, -half), (half, half, half))),
        ((-1.0, 0.0, 0.0), ((-half, -half, -half), (-half, -half, half), (-half, half, half), (-half, half, -half))),
        ((0.0, 1.0, 0.0), ((-half, half, half), (half, half, half), (half, half, -half), (-half, half, -half))),
        ((0.0, -1.0, 0.0), ((-half, -half, -half), (half, -half, -half), (half, -half, half), (-half, -half, half))),
    )
    cube_translation = np.array((0.0, 0.5, -1.5), dtype=np.float64)
    for normal_values, corners in cube_faces:
        projected = _project(np.array(corners, dtype=np.float64) + cube_translation, view_projection)
        face_color = _lit_color(
            (0.88, 0.1672, 0.17907982), np.array(normal_values, dtype=np.float64)
        )
        for triangle in ((0, 1, 2), (0, 2, 3)):
            _rasterize_triangle(image, depth_buffer, projected[list(triangle)], face_color)

    _write_png(output_path, image)
    return hashlib.sha256(output_path.read_bytes()).hexdigest()


def main() -> None:
    arguments = _script_arguments()
    car_vertices, car_indices, car_digest = _generate_car_header(arguments.car_output)
    background_digest = _generate_background(arguments.background_output)
    print(
        f"Generated {arguments.car_output}: {car_vertices:,} vertices, "
        f"{car_indices:,} indices, SHA-256 {car_digest}"
    )
    print(
        f"Generated {arguments.background_output}: {BACKGROUND_WIDTH}x{BACKGROUND_HEIGHT}, "
        f"SHA-256 {background_digest}"
    )


if __name__ == "__main__":
    main()
