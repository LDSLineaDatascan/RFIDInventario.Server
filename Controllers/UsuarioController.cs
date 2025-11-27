using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RFIDInventario.Server.Models;
using RFIDInventario.Server.Services;
using RFIDInventario.Server.Hubs;

namespace RFIDInventario.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public UsuariosController(UsuarioService usuarioService, IHubContext<NotificationHub> hubContext)
        {
            _usuarioService = usuarioService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<Usuario>>> GetAllUsuarios()
        {
            var usuarios = await _usuarioService.GetAllUsuariosAsync();
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuarioById(int id)
        {
            var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        [HttpGet("correo/{correo}")]
        public async Task<ActionResult<Usuario>> GetUsuarioByCorreo(string correo)
        {
            var usuario = await _usuarioService.GetUsuarioByCorreoAsync(correo);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        [HttpPost]
        public async Task<ActionResult<Usuario>> CreateUsuario([FromBody] Usuario usuario)
        {
            var createdUsuario = await _usuarioService.AddUsuarioAsync(usuario);
            await _hubContext.Clients.All.SendAsync("UsuarioCreado", createdUsuario);
            return CreatedAtAction(nameof(GetUsuarioById), new { id = createdUsuario.Id }, createdUsuario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] Usuario usuario)
        {
            if (id != usuario.Id) return BadRequest();
            var result = await _usuarioService.UpdateUsuarioAsync(usuario);
            if (!result) return NotFound();
            await _hubContext.Clients.All.SendAsync("UsuarioActualizado", usuario);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var result = await _usuarioService.DeleteUsuarioAsync(id);
            if (!result) return NotFound();
            await _hubContext.Clients.All.SendAsync("UsuarioEliminado", id);
            return NoContent();
        }

        [HttpPost("Login-federado")]   
        public async Task<IActionResult> LoginFederado([FromBody] Usuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Correo))
                return BadRequest("Correo requerido");

            var existingUsuario = await _usuarioService.GetUsuarioByCorreoAsync(usuario.Correo);
            if (existingUsuario != null)
            {
                // usuario si existe
                return Ok(existingUsuario);
            }
            else
            {
                // usuaro nuevo si no existe
                var newUsuario = new Usuario
                {
                    Correo = usuario.Correo,
                    Nombre = usuario.Nombre,
                    Rol = "NoAutorizado",//rol por defecto y seguridad
                    Activo = false,
                    FechaCreacion = DateTime.Now,
                    FechaActualizado = DateTime.Now
                };
                var createdUsuario = await _usuarioService.AddUsuarioAsync(newUsuario);
                await _hubContext.Clients.All.SendAsync("UsuarioCreado", createdUsuario);
                return CreatedAtAction(nameof(GetUsuarioById), new { id = createdUsuario.Id }, createdUsuario);
            }
        }
    }
}
