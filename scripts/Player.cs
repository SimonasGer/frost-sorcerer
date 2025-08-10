using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 220f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 dir = Vector2.Zero;

		if (Input.IsActionPressed("move_right")) dir.X += 1;
		if (Input.IsActionPressed("move_left"))  dir.X -= 1;
		if (Input.IsActionPressed("move_down"))  dir.Y += 1;
		if (Input.IsActionPressed("move_up"))    dir.Y -= 1;

		Velocity = dir.Normalized() * Speed;
		MoveAndSlide();
	}
}
