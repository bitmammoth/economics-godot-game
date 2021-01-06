using Godot;
using System;
using System.Linq;
using Game;
using Newtonsoft.Json;

using System.Collections.Generic;
namespace Game
{
    public class Player : NetworkPlayer
    {
        const float MOTION_INTERPOLATE_SPEED = 15.0f;
        const float CAMERA_ROTATION_SPEED = 0.001f;
        const float CAMERA_X_ROT_MIN = -80;
        const float CAMERA_X_ROT_MAX = 80;


        public bool inputEnabled = true;

        [Export]
        public float InputPredictionMaxSize = 0.05f;

        [Export]
        public NodePath rayDragPath;

        [Export]
        public int InputPredictionMaxPackages = 1024;

        [Export]
        public NodePath cameraPath;

        [Export]
        public NodePath cameraBasePath;

        [Export]
        public NodePath targetPath;

        [Export]
        public NodePath crosshairPath;

        public ClippedCamera camera { get; set; }
        public Spatial cameraBase { get; set; }
        public Spatial target { get; set; }
        public Control crosshair { get; set; }
        public RayCast rayDrag { get; set; }

        protected NetworkPlayerChar character { get; set; }

        [Export]
        public NodePath characterPath;

        private float fov_initial = 0f;
        private Vector3 camera_target_initial = Vector3.Zero;
        private Transform origCameraTransform;


        private float camera_x_rot = 0.0f;
        private float camera_y_rot = 0.0f;

        private Timer stateTimer = new Timer();

        private bool reProcess = false;


        public List<FrameSnapshot> snapshots = new List<FrameSnapshot>();
        public List<FrameSnapshot> sendList = new List<FrameSnapshot>();

        public Queue<PlayerInput> inputQueue = new Queue<PlayerInput>();
        private PlayerInput lastInput = null;
        public override void _Ready()
        {
            InitPlayer();

            target = (Spatial)GetNode(targetPath);

            camera = (ClippedCamera)GetNode(cameraPath);
            cameraBase = (Spatial)GetNode(cameraBasePath);

            crosshair = (Control)GetNode(crosshairPath);
            character = (NetworkPlayerChar)GetNode(characterPath);
            rayDrag = (RayCast)GetNode(rayDragPath);

            camera_target_initial = target.Transform.origin;
            origCameraTransform = camera.Transform;

            fov_initial = camera.Fov;
        }


        public override void _PhysicsProcess(float delta)
        {
            //process input
            inputQueue.Enqueue(GetInput(delta));

            while (inputQueue.Count > 0)
            {
                var newInput = inputQueue.Dequeue();

                if (newInput != null)
                {
                    lastInput = ProcessInput(newInput, lastInput, delta);
                    character.ProcessAnimation(playerState, newInput, delta);
                }
            }

            if (sendList.Count() >= 10)
            {
                var list = sendList.GetRange(0, 10);
                sendList.RemoveRange(0, 10);

                SendInputToServer(list);
            }
        }

        private PlayerInput ProcessInput(PlayerInput newInput, PlayerInput oldInput, float delta)
        {
            if (oldInput != null)
                newInput.velocity = oldInput.velocity;

            newInput.velocity = DoWalk(CalculateVelocityByInput(newInput, delta));

            var snap = SaveSnapshot(newInput);

            return newInput;
        }


        public override void _Process(float delta)
        {
            networkStats.loop();
        }

        private void SendInputToServer(List<FrameSnapshot> snapshot)
        {
            var snap = Game.Networking.NetworkCompressor.Compress(snapshot);
            networkStats.AddPackage(snap);

            RpcUnreliableId(1, "OnNewInputSnapshot", snap);
        }
        private FrameSnapshot SaveSnapshot(PlayerInput state)
        {
            var snapshot = new FrameSnapshot();
            snapshot.movementState = state;
            snapshot.timestamp = OS.GetTicksMsec();
            snapshot.origin = GetPlayerPosition();
            snapshot.rotation = GetPlayerRotation();

            snapshots.Add(snapshot);
            sendList.Add(snapshot);

            //clear snapshots after
            if (snapshots.Count() > InputPredictionMaxPackages)
                snapshots.RemoveAt(0);


            return snapshot;
        }

