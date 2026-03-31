using System.ComponentModel.DataAnnotations;

namespace TinyURL.Api.Contracts;

public sealed record CreateShortUrlRequest(
    [property: Required]
    string LongUrl);
