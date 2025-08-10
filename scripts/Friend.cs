using Godot;

public partial class Friend : CharacterBody2D
{
    [Export] public float interactionRange = 100.0f;
    [Export] public CharacterBody2D player;

    private bool _playerInRange;

    public override void _Process(double delta)
    {
        if (player == null) return;

        bool isInRange = GlobalPosition.DistanceTo(player.GlobalPosition) <= interactionRange;
        if (isInRange == _playerInRange) return; // no spam

        _playerInRange = isInRange;

        var panel = GetTree().Root.GetNode<Panel>("openWorld/CanvasLayer/Panel");
        var label = panel.GetNode<Label>("Label");

        if (_playerInRange)
        {
            label.Text = "Press SPACE to interact";
            panel.Visible = true;
        }
        else
        {
            panel.Visible = false;
        }
    }
}