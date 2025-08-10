using Godot;

public partial class Enemy : CharacterBody2D
{
    [Export] public PackedScene BattleScene; // drag your battle scene here
    private bool _inRange, _waitRelease;

    public override void _Ready()
    {
        var area = GetNode<Area2D>("InteractArea");
        area.BodyEntered += b => { if (b.IsInGroup("player")) { _inRange = true; UIRoot.I.ShowPrompt("Press SPACE to fight"); } };
        area.BodyExited  += b => { if (b.IsInGroup("player")) { _inRange = false; UIRoot.I.HidePrompt(); } };
    }

    public override void _Process(double delta)
    {
        if (_waitRelease) { if (!Input.IsActionPressed("interact")) _waitRelease = false; return; }
        if (_inRange && Input.IsActionJustPressed("interact"))
        {
            _waitRelease = true;
            UIRoot.I.HidePrompt();
            GetTree().ChangeSceneToPacked(BattleScene); // ← switch scenes
        }
    }
}
