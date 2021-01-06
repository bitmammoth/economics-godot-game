using Godot;
using System;

public class SpriteSky : Sprite
{
    float iTime = 0.0f;
    float iFrame = 0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }


    public override void _Process(float delta)
    {
        iTime += delta;
        iFrame += 1;
        Material.Set("shader_param/iTime", iTime);
        Material.Set("shader_param/iFrame", iFrame);

    }
    public void cov_scb(float value)

    {
        Material.Set("shader_param/COVERAGE", value / 100f);
    }

    public void absb_scb(float value)
    {
        Material.Set("shader_param/ABSORPTION", value / 10f);

    }


    public void thick_scb(float value)

    {
        Material.Set("shader_param/THICKNESS", value);

    }
    public void step_scb(float value)
    {
        Material.Set("shader_param/STEPS", value);

    }


}
