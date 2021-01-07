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

    [Export]
    public NodePath holderPath;

    public GridContainer charHolder;

    [Export]
    public NodePath charEditorPath;

    public CharacterEditor charEditor;

    protected string accessToken = "";

    [Signal]
    public delegate void onSelect(int id);

    protected List<OnlineCharacter> charList = new List<OnlineCharacter>();


    // Called when the node enters the scene tree for the first time.
    private CreateDialogWindow createDialog = null;
    public override void _Ready()
    {
        createDialog = GetNode("CreateDialog") as CreateDialogWindow;
        createDialog.Connect("OnPlayerCreated", this, "OnPlayerCreated");

        charHolder = GetNode(holderPath) as GridContainer;
        charEditor = GetNode(charEditorPath) as CharacterEditor;

        charEditor.Connect("onCharacterSave", this, "storeCharacter");

        GD.Print("ready up");
    }
    private void UpdateCharList()
    {
        foreach (CharacterSelectView _child in FindNode("char_holder").GetChildren())
        {
            FindNode("char_holder").RemoveChild(_child);
        }

        foreach (var item in charList)
        {

            var networkCharScene = (PackedScene)ResourceLoader.Load("res://utils/character/selector/CharacterSelectView.tscn");
            CharacterSelectView character = (CharacterSelectView)networkCharScene.Instance();
            character.Name = item.Id.ToString();
            FindNode("char_holder").AddChild(character);
            character.SetCharacter(item);

            character.Connect("onDeleteCharacter", this, "CharacterDeleteEvent");
            character.Connect("onEditCharacter", this, "CharacterEditEvent");
            character.Connect("onLaunchCharacter", this, "CharacterLaunch");
        }
    }

    public async void CharacterDeleteEvent(int id)
    {
        var _charNode = charHolder.GetNodeOrNull(id.ToString());
        if (_charNode != null)
        {
            var _char = (_charNode as CharacterSelectView).character;

            string text = "Are you realy want to delete " + _char.getFullname() + "? Attention: You cant undo this!!";

            bool result = await UIHelper.Helper.AcceptDialog("Realy want to delete character?", text);
            if (result)
                deleteCharacterRequest(_charNode as CharacterSelectView, _char.Id);

        }
    }

    private void CharacterLaunch(int id)
    {
       EmitSignal(nameof(onSelect), id);
    }

    private void storeCharacter(int id, string body)
    {
       //todo store character
    }

    private async void deleteCharacterRequest(CharacterSelectView node, int id)
    {
        try
        {
            var restClient = new RestClient.Net.Client(new RestClient.Net.NewtonsoftSerializationAdapter(), new Uri("http://" + hostname + ":" + port + "/api/deleteCharacter/" + id));
            restClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            CharDeleteMessage response = await restClient.GetAsync<CharDeleteMessage>();

            if (response.success == true)
            {
                GD.Print("Reponose succes");
                charHolder.RemoveChild(node);
                charList.RemoveAll((tf) => tf.Id == id);
            }
            else
            {
                throw new Exception(response.errorMessage);
            }
        }
        catch (Exception e)
        {
            await UIHelper.Helper.AcceptDialog("Something went wrong.", e.Message);
        }
    }

    public void CharacterEditEvent(int id)
    {
        var _charNode = charHolder.GetNodeOrNull(id.ToString());
        if (_charNode != null)
        {
            var _char = (_charNode as CharacterSelectView).character;

            charEditor.Visible = true;
            charEditor.characterId = _char.Id;
            charEditor.loadJson(_char.body);
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
