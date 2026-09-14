using UnityEngine;

namespace ArcadeLockdown
{
    public static partial class ArcadeWorldBuilder
    {
        private static void BuildDetailedAttractions(Transform parent)
        {
            BuildRealisticClaw(parent, new Vector3(9,0,5.4f), 90);
            BuildRealisticBasketball(parent, new Vector3(-8.7f,0,-2.8f), -90);
            BuildRealisticHockey(parent, new Vector3(-4.3f,0,-1.7f));
            BuildFreeStool(parent,new Vector3(-4.9f,0,5.1f));
            BuildFreeStool(parent,new Vector3(7.6f,0,5.1f));
        }

        private static void BuildPrizeWheel(Transform parent, Vector3 position, float yaw) => BuildRealisticWheel(parent,position,yaw);
        private static void BuildVendingMachine(Transform parent, Vector3 position, float yaw) => BuildRealisticVending(parent,position,yaw);

        private static void BuildFreeStool(Transform parent,Vector3 position)
        {
            GameObject prefab=Resources.Load<GameObject>("Models/BenjaArcade/ChairShort");
            if(prefab==null)return;
            Transform root=PartRoot("Arcade stool - CC0 BenjaTheMaker",parent,position);
            GameObject model=Object.Instantiate(prefab,root);
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            Renderer[] renderers=model.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds;
            foreach(Renderer renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float factor=.62f/Mathf.Max(.01f,bounds.size.y);
            model.transform.localScale*=factor;
            model.transform.localPosition=new Vector3(root.position.x-bounds.center.x,root.position.y-bounds.min.y,root.position.z-bounds.center.z)*factor;
            RefineImportedRenderers(model,.22f,.4f);
            Texture2D texture=Resources.Load<Texture2D>("Models/BenjaArcade/ChairTallTexture");
            foreach(Renderer renderer in renderers)foreach(Material material in renderer.sharedMaterials)
                if(material!=null&&texture!=null)SetMaterialTexture(material,"_BaseMap","_MainTex",texture);
            AddBoxCollider(root.gameObject,new Vector3(0,.31f,0),new Vector3(.45f,.62f,.45f));
        }

        private static void BuildRealisticClaw(Transform parent, Vector3 position, float yaw)
        {
            Transform root=PartRoot("Working claw machine - steel and glass",parent,position,yaw);
            Material enamel=MakeMaterial("Claw burgundy enamel",new Color(.26f,.055f,.09f),false,.3f,.5f);
            DetailBox("Steel lower cabinet",root,new Vector3(0,.40f,0),new Vector3(1.15f,.8f,.96f),enamel,.035f);
            DetailBox("Prize bed",root,new Vector3(0,.86f,0),new Vector3(1.12f,.08f,.94f),Materials["White"],.015f);
            DetailBox("Top light housing",root,new Vector3(0,2.03f,0),new Vector3(1.22f,.22f,1.02f),enamel,.025f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                DetailBox("Aluminum corner upright",root,new Vector3(x*.557f,1.43f,z*.453f),new Vector3(.047f,1.13f,.047f),Materials["Metal"],.009f);
            for(int side=-1;side<=1;side+=2)
                DetailBox("Side tempered glass",root,new Vector3(side*.55f,1.43f,0),new Vector3(.009f,1.08f,.86f),Materials["ClearGlass"],.002f);
            DetailBox("Front safety glass",root,new Vector3(0,1.43f,-.453f),new Vector3(1.07f,1.08f,.009f),Materials["ClearGlass"],.002f);
            DetailBox("Back mirror",root,new Vector3(0,1.43f,.45f),new Vector3(1.07f,1.08f,.01f),Materials["Metal"],.002f);
            for(int i=0;i<12;i++)
            {
                Vector3 p=new Vector3(-.36f+(i%4)*.24f,.98f+(i%2)*.045f,-.28f+(i/4)*.26f);
                Material prize=i%3==0?Materials["Orange"]:i%3==1?Materials["White"]:Materials["Blue"];
                Primitive("Prize capsule",PrimitiveType.Sphere,root,p,Vector3.one*.18f,prize,false);
            }
            Rod("Claw X rail",root,new Vector3(-.49f,1.87f,0),new Vector3(.49f,1.87f,0),.036f,Materials["Metal"]);
            Transform claw=PartRoot("Moving claw carriage",root,new Vector3(0,1.86f,0));
            DetailBox("Rail motor",claw,Vector3.zero,new Vector3(.14f,.08f,.14f),Materials["DarkMetal"],.015f);
            Rod("Hanging cable",claw,Vector3.down*.04f,Vector3.down*.25f,.012f,Materials["Black"]);
            Primitive("Claw hub",PrimitiveType.Cylinder,claw,Vector3.down*.29f,new Vector3(.09f,.045f,.09f),Materials["Metal"],false);
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI*2/3;
                Vector3 outwards=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Rod("Articulated claw finger",claw,Vector3.down*.3f+outwards*.02f,Vector3.down*.44f+outwards*.10f,.018f,Materials["Metal"]);
                Rod("Curved claw tip",claw,Vector3.down*.44f+outwards*.10f,Vector3.down*.48f+outwards*.04f,.018f,Materials["Metal"]);
            }
            DisplayMachineAnimator motion=root.gameObject.AddComponent<DisplayMachineAnimator>();
            motion.movingPart=claw; motion.motionStyle=DisplayMachineAnimator.MotionStyle.Slide; motion.axis=Vector3.right; motion.amplitude=.32f; motion.speed=.55f;
            DetailBox("Prize collection hatch",root,new Vector3(0,.31f,-.495f),new Vector3(.43f,.31f,.05f),Materials["Rubber"],.025f);
            CreateText("PRIZE DROP",root,new Vector3(0,.56f,-.49f),Quaternion.identity,.018f,Color.white,64);
            DetailBox("Control ledge",root,new Vector3(0,.8f,-.57f),new Vector3(.70f,.10f,.22f),Materials["DarkMetal"],.02f);
            Primitive("Claw joystick",PrimitiveType.Sphere,root,new Vector3(-.2f,.89f,-.58f),Vector3.one*.07f,Materials["Red"],false);
            Primitive("Drop button",PrimitiveType.Sphere,root,new Vector3(.2f,.865f,-.58f),new Vector3(.09f,.035f,.09f),Materials["Yellow"],false);
            CreateText("LUCKY CATCH",root,new Vector3(0,2.035f,-.52f),Quaternion.identity,.028f,Color.white,64);
            CreateAccentLight(root,"Glass cabinet light",new Vector3(0,1.85f,-.15f),new Color(.72f,.84f,1),2.4f,1.2f);
            AddBoxCollider(root.gameObject,new Vector3(0,1.07f,-.04f),new Vector3(1.23f,2.14f,1.16f));
            AddInteraction(root.gameObject,InteractionKind.Information,"Inspect claw machine","The motor carries the steel claw along its rail above the capsule prizes.");
        }

        private static void BuildRealisticHockey(Transform parent,Vector3 position)
        {
            Transform root=PartRoot("Air hockey table - stainless rails",parent,position);
            DetailBox("Timber table cabinet",root,new Vector3(0,.69f,0),new Vector3(1.38f,.30f,2.28f),Materials["Wood"],.07f);
            DetailBox("Perforated white playfield",root,new Vector3(0,.86f,0),new Vector3(1.24f,.035f,2.12f),Materials["White"],.035f);
            Material blue=MakeMaterial("Hockey blue markings",new Color(.03f,.24f,.45f),false,0,.4f);
            for(int x=-1;x<=1;x+=2)
                DetailBox("Side aluminum cushion",root,new Vector3(x*.66f,.92f,0),new Vector3(.085f,.13f,2.3f),Materials["Metal"],.025f);
            for(int z=-1;z<=1;z+=2)
            {
                DetailBox("End cushion",root,new Vector3(0,.92f,z*1.11f),new Vector3(1.36f,.13f,.085f),Materials["Metal"],.025f);
                DetailBox("Goal opening",root,new Vector3(0,.89f,z*1.158f),new Vector3(.40f,.06f,.012f),Materials["Black"],.01f);
                DetailBox("Puck return chute",root,new Vector3(0,.65f,z*1.157f),new Vector3(.42f,.12f,.018f),Materials["Rubber"],.018f);
                for(int x=-1;x<=1;x+=2)
                    DetailBox("Splayed steel leg",root,new Vector3(x*.51f,.31f,z*.87f),new Vector3(.12f,.62f,.12f),Materials["DarkMetal"],.025f);
            }
            Cube("Centre court line",root,new Vector3(0,.881f,0),new Vector3(1.24f,.002f,.014f),blue,false);
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI/20, b=(i+1)*Mathf.PI/20;
                Rod("Centre court circle",root,new Vector3(Mathf.Cos(a)*.25f,.883f,Mathf.Sin(a)*.25f),new Vector3(Mathf.Cos(b)*.25f,.883f,Mathf.Sin(b)*.25f),.008f,blue);
            }
            for(int x=0;x<9;x++)for(int z=0;z<15;z++)
                Cube("Air perforation",root,new Vector3(-.48f+x*.12f,.883f,-.87f+z*.12f),new Vector3(.005f,.001f,.005f),Materials["DarkMetal"],false);
            Transform puck=Primitive("Moving red puck",PrimitiveType.Cylinder,root,new Vector3(0,.906f,0),new Vector3(.085f,.012f,.085f),Materials["Red"],false).transform;
            DisplayMachineAnimator motion=root.gameObject.AddComponent<DisplayMachineAnimator>();
            motion.movingPart=puck; motion.motionStyle=DisplayMachineAnimator.MotionStyle.Orbit; motion.amplitude=.4f; motion.speed=1.5f;
            for(int z=-1;z<=1;z+=2)
            {
                Primitive("Player paddle base",PrimitiveType.Cylinder,root,new Vector3(z*.18f,.905f,z*.74f),new Vector3(.14f,.018f,.14f),blue,false);
                Primitive("Player paddle handle",PrimitiveType.Cylinder,root,new Vector3(z*.18f,.96f,z*.74f),new Vector3(.055f,.055f,.055f),blue,false);
            }
            AddBoxCollider(root.gameObject,new Vector3(0,.49f,0),new Vector3(1.4f,.98f,2.34f));
            AddInteraction(root.gameObject,InteractionKind.Information,"Inspect air hockey","The puck glides across the perforated playfield.");
        }

