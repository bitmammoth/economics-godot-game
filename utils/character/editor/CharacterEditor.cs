using Godot;
using System;
using Game;
using Newtonsoft.Json;

public class CharacterEditor : Control
{
    NetworkPlayerChar character = null;

    private float cartoonish = 1.0f;
    public override void _Ready()
    {
        //randomize();
        Godot.Collections.Array morphs = new Godot.Collections.Array();

        morphs = GetNode("morphs/Body").GetChildren();

        Godot.Collections.Dictionary<string, Color[]> Body_forms = new Godot.Collections.Dictionary<string, Color[]>();
        Godot.Collections.Dictionary<string, Color[]> Head_forms = new Godot.Collections.Dictionary<string, Color[]>();
        Godot.Collections.Dictionary<string, Color[]> Exp_forms = new Godot.Collections.Dictionary<string, Color[]>();

        foreach (CharSlider i in morphs)
        {
            i.Connect("change_morph", this, "change_morph");
            Body_forms[i.Text] = i.vertex_groups;
        }

        morphs = GetNode("morphs/Head").GetChildren();
        foreach (CharSlider i in morphs)
        {
            i.Connect("change_morph", this, "change_morph");
            Head_forms[i.Text] = i.vertex_groups;
        }

        morphs = GetNode("morphs/Face").GetChildren();

        foreach (CharSlider i in morphs)
        {
            i.Connect("change_morph", this, "change_morph");
            Head_forms[i.Text] = i.vertex_groups;
        }

        morphs = GetNode("morphs/Eyes").GetChildren();


        foreach (CharSlider i in morphs)
        {
            i.Connect("change_morph", this, "change_morph");
            Head_forms[i.Text] = i.vertex_groups;
        }


        morphs = GetNode("morphs/Exp").GetChildren();
        foreach (CharSlider i in morphs)
        {
            i.Connect("change_morph", this, "change_morph");
            Exp_forms[i.Text] = i.vertex_groups;
        }
    }

    public void SetCharacter(NetworkPlayerChar human)
    {
        character = human;
        GetNode("cam/spt").AddChild(character);
        ResetAllSliders(human.appearance);
    }

    private void change_morph(string text, float value)
    {
        character.UpdateMorph(text, value);
    }

    private void loadJson(string json)
    {
        character.appearance = JsonConvert.DeserializeObject<Godot.Collections.Dictionary<string, float>>(json);
        ResetAllSliders(character.appearance);
        character.GenerateAllMeshes();
    }
    public void _on_save_pressed()
    {
        GD.Print(JsonConvert.SerializeObject(character.appearance));
    }

    private void ResetTabSlider(Godot.Collections.Array morphs, Godot.Collections.Dictionary<string, float> apperances)
    {
        foreach (CharSlider sld in morphs)
        {
            if (apperances.ContainsKey(sld.Text))
            {
                sld.SetSlider(apperances[sld.Text]);
            }
            else
                sld.SetSlider(0);
        }
    }

    private void ResetAllSliders(Godot.Collections.Dictionary<string, float> apperances)
    {
        ResetTabSlider(GetNode("morphs/Body").GetChildren(), apperances);
        ResetTabSlider(GetNode("morphs/Head").GetChildren(), apperances);
        ResetTabSlider(GetNode("morphs/Face").GetChildren(), apperances);
        ResetTabSlider(GetNode("morphs/Eyes").GetChildren(), apperances);
        ResetTabSlider(GetNode("morphs/Exp").GetChildren(), apperances);
    }


    public void OnRandomButtonPressed()
    {
        GD.Print("do random");
        character.GenerateRandomFace(cartoonish);
        ResetTabSlider(GetNode("morphs/Head").GetChildren(), character.appearance);
        ResetTabSlider(GetNode("morphs/Face").GetChildren(), character.appearance);
        ResetTabSlider(GetNode("morphs/Eyes").GetChildren(), character.appearance);
    }

    private void setCartoonish(float value)
    {
        cartoonish = value;
    }



    /*


func _on_dress_mini_03_toggled(button_pressed) :
	if button_pressed:
		character.take_on_clothes("dress_mini_03")
	else:
		character.take_off_clothes("dress_mini_03")

func _on_male_shirt_01_toggled(button_pressed):
	if button_pressed:
		character.take_on_clothes("male_shirt_01")
	else:
		character.take_off_clothes("male_shirt_01")

func _on_male_jeans_01_toggled(button_pressed):
	if button_pressed:
		character.take_on_clothes("male_jeans_01")
	else:
		character.take_off_clothes("male_jeans_01")
*/
}
