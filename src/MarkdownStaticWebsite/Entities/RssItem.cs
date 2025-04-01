namespace MarkdownStaticWebsite.Entities
{
    public class RssItem(string link, string title, string description, DateTime date)
    {
        public string Link { get; } = link;
        public string Title { get; } = title;
        public string Description { get; } = description;
        public DateTime Date { get; } = date;
    }
}