        private static void BuildRealisticBasketball(Transform parent,Vector3 position,float yaw)
        {
            Transform root=PartRoot("Basketball challenge - cage and return ramp",parent,position,yaw);
            Material paint=MakeMaterial("Basketball cage enamel",new Color(.32f,.1f,.035f),false,.4f,.45f);
            DetailBox("Ball return cabinet",root,new Vector3(0,.47f,0),new Vector3(1.42f,.94f,2.1f),Materials["CabinetWood"],.035f);
            Transform ramp=DetailBox("Sloping ball return",root,new Vector3(0,1.02f,0),new Vector3(1.30f,.065f,2.06f),Materials["DarkMetal"],.016f).transform;
            ramp.localRotation=Quaternion.Euler(-12f,0,0);
            for(int x=-1;x<=1;x+=2)
            {
                Rod("Cage corner post",root,new Vector3(x*.70f,.90f,.95f),new Vector3(x*.70f,2.65f,.95f),.05f,paint);
                Rod("Side safety rail",root,new Vector3(x*.70f,1.15f,-1),new Vector3(x*.70f,2.45f,.95f),.035f,paint);
                for(int i=0;i<9;i++)
                {
                    float z=-.85f+i*.21f, height=Mathf.Lerp(1.24f,2.37f,i/8f);
                    Rod("Wire safety mesh",root,new Vector3(x*.70f,1.02f,z),new Vector3(x*.70f,height,z),.008f,Materials["Metal"]);
                }
            }
            DetailBox("Basketball backboard",root,new Vector3(0,2.25f,.98f),new Vector3(1.35f,.75f,.065f),Materials["White"],.025f);
            DetailBox("Backboard target",root,new Vector3(0,2.17f,.941f),new Vector3(.49f,.34f,.005f),paint,.009f);
            DetailBox("Target center",root,new Vector3(0,2.17f,.935f),new Vector3(.44f,.29f,.005f),Materials["White"],.006f);
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                Rod("Steel basket rim",root,new Vector3(Mathf.Cos(a)*.22f,1.97f,.62f+Mathf.Sin(a)*.22f),new Vector3(Mathf.Cos(b)*.22f,1.97f,.62f+Mathf.Sin(b)*.22f),.022f,paint);
                if(i%2==0)Rod("Basket net cord",root,new Vector3(Mathf.Cos(a)*.22f,1.95f,.62f+Mathf.Sin(a)*.22f),new Vector3(Mathf.Cos(a+.4f)*.11f,1.68f,.62f+Mathf.Sin(a+.4f)*.11f),.006f,Materials["White"]);
            }
            Transform ball=Primitive("Animated basketball",PrimitiveType.Sphere,root,new Vector3(0,1.12f,-.76f),Vector3.one*.24f,paint,false).transform;
            DisplayMachineAnimator motion=root.gameObject.AddComponent<DisplayMachineAnimator>();
            motion.movingPart=ball; motion.motionStyle=DisplayMachineAnimator.MotionStyle.Shot; motion.destination=new Vector3(0,1.88f,.62f); motion.amplitude=.94f; motion.speed=.55f;
            CreateText("HOOP CHALLENGE",root,new Vector3(0,.65f,-1.075f),Quaternion.identity,.025f,Color.white,64);
            AddBoxCollider(root.gameObject,new Vector3(0,1.34f,0),new Vector3(1.46f,2.68f,2.16f));
            AddInteraction(root.gameObject,InteractionKind.Information,"Watch basketball","The demo ball arcs toward the hoop and rolls down the return ramp.");
        }

