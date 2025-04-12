using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Shared.Enums.Todo;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Host.Controllers.Todo
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [FeatureRequirement("TodoApp")]
    public class TasksController : ControllerBase
    {
        private readonly ITodoTaskService _todoTaskService;

        public TasksController(ITodoTaskService todoTaskService)
        {
            _todoTaskService = todoTaskService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<TodoTaskDto>>> GetTasks([FromQuery] TaskFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If user is admin with ViewAllTodos permission, they should be able to view all tasks
            // This should be handled in the service layer
            var result = await _todoTaskService.GetTasksAsync(userId, filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "TodoOwnerPolicy")]
        public async Task<ActionResult<TodoTaskDto>> GetTask(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _todoTaskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TodoTaskDto>> CreateTask(CreateTodoTaskDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _todoTaskService.CreateTaskAsync(userId, dto);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "TodoOwnerPolicy")]
        public async Task<ActionResult<TodoTaskDto>> UpdateTask(Guid id, UpdateTodoTaskDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _todoTaskService.UpdateTaskAsync(id, userId, dto);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "TodoOwnerPolicy")]
        public async Task<ActionResult> DeleteTask(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _todoTaskService.DeleteTaskAsync(id, userId);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Policy = "TodoOwnerPolicy")]
        public async Task<ActionResult<TodoTaskDto>> UpdateTaskStatus(Guid id, [FromBody] TodoTaskStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _todoTaskService.ChangeTaskStatusAsync(id, userId, status);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<TaskStatisticsDto>> GetStatistics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var statistics = await _todoTaskService.GetTaskStatisticsAsync(userId);
            return Ok(statistics);
        }

        [HttpGet("all")]
        [Authorize(Policy = "CanViewAllTodos")]
        public async Task<ActionResult<PagedResponse<TodoTaskDto>>> GetAllTasks([FromQuery] TaskFilterDto filter)
        {
            // Special admin mode - pass null for userId to get all tasks
            var result = await _todoTaskService.GetAllTasksAsync(filter);
            return Ok(result);
        }
    }
}