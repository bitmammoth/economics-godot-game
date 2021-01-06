using Godot;
using System;
using System.Text;
using System.Collections.Generic;
using RestClient.Net;
using RestClient.Net.Abstractions;
using Game;

public class CharacterSelector : Control
{
    [Export]
    public string hostname = "localhost";

    [Export]
    public int port = 27021;

    protected string accessToken = "";

    protected List<OnlineCharacter> charList = new List<OnlineCharacter>();


    // Called when the node enters the scene tree for the first time.
    private CreateDialogWindow createDialog = null;
    public override void _Ready()
    {
        createDialog = GetNode("CreateDialog") as CreateDialogWindow;
        createDialog.Connect("OnPlayerCreated", this, "OnPlayerCreated");


        GD.Print("ready up");
    }
    private void UpdateCharList()
    {
        foreach(CharacterSelectView _child in  FindNode("char_holder").GetChildren())
        {
            FindNode("char_holder").RemoveChild(_child);
        }

        foreach (var item in charList)
        {

            var networkCharScene = (PackedScene)ResourceLoader.Load("res://utils/character/selector/CharacterSelectView.tscn");
            CharacterSelectView character = (CharacterSelectView)networkCharScene.Instance();

            FindNode("char_holder").AddChild(character);
            character.SetCharacter(item);
        }
    }
    public async void GetCharList()
    {
        try
        {
            var restClient = new RestClient.Net.Client(new RestClient.Net.NewtonsoftSerializationAdapter(), new Uri("http://" + hostname + ":" + port + "/api/characters"));
            restClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            CharReponseList response = await restClient.GetAsync<CharReponseList>();

            if (response.characters != null)
            {
                charList = response.characters;
                UpdateCharList();
            }

        }
        catch (Exception e)
        {
            GD.Print("client error: " + e.Message);
            Hide();
        }
    }

    public void SetToken(string token)
    {
        accessToken = token;
        createDialog.SetToken(accessToken);

    }

    public void onCreateButtonPressed()
    {
        createDialog.PopupCentered();
    }

    public void OnPlayerCreated(int id, string fristname, string lastname, string birthday, bool isMale)
    {
        GD.Print("On created");
        charList.Add(new OnlineCharacter { Id = id, firstname = fristname, lastname = lastname, birthday = birthday, isMale = isMale });
        createDialog.hostname = hostname;
        createDialog.port = port;

        UpdateCharList();
    }


}
