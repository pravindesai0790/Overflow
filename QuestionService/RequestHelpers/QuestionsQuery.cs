using Common;

namespace QuestionService.RequestHelpers;

public record QuestionsQuery : PaginationRequest
{
    public string? Tag { get; set; }
}