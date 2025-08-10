using Godot;

public partial class BattleScene : Control
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_accept")) GetTree().ChangeSceneToFile("res://scenes/OpenWorld.tscn"); // win
        if (e.IsActionPressed("ui_cancel"))  GetTree().ChangeSceneToFile("res://scenes/OpenWorld.tscn"); // flee/lose
    }
}
