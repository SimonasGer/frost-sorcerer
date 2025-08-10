using Godot;

public partial class Friend : CharacterBody2D
{
    [Export] public float interactionRange = 50.0f;
    [Export] public CharacterBody2D player;
    [Export] public string conversationId = "friend_intro";

    private bool _inRange;
    private bool _waitRelease; // blocks re-start until Space is released

    public override void _Process(double delta)
    {
        if (player == null) return;

        // While dialogue is active, do nothing
        if (DialogueManager.I != null && DialogueManager.I.IsActive) return;

        // Debounce: if we're waiting for Space to be released, bail out
        if (_waitRelease)
        {
            if (!Input.IsActionPressed("interact"))
                _waitRelease = false; // released now; ready to accept a fresh press
            else
                return;
        }

        bool nowInRange = GlobalPosition.DistanceTo(player.GlobalPosition) <= interactionRange;
        if (nowInRange != _inRange)
        {
            _inRange = nowInRange;
            if (_inRange) UIRoot.I.ShowPrompt("Press SPACE to interact");
            else UIRoot.I.HidePrompt();
        }

        if (_inRange && Input.IsActionJustPressed("interact"))
        {
            _waitRelease = true;       // block immediate re-trigger
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
        _waitRelease = true; // require a fresh Space press after closing
        if (_inRange) UIRoot.I.ShowPrompt("Press SPACE to interact");
        DialogueManager.I.Ended -= OnDialogueEnded;
    }
}