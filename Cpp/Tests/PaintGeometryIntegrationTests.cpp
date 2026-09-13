#include "SimplePaint/Geometry.h"
#include "UVSphere.h"
#include <iostream>

using SimplePaint::ValidateMesh;
template<class Function> void Throws(Function function)
{
    try { function(); } catch (const std::invalid_argument&) { return; }
    throw std::runtime_error("Invalid input did not throw std::invalid_argument");
}
int main()
{
    try
    {
        // Validate actual assets and minimum sphere resolution, then corrupt each
        // independently to prove invalid geometry is rejected before upload.
        const std::vector<std::uint32_t> carIndices(std::begin(GeneratedCarMesh::Indices),std::end(GeneratedCarMesh::Indices));
        ValidateMesh<GeneratedCarMesh::Vertex>(GeneratedCarMesh::Vertices,carIndices,5);
        auto mesh = UVSphere::Generate(3,2,0);
        ValidateMesh<GeneratedCarMesh::Vertex>(mesh.vertices,mesh.indices,1);
        auto bad = mesh;
        bad.vertices[0].normalY=0; Throws([&]{ValidateMesh<GeneratedCarMesh::Vertex>(bad.vertices,bad.indices,1);});
        bad=mesh; bad.indices[0]=9999; Throws([&]{ValidateMesh<GeneratedCarMesh::Vertex>(bad.vertices,bad.indices,1);});
        bad=mesh; bad.vertices[0].materialIndex=1; Throws([&]{ValidateMesh<GeneratedCarMesh::Vertex>(bad.vertices,bad.indices,2);});
        bad=mesh; bad.vertices[0].positionX=INFINITY; Throws([&]{ValidateMesh<GeneratedCarMesh::Vertex>(bad.vertices,bad.indices,1);});
        bad=mesh;
        bad.vertices[0].normalX=-bad.vertices[bad.indices[1]].normalX;
        bad.vertices[0].normalY=-bad.vertices[bad.indices[1]].normalY;
        bad.vertices[0].normalZ=-bad.vertices[bad.indices[1]].normalZ;
        Throws([&]{ValidateMesh<GeneratedCarMesh::Vertex>(bad.vertices,bad.indices,1);});
        // Paint rotation is constant across each primitive, including car seams.
        for (size_t i = 0; i < std::size(GeneratedCarMesh::Indices); i += 3)
        {
            const auto a = GeneratedCarMesh::Vertices[GeneratedCarMesh::Indices[i]].materialIndex;
            const auto b = GeneratedCarMesh::Vertices[GeneratedCarMesh::Indices[i + 1]].materialIndex;
            const auto c = GeneratedCarMesh::Vertices[GeneratedCarMesh::Indices[i + 2]].materialIndex;
            if (a != b || a != c) throw std::runtime_error("A car triangle mixes paint materials");
        }

        std::cout << "Game mesh paint validation passed.\n";
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
