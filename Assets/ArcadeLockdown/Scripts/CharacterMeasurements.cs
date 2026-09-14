using UnityEngine;

namespace ArcadeLockdown
{
    public static class CharacterMeasurements
    {
        // Measure the actual animated vertices; import bounds can include root motion.
        public static Bounds Measure(Transform relativeTo)
        {
            Bounds bounds = new Bounds();
            bool started = false;
            foreach (SkinnedMeshRenderer renderer in relativeTo.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh mesh = renderer.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                BoneWeight[] weights = mesh.boneWeights;
                Matrix4x4[] bindposes = mesh.bindposes;
                Transform[] bones = renderer.bones;
                Matrix4x4[] matrices = new Matrix4x4[bindposes.Length];
                for (int i=0;i<matrices.Length;i++)
                    matrices[i] = relativeTo.worldToLocalMatrix * bones[i].localToWorldMatrix * bindposes[i];
                for (int i=0;i<vertices.Length;i++)
                {
                    Vector3 vertex = vertices[i];
                    BoneWeight weight = weights[i];
                    Vector3 point = matrices[weight.boneIndex0].MultiplyPoint3x4(vertex) * weight.weight0
                                  + matrices[weight.boneIndex1].MultiplyPoint3x4(vertex) * weight.weight1
                                  + matrices[weight.boneIndex2].MultiplyPoint3x4(vertex) * weight.weight2
                                  + matrices[weight.boneIndex3].MultiplyPoint3x4(vertex) * weight.weight3;
                    if (!started) { bounds = new Bounds(point, Vector3.zero); started = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }
    }
}
