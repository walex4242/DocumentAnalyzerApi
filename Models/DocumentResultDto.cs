public class DocumentResultDto
{
    public string ModelUsed { get; set; }
    public string DocumentType { get; set; }
    public Dictionary<string, string> Fields { get; set; } = new();
    public List<Dictionary<string, string>> Items { get; set; }
}
