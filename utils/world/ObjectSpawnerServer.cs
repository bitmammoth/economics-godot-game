using Godot;
using System.Collections.Generic;

namespace Game
{
    public class ObjectSpawnerServer : ObjectSpawnerBase
    {
        public override void _Ready()
        {
            if (Server.database != null)
            {
                foreach (var item in Server.database.Table<WorldObject>().ToList())
                {
                    SpawnObject(item);
                }
            }
        }


        [Remote]
        public void GetObjectList(Vector3 position, float distance)
        {
            //GD.Print("[Server][Object] Request list for " + position.ToString() + " with distance " + distance);

            var list = new List<WorldObject>();
            foreach (WorldObjectNode x in GetChildren())
            {
                if (x.worldObject != null && (x.worldObject.GetPosition() - position).Length() <= distance)
                {
                    list.Add(x.worldObject);
                }
            }

            var id = Multiplayer.GetRpcSenderId();
            var objectJson = Networking.NetworkCompressor.Compress(list);

            RpcId(id, "UpdateClientObjectList", objectJson);
        }

        [Remote]
        public void AddObject(string model, Vector3 pos, Vector3 rot)
        {
            GD.Print("[Server][Object] Create " + model);

            var obj = new WorldObject
            {

                modelName = model
            };

            obj.SetPosition(pos);
            obj.SetRotation(rot);

            Server.database.Insert(obj);

            System.Threading.Thread thread = new System.Threading.Thread(() => SpawnObject(obj));
            thread.Start();

            

            var objectJson = Networking.NetworkCompressor.Compress(obj);
            Rpc("OnNewObject", objectJson);
        }


    }
}

