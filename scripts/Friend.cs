using Godot;

public partial class Friend : CharacterBody2D
{
    [Export] public string conversationId = "friend_intro";

    private bool _inRange;
    private bool _waitRelease; // require key release before re-trigger

    public override void _Ready()
    {
        // Expect a child Area2D named "InteractArea" with a CollisionShape2D
        var area = GetNode<Area2D>("InteractArea");
        area.BodyEntered += OnBodyEntered;
        area.BodyExited  += OnBodyExited;
    }

    private void OnBodyEntered(Node body)
    {
        if (!body.IsInGroup("player")) return;

        _inRange = true;

        // Don’t show prompt if dialogue is already active
        if (DialogueManager.I == null || !DialogueManager.I.IsActive)
            UIRoot.I.ShowPrompt("Press SPACE to talk");
    }

    private void OnBodyExited(Node body)
    {
        if (!body.IsInGroup("player")) return;

        _inRange = false;
        UIRoot.I.HidePrompt();
    }

    public override void _Process(double delta)
    {
        if (DialogueManager.I != null && DialogueManager.I.IsActive)
            return;

        // Debounce: wait for Space release after any interaction
        if (_waitRelease)
        {
            if (!Input.IsActionPressed("interact"))
                _waitRelease = false;
            return;
        }

        if (_inRange && Input.IsActionJustPressed("interact"))
        {
            _waitRelease = true;
            UIRoot.I.HidePrompt();
            StartFriendDialogue();
        }
    }

    private void StartFriendDialogue()
    {
        if (DialogueManager.I == null)
        {
            GD.PushError("DialogueManager not found/autoloaded.");
            return;
        }

        DialogueManager.I.Start(conversationId);
        DialogueManager.I.Ended += OnDialogueEnded;
    }

    private void OnDialogueEnded()
    {
        _waitRelease = true; // require fresh Space press
        if (_inRange) UIRoot.I.ShowPrompt("Press SPACE to talk");
        DialogueManager.I.Ended -= OnDialogueEnded;
    }
}