using Microsoft.AspNetCore.Mvc;
using RFIDInventario.Server.Models;
using RFIDInventario.Server.Services;

namespace RFIDInventario.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioTiendaController : ControllerBase
    {
        private readonly UsuarioTiendaService _usuarioTiendaService;

        public UsuarioTiendaController(UsuarioTiendaService usuarioTiendaService)
        {
            _usuarioTiendaService = usuarioTiendaService;
        }

        // 🔹 GET: api/UsuarioTienda/usuario/6
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerTiendasPorUsuario(int usuarioId)
        {
            var tiendas = await _usuarioTiendaService.ObtenerTiendasPorUsuario(usuarioId);
            return Ok(tiendas);
        }

        // 🔹 POST: api/UsuarioTienda/asignar
        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarTienda([FromBody] AsignarTiendaDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.TiendaCodigo))
                return BadRequest("Datos incompletos.");

            var result = await _usuarioTiendaService.AsignarTienda(
                dto.UsuarioId,
                dto.TiendaCodigo,
                dto.RolAsignado ?? "User",
                dto.AsignadoPorId
            );

            return Ok(result);
        }

        // 🔹 DELETE: api/UsuarioTienda/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarAsignacion(int id)
        {
            var ok = await _usuarioTiendaService.EliminarAsignacion(id);
            if (!ok)
                return NotFound(new { message = "No se encontró la asignación." });

            return NoContent();
        }

        // 🔹 DELETE: api/UsuarioTienda/correo?correoUsuario=user@correo.com&tiendaCodigo=T001
        [HttpDelete("correo")]
        public async Task<IActionResult> EliminarAsignacionPorCorreo(
            [FromQuery] string correoUsuario,
            [FromQuery] string? tiendaCodigo = null)
        {
            if (string.IsNullOrEmpty(correoUsuario))
                return BadRequest("Debe especificar el correo del usuario.");

            var ok = await _usuarioTiendaService.EliminarAsignacionPorCorreo(correoUsuario, tiendaCodigo);
            if (!ok)
                return NotFound(new { message = "No se encontró ninguna asignación para ese usuario." });

            return NoContent();
        }
    }

    // 🔸 DTO limpio para evitar exponer campos innecesarios
    public class AsignarTiendaDto
    {
        public int UsuarioId { get; set; }
        public string TiendaCodigo { get; set; } = string.Empty;
        public string? RolAsignado { get; set; }
        public int? AsignadoPorId { get; set; }
    }
}
