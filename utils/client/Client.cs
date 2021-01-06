using Godot;
using System;
using Game.Loader;
using System.Collections.Generic;
using RestClient.Net;

namespace Game
{
    public class Client : NetworkGame
    {
        [Export]
        public string hostname = "localhost";

        [Export]
        public int port = 27021;

        protected string accessToken = "Q80GQUXJTQD29BP414NG53TBDGALELLEYB75UE00MP0KW3GFRDST6EX6JP14IKDA6GSV9JKF090J3VT9";

        private int ownNetworkId = 0;

        private World gameWorld;

        public bool inputEnabled = true;

        AcceptDialog customConnectDialog = null;
        ServerPreAuthDialog serverPareAuthDialog = null;
        CharacterSelector charSelctor = null;

        Timer statsTimer = new Timer();
        public override void _Ready()
        {
            InitNetwork();

            Multiplayer.Connect("connected_to_server", this, "onConnected");
            Multiplayer.Connect("connection_failed", this, "onConnectionFailed");
            Multiplayer.Connect("server_disconnected", this, "onDisconnect");

            serverPareAuthDialog = GetNode("hud/server_preauth_dialog") as ServerPreAuthDialog;
            serverPareAuthDialog.Connect("onLogin", this, "onLoginSuccess");

            customConnectDialog = GetNode("hud/custom_connect_dialog") as AcceptDialog;
            customConnectDialog.Connect("confirmed", this, "onCustomServerConnect");

            charSelctor = GetNode("hud/char_selector") as CharacterSelector;
            charSelctor.Visible = false;

            statsTimer.Name = "stats_timer";
            statsTimer.Autostart = true;
            statsTimer.WaitTime = 0.5f;
            statsTimer.Connect("timeout", this, "showSystemStats");

            AddChild(statsTimer);
        }

        public void onLoginSuccess(string _token)
        {
            accessToken = _token;
            charSelctor.Visible = true;
            charSelctor.hostname = hostname;
            charSelctor.port = port;
            charSelctor.SetToken(accessToken);
            charSelctor.GetCharList();
            
            //ConnectToServer();
            
        }
        public void onCustomServerConnect()
        {
            var customAddress = (customConnectDialog.FindNode("custom_server_address") as LineEdit).Text;
            var custumPort = (customConnectDialog.FindNode("custom_server_port") as LineEdit).Text;

            try
            {
                port = Int16.Parse(custumPort);
                hostname = customAddress;
                DoPreAuthLogin();

            }
            catch
            {
                
            }
        }

        public async void DoPreAuthLogin()
        {
            try
            {
                var restClient = new RestClient.Net.Client(new RestClient.Net.NewtonsoftSerializationAdapter(), new Uri("http://" + hostname + ":" + port + "/api/hello"));
                ServerVersion response = await restClient.GetAsync<ServerVersion>();
                drawSystemMessage("Server version: v" + response.version);

                serverPareAuthDialog.hostname = hostname;
                serverPareAuthDialog.port = port;
                serverPareAuthDialog.WindowTitle = "Auth on " + response.name;
                serverPareAuthDialog.PopupCentered();
            }
            catch (Exception e)
            {
                drawSystemMessage("This is not a economics server: " + e.Message);
            }
        }

        public void ConnectToServer()
        {
            var realIP = IP.ResolveHostname(hostname, IP.Type.Ipv4);
            if (realIP.IsValidIPAddress())
            {
                drawSystemMessage("Try to connect to " + realIP + ":" + port);
                var error = network.CreateClient(realIP, port);
                if (error != Error.Ok)
                {
                    drawSystemMessage("Network error:" + error.ToString());
                }

                Multiplayer.NetworkPeer = network;
            }
            else
            {
                drawSystemMessage("Hostname not arreachable.");
            }
        }

        public void onConnectionFailed()
        {
            drawSystemMessage("Error cant connect.");
        }

        public void onDisconnect()
        {
            drawSystemMessage("Server disconnected.");
        }

        public void onConnected()
        {
            ownNetworkId = Multiplayer.GetNetworkUniqueId();
            drawSystemMessage("Connection established. Your id is " + ownNetworkId.ToString());
        }

        private void drawSystemMessage(string message)
        {
            GD.Print("[Client] " + message);
            (GetNode("hud/top/message") as Label).Text = message;
        }

        private void _on_connect_button_pressed()
        {
            customConnectDialog.PopupCentered();
        }

        [Puppet]
        public void CreatePuppet(int playerId, uint timestamp, Vector3 pos, Vector3 rot)
        {
            if (gameWorld != null && gameWorld.player != null && playerId != gameWorld.player.networkId)
                gameWorld.CreatePuppet(playerId, timestamp, pos, rot);
        }

        [Puppet]
        public void GetPlayerList(string playerListJson)
        {
            var playerList = Game.Networking.NetworkCompressor.Decompress<List<PlayerSyncListItem>>(playerListJson);

            if (gameWorld == null)
                return;

            foreach (var item in playerList)
            {
                gameWorld.CreatePuppet(item.playerId, item.timestamp, item.pos, item.rot);
            }

        }

        [Puppet]
        private void doHandshake(string mapName, Vector3 spawnPoint, Vector3 spawnRot)
        {
            drawSystemMessage("Handhshaking complete. Loading map: " + mapName);

            var newCoreMap = (PackedScene)ResourceLoader.Load("res://utils/world/World.tscn");
            gameWorld = (World)newCoreMap.Instance();
            gameWorld.Name = "world";
            AddChild(gameWorld);

            drawSystemMessage("Game world init");
            gameWorld.CreateLocalPlayer(ownNetworkId, spawnPoint, spawnRot, inputEnabled);

            (GetNode("hud/menu") as Control).Visible = false;
            Input.SetMouseMode(Input.MouseMode.Captured);

            RpcId(1, "playerWorldLoaded");
        }

        [PuppetSync]
        private void startPreAuth()
        {
            GD.Print("call startPreauth at " + ownNetworkId);
            drawSystemMessage("Start to pre auth with token" + accessToken);
            RpcId(1, "authPlayer", accessToken);
        }

        [PuppetSync]
        private void forceDisconnect(string message)
        {
            drawSystemMessage("Connection lost for reason:" + message);
        }

    
        private void showSystemStats()
        {
            var pos = "";
            var net_out = "";
            if (gameWorld != null && gameWorld.player != null)
            {
                pos += "x:" + Math.Round(gameWorld.player.GetPlayerPosition().x, 4);
                pos += ", y:" + Math.Round(gameWorld.player.GetPlayerPosition().y, 4);
                pos += ", z:" + Math.Round(gameWorld.player.GetPlayerPosition().z, 4);
                net_out = gameWorld.player.networkStats.getNetOut();
            }

            (GetNode("hud/top/pos") as Label).Text = "Pos: " + pos;
            (GetNode("hud/top/net_out") as Label).Text = "Net out: " + net_out;

            (GetNode("hud/top/fps_counter") as Label).Text = "FPS: " + Engine.GetFramesPerSecond();
            //(GetNode("hud/top/objects") as Label).Text = "Objects: " + GetTree().Root.GetNode("level").GetNode("properties").GetChildCount() + "/" + spow.TotalObjects();
            (GetNode("hud/top/memory") as Label).Text = "Memory: " + OS.GetStaticMemoryUsage() / 1024 / 1024 + " MB";
            (GetNode("hud/top/video_memory") as Label).Text = "VRAM: " + (Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1024 / 1024).ToString() + " MB";
        }

    }
}