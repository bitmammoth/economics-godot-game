using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class ObjectSpawner : ObjectSpawnerBase
    {


        [Export]
        public float SyncTime = 0.5f;

        private float syncedTime = 0.0f;

        private bool onUpdate = false;


        public override void _Process(float delta)
        {
            if (syncedTime >= SyncTime)
            {
                if (!onUpdate)
                    RpcUnreliableId(1, "GetObjectList", (GetParent() as World).player.GetPlayerPosition(), ObjectDistance);
                syncedTime = 0.0f;
            }

            syncedTime += delta;
        }

        public void AskToCreate(string model, Vector3 pos, Vector3 rot)
        {
            RpcId(1, "AddObject", model, pos, rot);
        }

        [Puppet]
        private void UpdateClientObjectList(string json)
        {
            if (onUpdate)
                return;

            onUpdate = true;
            var objects = Networking.NetworkCompressor.Decompress<List<WorldObject>>(json);

            System.Threading.Thread thread = new System.Threading.Thread(() => ThreadedCreation(objects));
            thread.Start();

            onUpdate = false;
        }

        private void ThreadedCreation(List<WorldObject> _list)
        {
            var ids = _list.Select(c => c.Id.ToString()).ToList();

            foreach (var obj in _list)
            {
                var exist = GetNodeOrNull(obj.Id.ToString());
                if (exist != null)
                    continue;
                else
                {
                    SpawnObject(obj);
                }
            }

            foreach (WorldObjectNode existObj in GetChildren())
            {
                if (!ids.Contains(existObj.Name))
                {
                    existObj.QueueFree();
                }
            }

        }

        [Puppet]
        private void OnNewObject(string objectJson)
        {
            var obj = Networking.NetworkCompressor.Decompress<WorldObject>(objectJson);
            GD.Print("[Client][Object] " + obj.modelName + " at" + obj.GetPosition().ToString());


            System.Threading.Thread thread = new System.Threading.Thread(() => SpawnObject(obj));
            thread.Start();
        }



    }
}

