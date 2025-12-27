using System.Security.Claims;
using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using Wolverine;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext db, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Question>>> GetQuestions(string? tags)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(tags))
        {
            query = query.Where(x => x.TagSlugs.Contains(tags));
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await db.Questions.FindAsync(id);
        
        if(question is null) return NotFound();

        await db.Questions.Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount,
                x => x.ViewCount + 1));
        
        return question;
    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto questionDetails)
    {
        var validTags = await db.Tags.Where(x => questionDetails.Tags.Contains(x.Slug)).ToListAsync();
        
        var missingTags = questionDetails.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();

        if (missingTags.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missingTags)}");
        
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
        
        await bus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content
            , question.CreatedAt, question.TagSlugs));
        
        return Created($"Questions/{question.Id}", question);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto updateDetails)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();
        
        var validTags = await db.Tags.Where(x => updateDetails.Tags.Contains(x.Slug)).ToListAsync();
        
        var missingTags = updateDetails.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();

        if (missingTags.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missingTags)}");
        
        question.Title = updateDetails.Title;
        question.Content = updateDetails.Content;
        question.TagSlugs = updateDetails.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();

        db.Questions.Remove(question);
        await db.SaveChangesAsync();
        return NoContent();
    }
}