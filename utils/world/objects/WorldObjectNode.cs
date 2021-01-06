using System;
using Godot;
using SQLite;

namespace Game
{

    public class WorldObjectNode : StaticBody
    {
        public WorldObject worldObject = null;

        public bool colliderCreated = false;

        public string getUniqIdent()
        {
            return worldObject.modelName + "_" + GlobalTransform.origin.x + "_" + GlobalTransform.origin.y + "_" + GlobalTransform.origin.z;
        }

        public void LoadObjectByFilePath()
        {
            worldObject.modelName = worldObject.modelName;
            var meshInst = new MeshInstance();
            var mt = new SpatialMaterial();
            GD.Print("[Object][Load ressource] " + worldObject.modelName);
            ArrayMesh obj = (ArrayMesh)ResourceLoader.Load("res://objects/" + worldObject.modelName + ".obj");

            for (int i = 0; i < obj.GetSurfaceCount(); i++)
            {
                obj.SurfaceSetMaterial(i, createMaterial(worldObject.modelName, i));
            }

            meshInst.Mesh = obj;
            meshInst.Name = "model";
            AddChild(meshInst);
        }

        private SpatialMaterial createMaterial(string objName, int surface)
        {
            SpatialMaterial mat = new SpatialMaterial();

            if (ResourceLoader.Exists("res://objects/" + objName + "_"+surface+"_normal.jpg"))
            {
                StreamTexture normalTexture = (StreamTexture)ResourceLoader.Load("res://objects/" + objName + "_" + surface + "_normal.jpg");
                mat.NormalTexture = normalTexture;
                mat.NormalEnabled = true;
            }

            if (ResourceLoader.Exists("res://objects/" + objName + "_"+surface+"_color.jpg"))
            {
                StreamTexture albedoTexture = (StreamTexture)ResourceLoader.Load("res://objects/" + objName + "_" + surface + "_color.jpg");
                mat.AlbedoTexture = albedoTexture;
            }

            if (ResourceLoader.Exists("res://objects/" + objName + "_"+surface+"_height.jpg"))
            {
                StreamTexture heightTexture = (StreamTexture)ResourceLoader.Load("res://objects/" + objName + "_" + surface + "_height.jpg");
                mat.DepthTexture = heightTexture;
            }

            if (ResourceLoader.Exists("res://objects/" + objName + "_"+surface+"_metal.jpg"))
            {
                StreamTexture metalTexture = (StreamTexture)ResourceLoader.Load("res://objects/" + objName + "_" + surface + "_metal.jpg");
                mat.MetallicTexture = metalTexture;
            }

            return mat;
        }
    }
}

