using Contracts;
using Marten;

namespace StatsService.MessageHandlers;

public class QuestionCreatedHandler
{
    public static async Task Handle(QuestionCreated message, IDocumentSession session, CancellationToken ct)
    {
        session.Events.StartStream(message.QuestionId, message);
        
        await session.SaveChangesAsync(ct);
    }
}