using System.Threading;
using PolitoGPT;

var chat = new ChatGPT();
var polito = new Polito();

var userThreadIgnore = new Dictionary<string, string>()
{
    ["19283"] = "1666707"
};

void Log(string messagem)
{
    Console.WriteLine(messagem);
}

for(; ; )
{
    Loop().Wait();
    Thread.Sleep(10_000);
}

async Task Loop()
{
    var allNotifications = await polito.GetNotifications();

    Log($"{allNotifications.Length} notifications found");

    foreach(var notification in allNotifications)
    {
        var postId = notification.PostId;
        var alertId = notification.AlertId;
        var type = notification.Type;

        try
        {
            Log($"Getting value for post {postId}, alert {alertId}");

            var post = await polito.GetPostValue(postId, type);

            var content = post.FinalContent;
            var html = post.PostHtmlDecode;
            var xfToken = post.Token;
            var threadId = post.ThreadId;

            if(ShouldIgnoreNotification(notification, post))
            {
                Log($"Skiping notification from {notification.UserName}");
                continue;
            }

            Log($"Marking post {postId} as unread");

            await polito.MarkAlertAsUnread(alertId, xfToken);

            Log($"Getting completion for message: {content}");
            var answer = await chat.GetCompletion(content);

            string answerHtml = AddChatAnswerToHtml(html, answer);

            Log($"Replying thread {threadId}, anwser: {answer.GetFirstChoiceText()}");
            await polito.Reply(threadId, xfToken, answerHtml);

            Log($"Marking post {postId} as read");
            await polito.MarkAlertAsRead(alertId, xfToken);

            Log($"Mention {alertId} finished succefully!");

        }
        catch(Exception ex)
        {
            Log(ex.Message);
        }
    }

}

bool ShouldIgnoreNotification(Notification notification, Post post)
{
    var cleanThreadId = post.ThreadId.Split(".").Last();

    return userThreadIgnore.Contains(new(notification.UserId, cleanThreadId));
}

static string AddChatAnswerToHtml(string html, CompletionResponse answer)
{
    return html.Replace("<p></p>", $"<p>{answer.GetFirstChoiceText()}</p>");
}