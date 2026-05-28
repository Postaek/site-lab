namespace site.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public string UserId { get; set; }  
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