        private static void BuildRealisticWheel(Transform parent,Vector3 position,float yaw)
        {
            Transform root=PartRoot("Motorized prize wheel",parent,position,yaw);
            DetailBox("Prize wheel base",root,new Vector3(0,.43f,0),new Vector3(1.0f,.86f,.65f),Materials["CabinetWood"],.04f);
            DetailBox("Steel wheel support",root,new Vector3(0,1.1f,.15f),new Vector3(.30f,1.36f,.19f),Materials["DarkMetal"],.02f);
            Transform rotor=PartRoot("Moving printed prize wheel",root,new Vector3(0,1.39f,-.16f));
            Primitive("Wheel metal rim",PrimitiveType.Cylinder,rotor,Vector3.zero,new Vector3(1.15f,.044f,1.15f),Materials["Brass"],false).transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<12;i++)
            {
                Material color=MakeMaterial("Wheel segment "+i,i%2==0?new Color(.28f,.055f,.08f):new Color(.85f,.75f,.45f),false,0,.38f);
                Mesh mesh=new Mesh {name="Prize wheel wedge"};
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                mesh.vertices=new[]{new Vector3(0,0,-.047f),new Vector3(Mathf.Cos(b)*.54f,Mathf.Sin(b)*.54f,-.047f),new Vector3(Mathf.Cos(a)*.54f,Mathf.Sin(a)*.54f,-.047f)};
                mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();
                GameObject wedge=new GameObject("Printed wheel segment");wedge.transform.SetParent(rotor,false);
                wedge.AddComponent<MeshFilter>().sharedMesh=mesh;wedge.AddComponent<MeshRenderer>().sharedMaterial=color;
                float middle=(a+b)*.5f;
                CreateText((5+i*5).ToString(),rotor,new Vector3(Mathf.Cos(middle)*.40f,Mathf.Sin(middle)*.40f,-.051f),Quaternion.identity,.023f,i%2==0?Color.white:Color.black,64);
            }
            Primitive("Wheel center hub",PrimitiveType.Sphere,rotor,new Vector3(0,0,-.08f),new Vector3(.17f,.17f,.08f),Materials["Metal"],false);
            DetailBox("Fixed prize pointer",root,new Vector3(0,1.985f,-.24f),new Vector3(.05f,.15f,.06f),Materials["White"],.012f);
            DisplayMachineAnimator motion=root.gameObject.AddComponent<DisplayMachineAnimator>();
            motion.movingPart=rotor;motion.motionStyle=DisplayMachineAnimator.MotionStyle.Spin;motion.axis=Vector3.forward;motion.speed=.22f;
            CreateText("PRIZE WHEEL",root,new Vector3(0,.58f,-.336f),Quaternion.identity,.029f,Color.white,64);
            AddBoxCollider(root.gameObject,new Vector3(0,1.02f,0),new Vector3(1.17f,2.04f,.72f));
            AddInteraction(root.gameObject,InteractionKind.Information,"Watch prize wheel","The motor spins the printed prize wheel.");
        }

        private static void BuildRealisticVending(Transform parent,Vector3 position,float yaw)
        {
            Transform root=PartRoot("Vending machine - refrigerated drinks",parent,position,yaw);
            Material paint=MakeMaterial("Vending machine enamel",new Color(.09f,.15f,.18f),false,.32f,.48f);
            DetailBox("Rounded vending shell",root,new Vector3(0,1.0f,0),new Vector3(1.18f,2f,.83f),paint,.07f);
            DetailBox("Recessed display cavity",root,new Vector3(-.13f,1.25f,-.429f),new Vector3(.77f,1.11f,.032f),Materials["Black"],.028f);
            for(int row=0;row<3;row++)
            {
                DetailBox("Chilled shelf",root,new Vector3(-.14f,.85f+row*.29f,-.48f),new Vector3(.70f,.024f,.15f),Materials["Metal"],.006f);
                for(int col=0;col<4;col++)
                {
                    Vector3 p=new Vector3(-.40f+col*.17f,.96f+row*.29f,-.48f);
                    Material can=col%3==0?Materials["Red"]:col%3==1?Materials["Blue"]:Materials["White"];
                    Primitive("Drink can",PrimitiveType.Cylinder,root,p,new Vector3(.102f,.105f,.102f),can,false);
                    Primitive("Can lid",PrimitiveType.Cylinder,root,p+Vector3.up*.105f,new Vector3(.098f,.005f,.098f),Materials["Metal"],false);
                }
            }
            DetailBox("Vending display glass",root,new Vector3(-.13f,1.25f,-.56f),new Vector3(.79f,1.14f,.008f),Materials["ClearGlass"],.002f);
            DetailBox("Vending header",root,new Vector3(0,1.88f,-.438f),new Vector3(1.03f,.15f,.01f),Materials["SignFace"],.004f);
            CreateText("COLD DRINKS",root,new Vector3(0,1.88f,-.45f),Quaternion.identity,.026f,Color.white,64);
            for(int row=0;row<6;row++)
                DetailBox("Selection key",root,new Vector3(.44f,1.48f-row*.085f,-.435f),new Vector3(.10f,.057f,.03f),Materials["Metal"],.008f);
            DetailBox("Drink collection hatch",root,new Vector3(0,.39f,-.443f),new Vector3(.78f,.25f,.046f),Materials["Rubber"],.03f);
            TextMesh label=CreateText("OUT OF ORDER",root,new Vector3(.01f,.63f,-.45f),Quaternion.identity,.017f,Color.white,64);
            MachineAnimator motion=root.gameObject.AddComponent<MachineAnimator>();motion.screenText=label.transform;motion.speed=.4f;
            AddBoxCollider(root.gameObject,new Vector3(0,1,0),new Vector3(1.19f,2,.99f));
            AddInteraction(root.gameObject,InteractionKind.Information,"Inspect vending machine","OUT OF ORDER. The coin return contains only dust.");
        }
    }
}
