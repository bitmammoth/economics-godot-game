using Godot;
using System;

public class CharacterSelectView : Control
{
    private OnlineCharacter _char = null;
   
    public override void _Ready()
    {
        
    }

    public void SetCharacter(OnlineCharacter _tempChar)
    {
        _char = _tempChar;
        
        (FindNode("char_name") as Label).Text = _tempChar.getFullname();
        (FindNode("char_birthday") as Label).Text = "Age: " +_tempChar.Age().ToString();
        (FindNode("gender") as Label).Text = _tempChar.isMale ? "Male" : "Female";

    }


}
