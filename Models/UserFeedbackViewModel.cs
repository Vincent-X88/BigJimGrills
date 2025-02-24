using devDynast.Models;

namespace devDynast.ViewModels
{
    public class UserFeedbackViewModel
    {
        public int FeedbackId { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string OrderNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdminResponded { get; set; }
        public string? AdminResponse { get; set; }
    }
}
