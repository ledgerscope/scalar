using System.Text.Json.Serialization;

namespace Scalar.Aspire;

/// <summary>
/// Represents the OAuth2 Client Credentials flow configuration.
/// </summary>
public sealed class ClientCredentialsFlow : OAuthFlow
{
    /// <summary>
    /// Gets or sets the token URL to be used for this flow.
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// Gets or sets the client secret used for authentication.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the location where authentication credentials should be placed in HTTP requests.
    /// </summary>
    [JsonPropertyName("x-scalar-credentials-location")]
    [JsonConverter(typeof(CredentialsLocationConverter))]
    public CredentialsLocation? CredentialsLocation { get; set; }
}