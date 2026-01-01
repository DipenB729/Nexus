namespace AngularApp4.Model
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Task { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}
