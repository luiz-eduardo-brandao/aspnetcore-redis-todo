using aspnetcore_redis_todo.Core.Entities;
using aspnetcore_redis_todo.Infrastructure.Caching;
using aspnetcore_redis_todo.Infrastructure.Persistence;
using aspnetcore_redis_todo.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace aspnetcore_redis_todo.Controllers
{
    [ApiController]
    [Route("todos")]
    public class ToDosController : ControllerBase
    {
        private readonly ICachingService _cache;
        private readonly ToDoListDbContext _context;

        public ToDosController(ICachingService cache, ToDoListDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get() 
        {
            return Ok(await _context.ToDos.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) 
        {
            var todoCache = await _cache.GetAsync(id.ToString());
            ToDo? todo;

            if(!string.IsNullOrWhiteSpace(todoCache)) 
            {
                System.Console.WriteLine("Redis Cache");

                todo = JsonConvert.DeserializeObject<ToDo>(todoCache);
                return Ok(todo);
            }

            System.Console.WriteLine("Entity Framework in Memory");

            todo = await _context.ToDos.SingleOrDefaultAsync(t => t.Id == id);

            if (todo == null) 
                return NotFound();

            await _cache.SetAsync(id.ToString(), JsonConvert.SerializeObject(todo));

            return Ok(todo);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ToDoInputModel model) 
        {
            var todo = new ToDo(0, model.Title, model.Description);

            await _context.ToDos.AddAsync(todo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { todo.Id, todo.Title }, todo);
        }
    }
}