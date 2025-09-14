namespace EmailApp.Entities
{
    public class Message
    {
        public int ReceiverId { get; set; }
        public AppUser Receiver { get; set; }
        public int SenderId { get; set; }
        public AppUser Sender { get; set; }
        public int MessageId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SendDate { get; set; }
        public bool is_deleted { get; set; }
        public bool is_read { get; set; }
        public bool is_important { get; set; }
        public bool IsDraft { get; set; }

    }
}
