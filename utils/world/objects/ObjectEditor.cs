using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Game
{
    public class ObjectEditor : CanvasLayer
    {
        private ItemList objectList = null;
        private WindowDialog dialog = null;

        private Game.WorldObjectNode newWorldObject = null;
        private bool saveObject = false;
        private World world = null;
        private Player player = null;

        [Export]
        private float rotationMultiplier = 1.0f;

        private List<string> tempItemList = new List<string>();

        private string currentObjectName = null;

        public override void _Ready()
        {
            dialog = GetNode("dialog") as WindowDialog;
            world = GetParent().GetParent().GetParent() as World;
            player = GetParent() as Player;

            dialog.Connect("popup_hide", this, "onClose");
            dialog.GetCloseButton().Connect("pressed", this, "onClose");
            dialog.FindNode("add_object").Connect("pressed", this, "addObject");


            objectList = dialog.FindNode("object_list") as ItemList;
            objectList.Connect("item_selected", this, "onObjectSelected");
            (dialog.FindNode("object_search_box") as LineEdit).Connect("text_changed", this, "onObjectSearch");

            tempItemList = scanDir("res://objects/");
            tempItemList.Sort();

            foreach (var item in tempItemList)
            {
                objectList.AddItem(item.Replace("res://objects/", "").TrimStart('/'));
            }
        }

        public void onObjectSearch(string search)
        {
            objectList.Clear();
            if (string.IsNullOrEmpty(search))
            {
                foreach (var item in tempItemList)
                {
                    objectList.AddItem(item.Replace("res://objects/", "").TrimStart('/'));
                }
            }
            else
            {
                foreach (var item in tempItemList.Where(tf => tf.ToLower().Contains(search.ToLower())))
                {
                    objectList.AddItem(item.Replace("res://objects/", "").TrimStart('/'));
                }
            }
        }
        public void onObjectSelected(int selected)
        {
            var item = objectList.GetItemText(selected);

            if (currentObjectName != item)
            {
                showObject(item);
                currentObjectName = item;
            }
        }
        private void showObject(string name)
        {
            var oldObject = FindNode("camera_viewport").GetNodeOrNull("object_holder");
            if (oldObject != null)
                oldObject.QueueFree();

            if (!ResourceLoader.Exists("res://objects/" + name + ".tscn"))
            {
                GD.PrintErr("[Spawner] Cant find: " + name);
            }

            var nodeScene = (PackedScene)ResourceLoader.Load("res://objects/" + name + ".tscn");
            var spat = (Spatial)nodeScene.Instance();
            spat.Name = "object_holder";
            FindNode("camera_viewport").AddChild(spat);
        }
        public void addObject()
        {
            foreach (var i in objectList.GetSelectedItems())
            {
                var item = objectList.GetItemText(i);
                dialog.Hide();
                DrawObject(item);
                Input.SetMouseMode(Input.MouseMode.Captured);

                break;
            }
        }

        public void Show()
        {
            Input.SetMouseMode(Input.MouseMode.Visible);
            (GetNode("dialog") as WindowDialog).Show();
        }

        public void onClose()
        {
            Input.SetMouseMode(Input.MouseMode.Captured);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Input.GetMouseMode() != Input.MouseMode.Visible)
                MoveObject();

            if (Input.IsActionJustPressed("lmb") && Input.GetMouseMode() != Input.MouseMode.Visible)
                RequestAddObject();

            if (Input.IsActionJustPressed("rmb") && Input.GetMouseMode() != Input.MouseMode.Visible)
                CancelRequest();
        }


        private List<string> scanDir(string path)
        {
            string file_name = null;
            var files = new List<string>();
            var dir = new Directory();
            var error = dir.Open(path);
            if (error != Error.Ok)
            {
                GD.PrintErr("Can't open " + path + "!");
                return new List<string>();
            }

            dir.ListDirBegin(true);
            file_name = dir.GetNext();
            while (file_name != "")
            {
                if (dir.CurrentIsDir())
                {
                    var new_path = path + "/" + file_name;
                    files.AddRange(scanDir(new_path));
                }
                else
                {
                    var name = path + "/" + file_name;
                    if (file_name.Contains(".tscn"))
                        files.Add(name.Replace(".tscn", ""));
                }
                file_name = dir.GetNext();
            }

            dir.ListDirEnd();
            return files;
        }
        private void DrawObject(string modelName)
        {
            if (newWorldObject != null)
                return;

            newWorldObject = new Game.WorldObjectNode
            {
                worldObject = new Game.WorldObject
                {
                    type = Game.WorldObjectType.PROPERTY,
                    modelName = modelName
                }
            };

            var spawnThread = new System.Threading.Thread(() =>
            {
                bool result = newWorldObject.LoadObjectByFilePath();
                if (result)
                {
                    newWorldObject.Name = "temp_object";
                    saveObject = false;
                    world.AddChild(newWorldObject);
                }
            });

            spawnThread.Start();
        }
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton)
            {
                InputEventMouseButton emb = (InputEventMouseButton)@event;
                if (emb.IsPressed())
                {
                    if (emb.ButtonIndex == (int)ButtonList.WheelUp)
                    {
                        scrollObject(-1);
                    }
                    if (emb.ButtonIndex == (int)ButtonList.WheelDown)
                    {
                        scrollObject(1);
                    }
                }
            }
        }

        private void scrollObject(int rorator)
        {
            if (newWorldObject == null || !newWorldObject.IsInsideTree())
                return;

            var rot = newWorldObject.RotationDegrees;
            rot.y += rorator * rotationMultiplier;
            newWorldObject.RotationDegrees = rot;
        }
        private void CancelRequest()
        {
            if (newWorldObject != null)
            {
                newWorldObject.QueueFree();
            }

            newWorldObject = null;
            saveObject = false;
            player.onFocusing = false;

            //go back to dialog
            Input.SetMouseMode(Input.MouseMode.Visible);
            dialog.Show();
        }
        private void RequestAddObject()
        {
            GD.Print("press release");
            if (newWorldObject != null)
            {
                if (saveObject)
                {
                    var modelName = newWorldObject.worldObject.modelName;
                    var modelType = newWorldObject.worldObject.type;
                    var position = newWorldObject.GlobalTransform.origin;
                    var rotation = newWorldObject.Rotation;

                    world.spawner.AskToCreate(modelName, modelType, position, rotation);
                }
            }

            CancelRequest();
        }

        private void MoveObject()
        {
            if (newWorldObject == null || !newWorldObject.IsInsideTree())
                return;

            player.onFocusing = true;

            if (player.rayDrag.IsColliding())
            {
                newWorldObject.Visible = true;
                var raycast_result = player.rayDrag.GetCollider();

                if (raycast_result is Spatial && (raycast_result as Spatial).Name == "terrain")
                {
                    var gt = newWorldObject.GlobalTransform;
                    gt.origin = player.rayDrag.GetCollisionPoint();
                    newWorldObject.GlobalTransform = gt;
                    newWorldObject.Visible = true;

                    saveObject = true;
                }
                else
                {
                    newWorldObject.Visible = false;
                    saveObject = false;
                }
            }
            else
            {
                newWorldObject.Visible = false;
                saveObject = false;
            }
        }

    }
}