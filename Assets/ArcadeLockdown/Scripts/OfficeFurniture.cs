using UnityEngine;

namespace ArcadeLockdown
{
    public static partial class ArcadeWorldBuilder
    {
        private static void BuildOfficeDeskDetails(Transform desk)
        {
            // Preserve desk footprint and token placement, with real-scale manufactured parts.
            DetailBox("Walnut desktop with eased edge",desk,new Vector3(0,.84f,0),new Vector3(3.2f,.10f,1.3f),Materials["Wood"],.025f,true);
            DetailBox("Desktop underside trim",desk,new Vector3(0,.775f,0),new Vector3(3.12f,.04f,1.23f),Materials["CabinetWood"],.012f);
            for(int side=-1;side<=1;side+=2)
            {
                Transform drawers=PartRoot("Office drawer pedestal",desk,new Vector3(side*1.2f,0,0));
                DetailBox("Pedestal shell",drawers,new Vector3(0,.39f,0),new Vector3(.55f,.77f,1.1f),Materials["CabinetWood"],.015f,true);
                for(int i=0;i<3;i++)
                {
                    DetailBox("Recessed drawer front",drawers,new Vector3(0,.17f+i*.225f,-.556f),new Vector3(.50f,.20f,.034f),Materials["Wood"],.008f);
                    Rod("Brushed drawer pull",drawers,new Vector3(-.12f,.23f+i*.225f,-.602f),new Vector3(.12f,.23f+i*.225f,-.602f),.013f,Materials["Metal"]);
                    for(int x=-1;x<=1;x+=2) Rod("Pull support",drawers,new Vector3(x*.12f,.23f+i*.225f,-.573f),new Vector3(x*.12f,.23f+i*.225f,-.602f),.013f,Materials["Metal"]);
                }
                DetailBox("Recessed plinth",drawers,new Vector3(0,.045f,0),new Vector3(.49f,.09f,1.04f),Materials["Rubber"],.01f);
            }
            DetailBox("Desk modesty panel",desk,new Vector3(0,.49f,.46f),new Vector3(2.3f,.57f,.05f),Materials["Wood"],.01f);
        }

