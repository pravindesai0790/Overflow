using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext db) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto questionDetails)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if(userId is null || name is null) return BadRequest("Cannot get user details");

        var question = new Question
        {
            Title = questionDetails.Title,
            Content = questionDetails.Content,
            TagSlugs = questionDetails.Tags,
            AskerId = userId,
            AskerDisplayName = name
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        
        return Created($"Questions/{question.Id}", question);
    }
}