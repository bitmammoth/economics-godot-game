using Godot;
using System;

public class Level : Spatial
{

    [Export]
    public NodePath spawnPointPath;

    public Node SpawnPoints { get; set; }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SpawnPoints = (Node)GetNode(spawnPointPath);
        addPlayer();

      
    }

    private void findFreeSpawnpoint(character newChar)
    {
        foreach (Position3D x in SpawnPoints.GetChildren())
        {
            newChar.Teleport(x.GlobalTransform.origin);
            break;
        }
    }


    private void addPlayer()
    {
        var character = (PackedScene)ResourceLoader.Load("res://character/character.tscn");
        character newChar = (character)character.Instance();
        newChar.Name = "client_1";
        newChar.level = this;
        FindNode("clients").AddChild(newChar);
        findFreeSpawnpoint(newChar);
    }
}