        [Puppet]
        public void OnServerSnapshot(string correctedSnapshotJson)
        {

            if (reProcess == true)
                return;

            var correctedSnapshot = Game.Networking.NetworkCompressor.Decompress<FrameSnapshot>(correctedSnapshotJson);
            var oldSnapshot = snapshots.Find(t => t.timestamp == correctedSnapshot.timestamp);

            //remove all older then corrected snapshot and it to
            if (oldSnapshot != null)
            {
                var snapshotList = snapshots.Where(tf => tf.timestamp > correctedSnapshot.timestamp).ToList();

                var orig1 = oldSnapshot.origin;
                var orig2 = correctedSnapshot.origin;

                //draw server side mesh
                var td = (GetNode("server_side") as MeshInstance).GlobalTransform;
                td.origin = correctedSnapshot.origin;
                (GetNode("server_side") as MeshInstance).GlobalTransform = td;

                var td2 = (GetNode("client_side") as MeshInstance).GlobalTransform;
                td2.origin = oldSnapshot.origin;
                (GetNode("client_side") as MeshInstance).GlobalTransform = td2;


                if ((orig1 - orig2).Length() > InputPredictionMaxSize)
                {

                    GD.Print("[CLIENT] Pos correction for " + orig1 + " - " + orig2);

                    inputQueue.Clear();
                    reProcess = true;

                    //roleback
                    SetPlayerPosition(correctedSnapshot.origin);
                    SetPlayerRotation(correctedSnapshot.rotation);

                    lastInput = correctedSnapshot.movementState;
                    snapshotList.Reverse();

                    foreach (var lostSnapshot in snapshotList)
                    {
                        inputQueue.Enqueue(lostSnapshot.movementState);
                    }

                    reProcess = false;
                }

                snapshots.RemoveAll(tf => tf.timestamp <= correctedSnapshot.timestamp);
            }
            else if (snapshots.Count > 1)
            {
                GD.Print("[Client] Desyncing");

                snapshots.Clear();

                SetPlayerPosition(correctedSnapshot.origin);
                SetPlayerRotation(correctedSnapshot.rotation);

                lastInput = correctedSnapshot.movementState;
            }
        }

        private PlayerInput GetInput(float delta)
        {

            if (inputEnabled == false)
                return null;

            var movementInput = new PlayerInput();

            //cursor
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                if (Input.GetMouseMode() == Input.MouseMode.Visible)
                    Input.SetMouseMode(Input.MouseMode.Captured);

                else
                    Input.SetMouseMode(Input.MouseMode.Visible);
            }

            movementInput.cam_direction = Vector3.Zero;
            movementInput.movement_direction = Vector2.Zero;

            var cam_xform = camera.GlobalTransform;

            if (Input.IsActionPressed("move_forward") && Input.GetMouseMode() != Input.MouseMode.Visible)
                movementInput.movement_direction.y += 1;
            if (Input.IsActionPressed("move_backward") && Input.GetMouseMode() != Input.MouseMode.Visible)
                movementInput.movement_direction.y -= 1;
            if (Input.IsActionPressed("move_left") && Input.GetMouseMode() != Input.MouseMode.Visible)
                movementInput.movement_direction.x -= 1;
            if (Input.IsActionPressed("move_right") && Input.GetMouseMode() != Input.MouseMode.Visible)
                movementInput.movement_direction.x += 1;


            if (Input.IsActionJustPressed("spawn_object") && Input.GetMouseMode() != Input.MouseMode.Visible)
                DrawObject();
            if (Input.IsActionJustReleased("spawn_object") && Input.GetMouseMode() != Input.MouseMode.Visible)
                RequestAddObject();
            if (Input.IsActionPressed("spawn_object") && Input.GetMouseMode() != Input.MouseMode.Visible)
                MoveObject();


            // if (Input.IsActionJustReleased("save_objects") && Input.GetMouseMode() != Input.MouseMode.Visible)
            //   spow.SaveObjects(GetTree().Root.GetNode("level").GetNode("properties"));
            /*
            if (Input.IsActionJustPressed("enter_menu") && Input.GetMouseMode() != Input.MouseMode.Visible)
            {
                var coll = rayVehicle.GetCollider();
                if (coll != null)
                    menu.Open(GetNode("hud").GetNode("menu_popup") as WindowDialog, this, rayVehicle.GetCollider() as Node);
                else
                    menu.Open(GetNode("hud").GetNode("menu_popup") as WindowDialog, this, null);
            }
*/

            movementInput.movement_direction = movementInput.movement_direction.Normalized();

            movementInput.cam_direction += -cam_xform.basis.z * movementInput.movement_direction.y;
            movementInput.cam_direction += cam_xform.basis.x * movementInput.movement_direction.x;

            movementInput.cam_direction.y = 0;
            movementInput.cam_direction = movementInput.cam_direction.Normalized();

            //sprint
            if (Input.IsActionPressed("move_sprint"))
                movementInput.isSprinting = true;
            else
                movementInput.isSprinting = false;

            //jumping
            if (Input.IsActionPressed("move_jump"))
                movementInput.isJumping = true;
            else
                movementInput.isJumping = false;

            //jumping
            if (Input.IsActionJustReleased("test"))
            {
                GD.Print("Cheating");

                var gt = GlobalTransform;

                gt.origin = new Vector3(2559.1f, 223.767f, 4401f);
                GlobalTransform = gt;
            }

