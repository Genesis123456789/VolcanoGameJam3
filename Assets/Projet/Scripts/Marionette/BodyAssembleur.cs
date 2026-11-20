using UnityEditor;
using UnityEngine;




[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BodyAssembleur : MonoBehaviour
{

    public void Assemblement()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] instances = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            var meshFilter = meshFilters[i];

            instances[i] = new CombineInstance
            {
                mesh = meshFilter.sharedMesh,
                transform = meshFilter.transform.localToWorldMatrix,
            };

            meshFilter.gameObject.SetActive(false);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(instances);
        gameObject.GetComponent<MeshFilter>().sharedMesh = combinedMesh;
        gameObject.SetActive(true);
    }

    public void Remplacer(MeshFilter targetMeshFilter, Mesh replacementMesh, int subMeshIndexToReplace = 0)
    {
        Mesh originalMesh = targetMeshFilter.sharedMesh;

        if (originalMesh == null || replacementMesh == null) return;
        if (subMeshIndexToReplace < 0 || subMeshIndexToReplace >= originalMesh.subMeshCount) return;

        int subMeshCount = originalMesh.subMeshCount;
        CombineInstance[] combine = new CombineInstance[subMeshCount];

        for (int i = 0; i < subMeshCount; i++)
        {
            combine[i].transform = Matrix4x4.identity;

            if (i == subMeshIndexToReplace)
            {
                // On insère le nouveau mesh à l'index voulu
                combine[i].mesh = replacementMesh;
            }
            else
            {
                // On extrait le sub-mesh existant sous forme de Mesh indépendant
                combine[i].mesh = ExtractSubMesh(originalMesh, i);
            }
        }

        // Création du nouveau mesh composé
        Mesh newCombinedMesh = new Mesh();
        newCombinedMesh.name = originalMesh.name + "_Modified";

        // Le deuxième paramètre 'false' est crucial : il indique de garder les sub-meshes séparés
        newCombinedMesh.CombineMeshes(combine, false, false);

        targetMeshFilter.mesh = newCombinedMesh;
    }






    // Méthode utilitaire pour isoler un sub-mesh précis
    private Mesh ExtractSubMesh(Mesh mesh, int subMeshIndex)
    {
        Mesh subMesh = new Mesh();
        subMesh.vertices = mesh.vertices;
        subMesh.normals = mesh.normals;
        subMesh.uv = mesh.uv;
        subMesh.tangents = mesh.tangents;
        subMesh.colors = mesh.colors;

        // On ne copie que les triangles du sub-mesh ciblé
        subMesh.SetTriangles(mesh.GetTriangles(subMeshIndex), 0);
        subMesh.RecalculateBounds();

        return subMesh;
    }
}
