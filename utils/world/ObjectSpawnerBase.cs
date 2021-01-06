using Godot;
using System.Collections.Generic;

namespace Game
{
    public class ObjectSpawnerBase : Spatial
    {

        [Export]
        public float ObjectDistance = 100.0f;

        public WorldObjectNode SpawnObject(WorldObject spawn, bool ignoreCollider = false)
        {
           
            if (ignoreCollider == false)
            {
                foreach (WorldObjectNode x in GetChildren())
                {
                    if (x.worldObject.modelName == spawn.modelName && x.colliderCreated)
                    {
                        WorldObjectNode existNode = (WorldObjectNode)x.Duplicate();
                        existNode.worldObject = spawn;

                        var gt2 = existNode.GlobalTransform;
                        gt2.origin = spawn.GetPosition();
                        existNode.GlobalTransform = gt2;
                        existNode.Rotation = spawn.GetRotation();
                        existNode.Name = spawn.Id.ToString();

                        // existNode.origPos = pos;
                        AddChild(existNode);

                        existNode.Visible = false;
                        existNode.colliderCreated = true;

                        return existNode;
                    }
                }
            }
            

            var node = new WorldObjectNode();
            node.worldObject = spawn;
            node.LoadObjectByFilePath();
            node.Name = spawn.Id.ToString();
            AddChild(node);

            var gt = node.GlobalTransform;
            gt.origin = spawn.GetPosition();
            node.GlobalTransform = gt;
            node.Rotation = spawn.GetRotation();
            //node.origPos = pos;
            node.Visible = true;

            return node;
        }

    }
}