            //aiming
            var camera_target = camera_target_initial;
            var fov = fov_initial;
            var crosshair_alpha = 0.0f;

            if (Input.IsActionPressed("rmb") || Input.IsActionPressed("spawn_object"))
            {
                camera_target.x = -1.25f;
                crosshair_alpha = 1.0f;
                fov = 60;
                movementInput.isAiming = true;
            }

            if (Input.IsActionJustReleased("rmb"))
            {
                movementInput.isAiming = false;
            }

            var crmod = crosshair.Modulate;
            crmod.a += (crosshair_alpha - crosshair.Modulate.a) * 0.15f;
            crosshair.Modulate = crmod;

            var tf = target.Transform;
            tf.origin.x += (camera_target.x - target.Transform.origin.x) * 0.15f;
            target.Transform = tf;
            camera.Fov += (fov - camera.Fov) * 0.15f;

            // Force
            if (movementInput.isAiming && !playerState.weaponEquipped)
            {
                var space_state = GetWorld().DirectSpaceState;
                var center_position = GetViewport().Size / 2;
                var ray_from = camera.ProjectRayOrigin(center_position);
                var ray_to = ray_from + camera.ProjectRayNormal(center_position) * GRAB_DISTANCE;

                var arr = new Godot.Collections.Array();
                arr.Add(this);

                Godot.Collections.Dictionary ray_result = space_state.IntersectRay(ray_from, ray_to, arr);
                if (ray_result != null && ray_result.Contains("collider"))
                {
                    var body = ray_result["collider"];
                    if (body is RigidBody)
                    {
                        if (Input.IsActionJustPressed("lmb") && playerState.onGround)
                        {
                            (body as RigidBody).ApplyImpulse(new Vector3(0, 0, 0), -camera.GlobalTransform.basis.z.Normalized() * THROW_FORCE);
                        }
                    }
                }
            }

            return movementInput;
        }

        private Game.WorldObjectNode newWorldObject = null;
        private bool saveObject = false;
        private void DrawObject()
        {
            var gj = new Game.WorldObject
            {
                modelName = "tree"
            };

            newWorldObject = new Game.WorldObjectNode
            {
                worldObject = gj
            };

            newWorldObject.LoadObjectByFilePath();
            newWorldObject.Name = "temp_object";

            saveObject = false;

            GetParent().GetParent().AddChild(newWorldObject);

        }
        private void RequestAddObject()
        {
            if (saveObject && newWorldObject != null)
            {
                var modelName = newWorldObject.worldObject.modelName;
                var position = newWorldObject.GlobalTransform.origin;
                var rotation = newWorldObject.Rotation;
                var world = GetParent().GetParent() as World;
                world.spawner.AskToCreate(modelName, position, rotation);
            }

            if (newWorldObject != null)
            {
                GetParent().GetParent().RemoveChild(newWorldObject);
                newWorldObject = null;
            }

            saveObject = false;
        }

        private void MoveObject()
        {
            if (rayDrag.IsColliding() && newWorldObject != null)
            {
                newWorldObject.Visible = true;
                var raycast_result = rayDrag.GetCollider();

                if (raycast_result is StaticBody || raycast_result is Spatial)
                {
                    var gt = newWorldObject.GlobalTransform;
                    gt.origin = rayDrag.GetCollisionPoint();
                    newWorldObject.GlobalTransform = gt;
                    newWorldObject.Visible = true;

                    saveObject = true;

                }
                else
                {
                    newWorldObject.Visible = false;
                }
            }
            else if (newWorldObject != null)
            {
                newWorldObject.Visible = false;
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventMouseMotion && Input.GetMouseMode() == Input.MouseMode.Captured)
            {
                var mot = @event as InputEventMouseMotion;
                cameraBase.RotateY(-mot.Relative.x * CAMERA_ROTATION_SPEED);
                cameraBase.Orthonormalize();

                camera_x_rot = Mathf.Clamp(camera_x_rot + mot.Relative.y * CAMERA_ROTATION_SPEED, Mathf.Deg2Rad(CAMERA_X_ROT_MIN), Mathf.Deg2Rad(CAMERA_X_ROT_MAX));
                camera_y_rot = Mathf.Clamp(camera_y_rot + mot.Relative.x * CAMERA_ROTATION_SPEED, Mathf.Deg2Rad(CAMERA_X_ROT_MIN), Mathf.Deg2Rad(CAMERA_X_ROT_MAX));
                var newRot = (cameraBase.FindNode("rotation") as Spatial).Rotation;
                newRot.x = camera_x_rot;

                (cameraBase.FindNode("rotation") as Spatial).Rotation = newRot;
            }
        }

    }
}