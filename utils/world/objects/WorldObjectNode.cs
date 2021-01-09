using System;
using Godot;
using SQLite;

namespace Game
{

    public class WorldObjectNode : Spatial
    {
        public WorldObject worldObject = null;

        public string getUniqIdent()
        {
            return worldObject.modelName + "_" + GlobalTransform.origin.x + "_" + GlobalTransform.origin.y + "_" + GlobalTransform.origin.z;
        }

        public bool LoadObjectByFilePath()
        {
            if (worldObject.type == WorldObjectType.PROPERTY)
            {
                if (!ResourceLoader.Exists("res://objects/" + worldObject.modelName + ".tscn"))
                {
                    GD.PrintErr("[Spawner] Cant find: " + worldObject.modelName);
                    return false;
                }

                var nodeScene = (PackedScene)ResourceLoader.Load("res://objects/" + worldObject.modelName + ".tscn");
                AddChild((Spatial)nodeScene.Instance());

                return true;
            }
            else if (worldObject.type == WorldObjectType.MARKER)
            {
                var nodeScene = (PackedScene)ResourceLoader.Load("res://utils/world/objects/marker/marker.tscn");
                AddChild((Spatial)nodeScene.Instance());

                return true;
            }
            else
            {
                GD.PrintErr("[Spawner] Cant find type: " + worldObject.type.ToString());
                return false;
            }


        }
    }
}