        private static void BuildComputer(Transform desk)
        {
            Transform pc=PartRoot("MANAGER COMPUTER - Puzzle 2",desk,new Vector3(0,.90f,.13f));
            Material plastic=MakeMaterial("Office graphite ABS",new Color(.07f,.081f,.094f),false,0,.3f);
            Material display=MakeMaterial("Office LCD dark blue",new Color(.014f,.035f,.052f),true,0,.28f);
            DetailBox("Monitor foot",pc,new Vector3(0,.018f,.06f),new Vector3(.39f,.036f,.25f),plastic,.025f);
            DetailBox("Monitor stand",pc,new Vector3(0,.18f,.11f),new Vector3(.07f,.30f,.065f),Materials["Metal"],.014f);
            Transform screen=PartRoot("Tiltable monitor",pc,new Vector3(0,.39f,.045f));
            screen.localRotation=Quaternion.Euler(5,0,0);
            DetailBox("Monitor rear shell",screen,Vector3.zero,new Vector3(.78f,.49f,.06f),plastic,.018f,true);
            DetailBox("Recessed LCD glass",screen,new Vector3(0,.013f,-.034f),new Vector3(.719f,.402f,.008f),display,.006f);
            CreateText("ARCADE / SECURITY",screen,new Vector3(0,.145f,-.041f),Quaternion.identity,.0045f,new Color(.56f,.81f,.85f),64);
            CreateText("MANAGER LOGIN",screen,new Vector3(0,.049f,-.041f),Quaternion.identity,.0055f,Color.white,64);
            CreateText("PASSWORD   _ _ _ _ _ _",screen,new Vector3(0,-.039f,-.041f),Quaternion.identity,.0041f,new Color(.70f,.77f,.81f),64);
            CreateText("[ E ]  ACCESS TERMINAL",screen,new Vector3(0,-.125f,-.041f),Quaternion.identity,.0036f,new Color(.3f,.8f,.7f),64);
            for(int i=0;i<12;i++) DetailBox("Rear cooling slot",screen,new Vector3(-.23f+i*.04f,.14f,.031f),new Vector3(.017f,.04f,.006f),Materials["Black"],.001f);
            Primitive("Power LED",PrimitiveType.Sphere,screen,new Vector3(.32f,-.224f,-.035f),Vector3.one*.007f,Materials["Cyan"],false);
            // Keyboard has individual key caps, a space bar and a separate mouse.
            Transform keyboard=PartRoot("Office keyboard",pc,new Vector3(-.04f,.018f,-.43f));
            DetailBox("Keyboard tray",keyboard,Vector3.zero,new Vector3(.53f,.034f,.17f),plastic,.013f);
            for(int row=0;row<5;row++)for(int col=0;col<15;col++)
            {
                if(row==4 && col>2 && col<11) continue;
                DetailBox("Key cap",keyboard,new Vector3(-.235f+col*.033f,.023f,.059f-row*.029f),new Vector3(.027f,.012f,.023f),Materials["DarkMetal"],.003f);
            }
            DetailBox("Spacebar",keyboard,new Vector3(-.005f,.023f,-.057f),new Vector3(.244f,.012f,.023f),Materials["DarkMetal"],.004f);
            DetailBox("Mouse mat",pc,new Vector3(.43f,-.003f,-.40f),new Vector3(.28f,.006f,.25f),Materials["Rubber"],.012f);
            Primitive("Optical mouse shell",PrimitiveType.Sphere,pc,new Vector3(.43f,.024f,-.40f),new Vector3(.074f,.045f,.117f),plastic,false);
            Rod("Mouse button seam",pc,new Vector3(.43f,.047f,-.45f),new Vector3(.43f,.047f,-.40f),.0015f,Materials["Black"]);
            DetailBox("PC tower",pc,new Vector3(.70f,.23f,.15f),new Vector3(.20f,.46f,.43f),plastic,.015f,true);
            DetailBox("PC front bezel",pc,new Vector3(.70f,.23f,-.073f),new Vector3(.18f,.43f,.023f),Materials["DarkMetal"],.009f);
            for(int i=0;i<9;i++) Cube("Tower intake vent",pc,new Vector3(.70f,.05f+i*.017f,-.087f),new Vector3(.14f,.005f,.002f),Materials["Black"],false);
            for(int i=0;i<2;i++) DetailBox("USB port",pc,new Vector3(.675f+i*.047f,.30f,-.087f),new Vector3(.025f,.011f,.004f),Materials["Black"],.002f);
            Primitive("Tower power button",PrimitiveType.Sphere,pc,new Vector3(.70f,.37f,-.086f),Vector3.one*.015f,Materials["Metal"],false);
            Rod("Monitor data cable",pc,new Vector3(0,.17f,.14f),new Vector3(.45f,.022f,.28f),.009f,Materials["Rubber"]);
            Rod("Cable to tower",pc,new Vector3(.45f,.022f,.28f),new Vector3(.69f,.12f,.38f),.009f,Materials["Rubber"]);
            AddInteraction(pc.gameObject,InteractionKind.OfficeComputer,"Use manager computer",string.Empty);
            AddBoxCollider(pc.gameObject,new Vector3(0,.35f,0),new Vector3(.82f,.6f,.18f));
        }

