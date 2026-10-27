using Application.Propostas.ObterProposta;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/propostas")]
public sealed class PropostasController : ControllerBase
{
    private readonly ObterPropostaHandler _obter;

    public PropostasController(ObterPropostaHandler obter)
    {
        _obter = obter;
    }

    [HttpGet("{clienteId:guid}")]
    [ProducesResponseType(typeof(PropostaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorClienteId(
        Guid clienteId,
        CancellationToken cancellationToken)
    {
        var result = await _obter.HandleAsync(clienteId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
