using System.Security.Claims;
using Common;
using Contracts;
using FastExpressionCompiler;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using QuestionService.RequestHelpers;
using QuestionService.Services;
using Reputation;
using Wolverine;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext db, IMessageBus bus, TagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationResult<Question>>> GetQuestions([FromQuery] QuestionsQuery q)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(q.Tag))
        {
            query = query.Where(x => x.TagSlugs.Contains(q.Tag));
        }

        query = q.Sort switch
        {
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            "active" => query.OrderByDescending(x => new[]
            {
                x.CreatedAt,
                x.UpdatedAt ?? DateTime.MinValue,
                x.Answers.Max(a => (DateTime?)a.CreatedAt) ?? DateTime.MinValue,
                x.Answers.Max(a => a.UpdatedAt) ?? DateTime.MinValue
            }.Max()),
            "unanswered" => query.Where(x => x.AnswerCount == 0).OrderByDescending(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var result = await query.ToPaginatedListAsync(q);

        return result;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await db.Questions
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == id);
        
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
        if(!await tagService.AreTagsValidAsync(questionDetails.Tags))
            return BadRequest("Invalid tags");
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        
        if(userId is null || name is null) return BadRequest("Cannot get user details");

        var sanitizer = new HtmlSanitizer();

        var question = new Question
        {
            Title = questionDetails.Title,
            Content =sanitizer.Sanitize(questionDetails.Content),
            TagSlugs = questionDetails.Tags,
            AskerId = userId
        };

        await using var tx = await db.Database.BeginTransactionAsync();
        
        try
        {
            db.Questions.Add(question);
        
            await db.SaveChangesAsync();
            
            await bus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content,
                question.CreatedAt, question.TagSlugs));
            
            await tx.CommitAsync(); 
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await tx.RollbackAsync();
            throw;
        }

        var slugs = question.TagSlugs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (slugs.Length > 0)
        {
            await db.Tags
                .Where(t => ((IEnumerable<string>)slugs).Contains(t.Slug))
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.UsageCount, 
                    t => t.UsageCount + 1));
        }
        
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
        
        if(!await tagService.AreTagsValidAsync(updateDetails.Tags))
            return BadRequest("Invalid tags");
        
        var orignal = question.TagSlugs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var incoming = updateDetails.Tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var removed = orignal.Except(incoming, StringComparer.OrdinalIgnoreCase).ToArray();
        var added = incoming.Except(orignal, StringComparer.OrdinalIgnoreCase).ToArray();
        
        var sanitizer = new HtmlSanitizer();
        
        question.Title = updateDetails.Title;
        question.Content = sanitizer.Sanitize(updateDetails.Content);
        question.TagSlugs = updateDetails.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (removed.Length > 0)
        {
            await db.Tags
                .Where(t => Enumerable.Contains(removed, t.Slug) && t.UsageCount > 0)
                .ExecuteUpdateAsync(u => 
                    u.SetProperty(t => t.UsageCount, t => t.UsageCount - 1));
        }
        
        if (added.Length > 0)
        {
            await db.Tags
                .Where(t => Enumerable.Contains(added, t.Slug))
                .ExecuteUpdateAsync(u => 
                    u.SetProperty(t => t.UsageCount, t => t.UsageCount + 1));
        }

        await bus.PublishAsync(new QuestionUpdated(question.Id, question.Title, question.Content, 
            question.TagSlugs.AsArray()));
        
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

        await bus.PublishAsync(new QuestionDeleted(question.Id));
        
        return NoContent();
    }

    [Authorize]
    [HttpPost("{questionId}/answers")]
    public async Task<ActionResult> PostAnswer(string questionId, CreateAnswerDto answerDetails)
    {
        var question = await db.Questions.FindAsync(questionId);

        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");

        if (userId is null || name is null) return BadRequest("Cannot get user details");
        
        var sanitizer = new HtmlSanitizer();

        var answer = new Answer
        {
            Content = sanitizer.Sanitize(answerDetails.Content),
            UserId = userId,
            QuestionId = questionId
        };
        
        question.Answers.Add(answer);
        question.AnswerCount++;

        await db.SaveChangesAsync();

        await bus.PublishAsync(new AnswerCountUpdated(questionId, question.AnswerCount));
        
        return Created($"/questions/{questionId}", answer);
    }

    [Authorize]
    [HttpPut("{questionId}/answers/{answerId}")]
    public async Task<ActionResult> UpdateAnswer(string questionId, string answerId, CreateAnswerDto answerDetails)
    {
        var answer = await db.Answers.FindAsync(answerId);
        if (answer is null) return NotFound();
        if (answer.QuestionId != questionId) return BadRequest("Cannot update answer details");
        
        var sanitizer = new HtmlSanitizer();

        answer.Content = sanitizer.Sanitize(answerDetails.Content);
        answer.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
        
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{questionId}/answers/{answerId}")]
    public async Task<ActionResult> DeleteAnswer(string questionId, string answerId)
    {
        var answer = await db.Answers.FindAsync(answerId);
        var question = await db.Questions.FindAsync(questionId);
        if (answer is null || question is null) return NotFound();
        if (answer.QuestionId != questionId || answer.Accepted) return BadRequest("Cannot delete the answer");

        db.Answers.Remove(answer);
        question.AnswerCount--;
        
        await db.SaveChangesAsync();
        
        await bus.PublishAsync(new AnswerCountUpdated(questionId, question.AnswerCount));
        
        return NoContent();
    }

    [Authorize]
    [HttpPost("{questionId}/answers/{answerId}/accept")]
    public async Task<ActionResult> AcceptAnswer(string questionId, string answerId)
    {
        var answer = await db.Answers.FindAsync(answerId);
        var question = await db.Questions.FindAsync(questionId);
        if (answer is null || question is null) return NotFound();
        if (answer.QuestionId != questionId || question.HasAcceptedAnswer) return BadRequest("Cannot accept the answer");

        answer.Accepted = true;
        question.HasAcceptedAnswer = true;

        await db.SaveChangesAsync();
        
        await bus.PublishAsync(new AnswerAccepted(questionId));
        await bus.PublishAsync(ReputationHelper.MakeEvent(answer.UserId, ReputationReason.AnswerAccepted, question.AskerId));
        
        return NoContent();
    }
}