        private static void BuildOfficeChair(Transform parent,Vector3 position)
        {
            Transform chair=PartRoot("Manager chair - upholstered swivel",parent,position,180);
            Material fabric=MakePbrMaterial("Woven charcoal office upholstery",new Color(.18f,.23f,.26f),"PBR/Carpet012/Carpet012_1K-JPG_Color","PBR/Carpet012/Carpet012_1K-JPG_NormalGL","",new Vector2(3,3),0,.12f);
            Material frame=MakeMaterial("Chair graphite polymer",new Color(.045f,.053f,.06f),false,.12f,.32f);
            for(int i=0;i<5;i++)
            {
                float angle=i*Mathf.PI*2/5;
                Vector3 tip=new Vector3(Mathf.Cos(angle)*.33f,.10f,Mathf.Sin(angle)*.33f);
                Rod("Five-star aluminum base",chair,new Vector3(0,.18f,0),tip,.041f,Materials["Metal"]);
                Transform caster=PartRoot("Twin wheel caster",chair,tip, -angle*Mathf.Rad2Deg);
                for(int side=-1;side<=1;side+=2)
                    Primitive("Rubber caster wheel",PrimitiveType.Cylinder,caster,new Vector3(side*.03f,-.055f,0),new Vector3(.085f,.022f,.085f),frame,false).transform.localRotation=Quaternion.Euler(0,0,90);
            }
            Rod("Gas lift piston",chair,new Vector3(0,.14f,0),new Vector3(0,.46f,0),.043f,Materials["Metal"]);
            Rod("Piston boot",chair,new Vector3(0,.15f,0),new Vector3(0,.28f,0),.065f,frame);
            DetailBox("Seat molded shell",chair,new Vector3(0,.46f,0),new Vector3(.56f,.05f,.54f),frame,.055f);
            DetailBox("Seat cushion",chair,new Vector3(0,.51f,-.015f),new Vector3(.55f,.10f,.51f),fabric,.045f);
            Transform back=PartRoot("Ergonomic back support",chair,new Vector3(0,.80f,.225f));
            back.localRotation=Quaternion.Euler(9,0,0);
            DetailBox("Curved back shell",back,Vector3.zero,new Vector3(.52f,.59f,.075f),frame,.035f);
            DetailBox("Upholstered back cushion",back,new Vector3(0,0,-.038f),new Vector3(.47f,.54f,.065f),fabric,.029f);
            DetailBox("Lumbar cushion",back,new Vector3(0,-.17f,-.076f),new Vector3(.43f,.13f,.038f),fabric,.018f);
            for(int side=-1;side<=1;side+=2)
            {
                Rod("Armrest support",chair,new Vector3(side*.24f,.45f,.08f),new Vector3(side*.31f,.69f,.08f),.025f,frame);
                DetailBox("Padded armrest",chair,new Vector3(side*.31f,.71f,0),new Vector3(.075f,.045f,.32f),frame,.02f);
                Rod("Back support rail",chair,new Vector3(side*.15f,.44f,.16f),new Vector3(side*.15f,.81f,.28f),.021f,Materials["Metal"]);
            }
            Rod("Height adjustment lever",chair,new Vector3(.12f,.44f,0),new Vector3(.32f,.42f,-.10f),.015f,Materials["Metal"]);
            DetailBox("Lever grip",chair,new Vector3(.32f,.42f,-.10f),new Vector3(.085f,.022f,.039f),frame,.01f);
            AddBoxCollider(chair.gameObject,new Vector3(0,.55f,.02f),new Vector3(.69f,1.1f,.62f));
        }

        private static void BuildOfficeAccessories(Transform desk)
        {
            Transform phone=PartRoot("Office desk telephone",desk,new Vector3(-.95f,.92f,.04f),-12);
            DetailBox("Phone base",phone,Vector3.zero,new Vector3(.23f,.045f,.25f),Materials["Rubber"],.018f);
            DetailBox("Phone handset",phone,new Vector3(-.068f,.038f,0),new Vector3(.064f,.047f,.23f),Materials["DarkMetal"],.022f);
            for(int r=0;r<4;r++)for(int c=0;c<3;c++)DetailBox("Phone key",phone,new Vector3(.01f+c*.03f,.029f,-.07f+r*.03f),new Vector3(.021f,.009f,.022f),Materials["Metal"],.003f);
            for(int i=0;i<20;i++)Primitive("Coiled handset cord",PrimitiveType.Sphere,phone,new Vector3(-.15f+Mathf.Sin(i*1.7f)*.012f,-.004f,.1f-i*.011f),Vector3.one*.016f,Materials["Rubber"],false);
            for(int i=0;i<3;i++)DetailBox("Document tray",desk,new Vector3(1.06f,.918f+i*.034f,.17f),new Vector3(.32f,.012f,.40f),Materials["DarkMetal"],.008f);
            DetailBox("Paper stack",desk,new Vector3(1.06f,1.004f,.17f),new Vector3(.285f,.016f,.36f),Materials["White"],.001f);
            Primitive("Pen cup",PrimitiveType.Cylinder,desk,new Vector3(-.62f,.965f,.35f),new Vector3(.08f,.075f,.08f),Materials["Metal"],false);
            for(int i=0;i<4;i++)Rod("Pen",desk,new Vector3(-.645f+i*.014f,.95f,.35f),new Vector3(-.66f+i*.022f,1.09f,.355f),.005f,i%2==0?Materials["Blue"]:Materials["Black"]);
        }
    }
}
