using System.Net;
using System.Net.Http.Headers;
using SyWater.Devices.Application.Ports.Out;

namespace SyWater.Devices.Infrastructure.Places;

/// <summary>
/// Outbound adapter: asks ms-places "GET /api/places/{id}" WITH THE USER'S OWN TOKEN.
/// ms-places answers 200 only for the owner and 404 for everybody else, so we reuse its
/// ownership rule instead of reading the places schema (ADR-002 rule 4).
/// </summary>
public sealed class HttpPlaceOwnershipChecker(HttpClient http, IAccessTokenProvider tokens) : IPlaceOwnershipChecker
{
    public const string ServiceName = "place-service";

    public async Task<bool> IsOwnedByRequesterAsync(Guid placeId, CancellationToken ct)
    {
        var token = tokens.GetAccessToken()
                    ?? throw new InvalidOperationException("There is no access token in the current request.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/places/{placeId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => true,
                HttpStatusCode.NotFound => false,
                _ => throw new ExternalServiceUnavailableException(ServiceName,
                    new HttpRequestException($"Unexpected status {(int)response.StatusCode}.")),
            };
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceUnavailableException(ServiceName, ex);   // connection refused, DNS...
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ExternalServiceUnavailableException(ServiceName, ex);   // HttpClient timeout
        }
    }
}
