namespace EverythingGrows
{
    // Read-only result shared by action execution and its on-screen explanation.
    internal readonly struct FarmActionHint
    {
        public bool Allowed { get; }
        public string Text { get; }
        public FarmActionHint(bool allowed, string text) { Allowed = allowed; Text = text; }
    }
}
