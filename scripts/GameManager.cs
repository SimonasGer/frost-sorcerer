using Godot;

public partial class GameManager : Node
{
    public override void _Ready()
    {
        DialogueManager.I.LoadFromJson("res://dialogue/friend.json");
    }

}