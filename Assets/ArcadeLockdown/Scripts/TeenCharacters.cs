using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcadeLockdown
{
    public static partial class ArcadeWorldBuilder
    {
        public static void ReplacePlayerCharacter(ThirdPersonController controller, bool female)
        {
            if (controller == null) return;
            Transform old = controller.visualRoot;
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
            Transform visual = PartRoot(female ? "MAYA - female character" : "LEO - male character",controller.transform,Vector3.zero);
            controller.visualRoot = visual;
            controller.characterAnimator = null;
            controller.importedCharacterAnimator = null;
            controller.teenCharacterAnimator = CreateTeenCharacter(visual, female);
            SetLayerRecursively(visual.gameObject,2);
        }

        public static TeenCharacterAnimator CreateTeenCharacter(Transform visual, bool female)
        {
            string name=female?"Maya":"Leo";
            GameObject prefab=Resources.Load<GameObject>("Characters/Universal/"+name);
            if(prefab==null) throw new System.InvalidOperationException("Missing v5 character: "+name+". Import all package assets.");
            GameObject model=Object.Instantiate(prefab,visual);
            model.name=name+" - textured humanoid";
            model.transform.localPosition=Vector3.zero;
            model.transform.localRotation=Quaternion.identity;
            model.transform.localScale=Vector3.one;
            Animator animator=model.GetComponent<Animator>();
            if(animator==null)animator=model.AddComponent<Animator>();
            animator.enabled=false;
            foreach(SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] mats=skin.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    string lower=mats[i].name.ToLowerInvariant();
                    bool brow=lower.Contains("brow")||skin.name.ToLowerInvariant().Contains("brow");
                    string map=brow?"Hair1":lower.Contains("eye")?"Eyes":lower.Contains("hair")?"Hair1":female?"FemaleSkin":"MaleSkin";
                    mats[i]=MakeMaterial(name+" "+map,brow?new Color(.22f,.14f,.09f):Color.white,false,0,map=="Eyes"?.42f:.23f);
                    Texture texture=Resources.Load<Texture2D>("Characters/Universal/"+map);
                    mats[i].mainTexture=texture;
                    if(mats[i].HasProperty("_BaseMap"))mats[i].SetTexture("_BaseMap",texture);
                }
                skin.sharedMaterials=mats;
            }
            SkinnedMeshRenderer body=model.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
            Vector3[] restVertices=body.sharedMesh.vertices;
            Bounds bounds=new Bounds(body.transform.TransformPoint(restVertices[0]),Vector3.zero);
            foreach(Vector3 vertex in restVertices)bounds.Encapsulate(body.transform.TransformPoint(vertex));
            float height=bounds.size.y;
            if(height<.1f)throw new System.InvalidOperationException("Invalid character height.");
            AddCasualClothing(body,model.transform,bounds,female);
            GameObject hairPrefab=Resources.Load<GameObject>("Characters/Universal/"+name+"Hair");
            if(hairPrefab!=null)
            {
                GameObject hair=Object.Instantiate(hairPrefab,model.transform);
                hair.transform.localPosition=Vector3.zero;hair.transform.localRotation=Quaternion.identity;hair.transform.localScale=Vector3.one;
                RefineImportedRenderers(hair,0,.25f);
                foreach(Renderer r in hair.GetComponentsInChildren<Renderer>())
                {
                    Material mat=MakeMaterial(name+" natural brown hair",new Color(.27f,.19f,.13f),false,0,.25f);
                    Texture texture=Resources.Load<Texture2D>("Characters/Universal/"+(female?"Hair2":"Hair1"));
                    mat.mainTexture=texture;if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",texture);r.sharedMaterial=mat;
                }
                Transform head=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Head");
                if(head!=null)
                {
                    // Separate hairstyle FBXs face the opposite axis to the full-body FBXs.
                    hair.transform.RotateAround(head.position,Vector3.up,180f);
                    hair.transform.SetParent(head,true);
                }
            }
            // Adapt the freely licensed adult base to slimmer, late-teen-inspired proportions.
            // Fully clothed, with a relaxed crew-neck top, long trousers and shoes.
            float target=female?1.67f:1.74f;
            float scale=target/height;
            model.transform.localScale=new Vector3(scale*.91f,scale,scale*.96f);
            model.transform.localPosition=Vector3.up * (-(bounds.min.y-visual.position.y)*scale);
            model.transform.localRotation=Quaternion.Euler(0,180,0);
            animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Characters/Universal/TeenLocomotion");
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.enabled=true;
            animator.Rebind();animator.Update(0);
            TeenCharacterAnimator driver=visual.gameObject.AddComponent<TeenCharacterAnimator>();driver.animator=animator;
            driver.SetMovement(0,false);
            return driver;
        }

        private static void AddCasualClothing(SkinnedMeshRenderer body,Transform root,Bounds worldBounds,bool female)
        {
            Mesh original=body.sharedMesh;
            Vector3[] vertices=original.vertices;
            Vector3[] normals=original.normals;
            var groups=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
            Vector3[] adjusted=(Vector3[])vertices.Clone();
            Vector2[] uv=(Vector2[])original.uv.Clone();
            Vector3[] points=new Vector3[vertices.Length];
            float h=worldBounds.size.y;
            for(int i=0;i<vertices.Length;i++) points[i]=body.transform.TransformPoint(vertices[i])-new Vector3(worldBounds.center.x,worldBounds.min.y,worldBounds.center.z);
            int[] indices=original.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                Vector3 p=(points[indices[i]]+points[indices[i+1]]+points[indices[i+2]])/3f;
                float y=p.y/h, x=Mathf.Abs(p.x)/h;
                // 0: exposed face/hands/neck; 1: long trousers; 2: crew-neck short-sleeve shirt; 3: shoes.
                int group=y<.075f?3:y<.535f?1:y<.855f&&x<.245f?2:0;
                groups[group].Add(indices[i]);groups[group].Add(indices[i+1]);groups[group].Add(indices[i+2]);
            }
            var used=new HashSet<int>();
            for(int group=1;group<4;group++)foreach(int i in groups[group])
            {
                if(!used.Add(i))continue;
                Vector3 p=points[i];
                // Looser silhouette: removes skin anatomy detail under clothes, no separate floating rigid clothing.
                float offset=group==2?.008f:group==1?.012f:.014f;
                Vector3 n=body.transform.TransformDirection(normals[i]).normalized;
                Vector3 world=body.transform.TransformPoint(vertices[i])+n*(h*offset);
                if(group==2 && Mathf.Abs(p.x)<h*.125f && p.y<h*.77f)
                {
                    float side=Mathf.Sign(p.z);
                    world.z=worldBounds.center.z+side*Mathf.Max(Mathf.Abs(world.z-worldBounds.center.z),h*.076f);
                }
                adjusted[i]=body.transform.InverseTransformPoint(world);
                uv[i]=new Vector2(p.x*5,p.y*5);
            }
            Mesh dressed=Object.Instantiate(original);dressed.name=(female?"Maya":"Leo")+" dressed skinned mesh";
            dressed.vertices=adjusted;dressed.uv=uv;dressed.subMeshCount=4;
            // Keep the authored upper-arm sleeve surface, but remove covered torso
            // triangles so they cannot poke through the separately skinned shirt.
            var sleeves=new List<int>();
            for(int i=0;i<groups[2].Count;i+=3)
            {
                int a=groups[2][i],b=groups[2][i+1],c=groups[2][i+2];
                Vector3 center=(points[a]+points[b]+points[c])/3f;
                if(Mathf.Abs(center.x)>h*.13f&&center.y>h*.65f){sleeves.Add(a);sleeves.Add(b);sleeves.Add(c);}
            }
            for(int i=0;i<4;i++)dressed.SetTriangles(i==3?new List<int>():i==2?sleeves:groups[i],i);
            dressed.RecalculateNormals();dressed.RecalculateBounds();dressed.RecalculateTangents();
            Material skin=body.sharedMaterial;
            Material denim=MakeMaterial("Indigo denim trousers",new Color(.085f,.13f,.19f),false,0,.17f);
            Material shirt=MakeMaterial(female?"Maya sage crew neck":"Leo navy crew neck",female?new Color(.26f,.40f,.35f):new Color(.11f,.19f,.30f),false,0,.15f);
            Material shoes=MakeMaterial("Off-white casual sneakers",new Color(.69f,.70f,.66f),false,0,.24f);
            body.sharedMesh=dressed;body.sharedMaterials=new[]{skin,denim,shirt,shoes};
            body.localBounds=dressed.bounds;
            BuildSkinnedShirt(body,original,points,worldBounds,shirt);
            BuildCharacterShoes(body,points,worldBounds);
        }

        private static void BuildSkinnedShirt(SkinnedMeshRenderer body,Mesh source,Vector3[] sourcePoints,Bounds bounds,Material material)
        {
            // Smooth, purpose-built garment topology; weights transferred from the underlying rigged body.
            var v=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var tris=new List<int>();
            Vector3 origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);float h=bounds.size.y;
            int pelvis=System.Array.FindIndex(body.bones,b=>b.name=="pelvis");
            int spine1=System.Array.FindIndex(body.bones,b=>b.name=="spine_01");
            int spine2=System.Array.FindIndex(body.bones,b=>b.name=="spine_02");
            int spine3=System.Array.FindIndex(body.bones,b=>b.name=="spine_03");
            BoneWeight currentWeight=Weight(pelvis,pelvis,0);
            System.Action<Vector3,float,float> add=(world,u,t)=>
            {
                v.Add(body.transform.InverseTransformPoint(world));uv.Add(new Vector2(u,t));
                weights.Add(currentWeight);
            };
            const int sides=48;
            float[] heights={.49f,.505f,.55f,.60f,.65f,.70f,.75f,.79f,.82f,.84f,.863f};
            float[] rx={.148f,.148f,.145f,.135f,.125f,.132f,.142f,.144f,.133f,.087f,.045f};
            float[] rz={.115f,.115f,.110f,.098f,.095f,.098f,.104f,.100f,.091f,.057f,.040f};
            for(int j=0;j<heights.Length;j++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides;
                float y=heights[j];
                currentWeight=y<.59f?Weight(pelvis,spine1,Mathf.InverseLerp(.51f,.59f,y)):
                    y<.7f?Weight(spine1,spine2,Mathf.InverseLerp(.59f,.7f,y)):Weight(spine2,spine3,Mathf.InverseLerp(.7f,.78f,y));
                add(origin+new Vector3(Mathf.Cos(a)*rx[j]*h,heights[j]*h,-Mathf.Sin(a)*rz[j]*h),i/(float)sides,j/(float)(heights.Length-1));
            }
            Stitch(tris,0,heights.Length,sides);
            Mesh mesh=new Mesh{name="Smooth crew-neck shirt with short sleeves"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();mesh.bindposes=source.bindposes;mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
            GameObject garment=new GameObject("Relaxed crew-neck shirt - skinned cloth");garment.transform.SetParent(body.transform,false);
            SkinnedMeshRenderer renderer=garment.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=body.bones;renderer.rootBone=body.rootBone;renderer.sharedMaterial=material;renderer.localBounds=body.localBounds;
            if(material.HasProperty("_Cull"))material.SetFloat("_Cull",0);
        }
        private static BoneWeight Weight(int first,int second,float blend)
        {
            return new BoneWeight{boneIndex0=Mathf.Max(0,first),boneIndex1=Mathf.Max(0,second),weight0=1-blend,weight1=blend};
        }
        private static void BuildCharacterShoes(SkinnedMeshRenderer body,Vector3[] points,Bounds bounds)
        {
            Vector3 origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);float h=bounds.size.y;
            Material canvas=MakeMaterial("Sneaker canvas",new Color(.28f,.30f,.32f),false,0,.18f);
            Material sole=MakeMaterial("Sneaker rubber sole",new Color(.77f,.76f,.70f),false,0,.20f);
            for(int side=-1;side<=1;side+=2)
            {
                Vector3[] footPoints=points.Where(p=>p.y<h*.075f&&Mathf.Sign(p.x)==side).ToArray();
                if(footPoints.Length==0)continue;
                Bounds foot=new Bounds(footPoints[0],Vector3.zero);foreach(Vector3 p in footPoints)foot.Encapsulate(p);
                Transform bone=body.bones.Where(b=>b.name=="foot_l"||b.name=="foot_r").OrderBy(b=>Mathf.Abs(b.position.x-(origin.x+foot.center.x))).First();
                GameObject shoe=new GameObject("Canvas sneaker");shoe.transform.position=origin+new Vector3(foot.center.x,h*.029f,foot.center.z);
                shoe.transform.rotation=Quaternion.identity;
                Vector3 size=new Vector3(Mathf.Max(foot.size.x+h*.018f,h*.061f),h*.043f,foot.size.z+h*.03f);
                DetailBox("Rounded canvas upper",shoe.transform,Vector3.zero,size,canvas,h*.018f);
                DetailBox("Rubber sole",shoe.transform,new Vector3(0,-h*.017f,0),new Vector3(size.x+h*.004f,h*.016f,size.z+h*.004f),sole,h*.007f);
                for(int i=0;i<4;i++)Rod("Cotton lace",shoe.transform,new Vector3(-size.x*.24f,h*.025f,-size.z*.08f+i*h*.013f),new Vector3(size.x*.24f,h*.025f,-size.z*.08f+i*h*.013f),h*.0022f,sole);
                shoe.transform.SetParent(bone,true);
            }
        }
        private static void Stitch(List<int> indices,int start,int rings,int sides)
        {
            for(int j=0;j<rings-1;j++)for(int i=0;i<sides;i++)
            {
                int a=start+j*(sides+1)+i,b=a+sides+1;
                indices.Add(a);indices.Add(a+1);indices.Add(b);indices.Add(a+1);indices.Add(b+1);indices.Add(b);
            }
        }
    }
}
