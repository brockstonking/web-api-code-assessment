// /////////////////////////////////////////////////////////////////////////////
// YOU CAN FREELY MODIFY THE CODE BELOW IN ORDER TO COMPLETE THE TASK
// /////////////////////////////////////////////////////////////////////////////
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayerWebApi.Data;
using PlayerWebApi.Data.Entities;

namespace PlayerWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly PlayerDbContext _dbContext;

    public PlayerController(PlayerDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        try
        {
            List<Player> players = await _dbContext.Players.Include(p => p.PlayerSkills).ToListAsync();
            return Ok(players);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        try
        {
            Player player = await _dbContext.Players.Where(p => p.Id == id).FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound();
            }

            return Ok(player);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] Player player)
    {
        IActionResult validationResult = ValidatePlayer(player);
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            _dbContext.Add(player);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }


        return new CreatedResult($"/api/player/{player.Id}", player);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(int id, [FromBody] Player playerUpdate)
    {
        IActionResult validationResult = ValidatePlayer(playerUpdate);
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            Player player = _dbContext.Players.Include(p => p.PlayerSkills).FirstOrDefault(p => p.Id == id);

            player.Name = playerUpdate.Name;
            player.Position = playerUpdate.Position;

            List<PlayerSkill> playerSkillsList = player.PlayerSkills.ToList();

            // Removes any PlayerSkills not present in the updated version, then Updates or Adds new ones
            foreach (PlayerSkill existingSkill in playerSkillsList)
            {
                if (!playerUpdate.PlayerSkills.Any(s => s.Skill == existingSkill.Skill))
                    _dbContext.PlayerSkills.Remove(existingSkill);
            }

            foreach (PlayerSkill updatedSkill in playerUpdate.PlayerSkills)
            {
                PlayerSkill existingSkill = playerSkillsList.SingleOrDefault(s => s.Skill == updatedSkill.Skill);
                if (existingSkill != null)
                {
                    existingSkill.Value = updatedSkill.Value;
                }
                else
                {
                    PlayerSkill newSkill = new PlayerSkill
                    {
                        Skill = updatedSkill.Skill,
                        Value = updatedSkill.Value,
                        PlayerId = player.Id
                    };
                    playerSkillsList.Add(newSkill);
                }
            }

            player.PlayerSkills = playerSkillsList;


            await _dbContext.SaveChangesAsync();
            return Ok(player);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        try
        {
            Player player = _dbContext.Players.Include(p => p.PlayerSkills).FirstOrDefault(p => p.Id == id);
            if (player == null)
            {
                return NotFound();
            }

            _dbContext.Players.Remove(player);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }
        return Ok();
    }

    private IActionResult ValidatePlayer(Player player)
    {
        if (string.IsNullOrEmpty(player.Name))
        {
            return BadRequest(new { message = "Player name cannot be null or empty" });
        }

        HashSet<string> validPositions = new HashSet<string> { "defender", "midfielder", "forward" };
        if (!validPositions.Contains(player.Position))
        {
            return BadRequest(new { message = $"Invalid value for position: {player.Position}" });
        }

        if (player.PlayerSkills == null || !player.PlayerSkills.Any())
        {
            return BadRequest(new { message = "PlayerSkills must contain at least one PlayerSkill object" });
        }

        HashSet<string> validSkills = new HashSet<string> { "defense", "attack", "speed", "strength", "stamina" };
        HashSet<string> playerSkills = new HashSet<string>();

        foreach (PlayerSkill skill in player.PlayerSkills)
        {
            if (!validSkills.Contains(skill.Skill))
            {
                return BadRequest(new { message = $"Invalid skill: {skill.Skill}" });
            }
            if (skill.Value < 1 || skill.Value > 99)
            {
                return BadRequest(new { message = $"Invalid value for player skill '{skill.Skill}': {skill.Value}" });
            }
            if (!playerSkills.Add(skill.Skill))
            {
                return BadRequest(new { message = $"Player cannot have multiple skills of the same type: {skill.Skill}" });
            }
        }

        return null;
    }
}
