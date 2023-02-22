namespace stocktrades.Models
{
    public class User
    {
        public string id { get { return this.userId.ToString(); } }
        public string userId { get; set; }
        public string sessionId { get; set; }
    }
}
