namespace EscapeTheDark
{
    public sealed class StoryNote : GameInteraction
    {
        public string NoteId { get; private set; }
        public string Title { get; private set; }
        public string Body { get; private set; }
        public override string Prompt => "[E] Ler " + Title;

        public void Configure(string id, string title, string body)
        {
            NoteId = id;
            Title = title;
            Body = body;
        }

        public override void Interact(PlayerMotor player) => GameDirector.Instance.ReadNote(this);
    }
}
