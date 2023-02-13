namespace PolitoGPT;

internal class Notification
{
    public Notification()
    {
    }

    public string AlertId { get; set; }
    public NotificationType Type { get; set; }
    public string PostId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }

}