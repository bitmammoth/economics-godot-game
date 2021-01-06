using Godot;
using System;
using RCONServerLib;
using System.Net;
using Game;
public class RconManager : Node
{
    public RemoteConServer server;
    public override void _Ready()
    {
        var server = new RemoteConServer(IPAddress.Any, 27015)
        {
            SendAuthImmediately = true,
            Debug = true,
            Password = "test"
        };

        server.CommandManager.Add("players", "Player list", (command, arguments) =>
        {
            var players = GetParent().GetNode("world").GetNode("players").GetChildren();
            var str = "";

            foreach(NetworkPlayer p in players)
            {
                str += p.networkId + "\t" + p.GlobalTransform.origin.ToString();
            }

            return str;
        });

        GD.Print("start rcon server");
        server.StartListening();

    }

}
