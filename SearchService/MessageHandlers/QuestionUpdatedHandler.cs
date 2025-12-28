using System.Text.RegularExpressions;
using Contracts;
using SearchService.Models;
using Typesense;

namespace SearchService.MessageHandlers;

public partial class QuestionUpdatedHandler(ITypesenseClient client)
{
    public async Task HandleAsync(QuestionUpdated message)
    {
        await client.UpdateDocument("questions", message.QuestionId, new
        {
            message.Title,
            Content = StripHtml(message.Content),
            message.Tags
        });
    }
    
    private static string StripHtml(string htmlContent)
    {
        return MyRegex().Replace(htmlContent, string.Empty);
    }

    [GeneratedRegex(@"<.*?>")]
    private static partial Regex MyRegex();
}