using DarkStar.Api.Contracts;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoController(CryptoApplicationService service) : ControllerBase
{
    [HttpPost("encrypt-text")]
    [ProducesResponseType(typeof(EncryptTextApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncryptTextApiResponse>> EncryptText(
        [FromBody] EncryptTextApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.EncryptTextAsync(
                new EncryptTextRequest(request.Plaintext, request.Passphrase, request.Algorithm),
                cancellationToken
            );

            return Ok(new EncryptTextApiResponse(result.Algorithm, result.CiphertextBase64));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("decrypt-text")]
    [ProducesResponseType(typeof(DecryptTextApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecryptTextApiResponse>> DecryptText(
        [FromBody] DecryptTextApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.DecryptTextAsync(
                new DecryptTextRequest(request.CiphertextBase64, request.Passphrase, request.Algorithm),
                cancellationToken
            );

            return Ok(new DecryptTextApiResponse(result.Algorithm, result.Plaintext));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Ciphertext must be valid base64" });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return BadRequest(new { error = "Decryption failed" });
        }
    }

    [HttpPost("sign-text")]
    [ProducesResponseType(typeof(SignTextApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignTextApiResponse>> SignText(
        [FromBody] SignTextApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SignTextAsync(
                new SignTextRequest(request.Message, request.Secret),
                cancellationToken
            );

            return Ok(new SignTextApiResponse(result.SignatureBase64));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("verify-text")]
    [ProducesResponseType(typeof(VerifyTextApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyTextApiResponse>> VerifyText(
        [FromBody] VerifyTextApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.VerifyTextAsync(
                new VerifyTextRequest(request.Message, request.Secret, request.SignatureBase64),
                cancellationToken
            );

            return Ok(new VerifyTextApiResponse(result.IsValid));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("encrypt-file")]
    [ProducesResponseType(typeof(FileCryptoApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileCryptoApiResponse>> EncryptFile(
        [FromBody] EncryptFileApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.EncryptFileAsync(
                new EncryptFileRequest(
                    request.InputPath,
                    request.OutputPath,
                    request.Passphrase,
                    request.Algorithm
                ),
                cancellationToken
            );

            return Ok(new FileCryptoApiResponse(
                result.Algorithm,
                result.InputPath,
                result.OutputPath,
                result.InputBytes,
                result.OutputBytes
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("decrypt-file")]
    [ProducesResponseType(typeof(FileCryptoApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileCryptoApiResponse>> DecryptFile(
        [FromBody] DecryptFileApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.DecryptFileAsync(
                new DecryptFileRequest(
                    request.InputPath,
                    request.OutputPath,
                    request.Passphrase,
                    request.Algorithm
                ),
                cancellationToken
            );

            return Ok(new FileCryptoApiResponse(
                result.Algorithm,
                result.InputPath,
                result.OutputPath,
                result.InputBytes,
                result.OutputBytes
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return BadRequest(new { error = "Decryption failed" });
        }
    }
}
