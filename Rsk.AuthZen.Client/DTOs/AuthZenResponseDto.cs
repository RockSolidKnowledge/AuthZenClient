using System.Text.Json;

namespace Rsk.AuthZen.Client.DTOs
{
    public class AuthZenResponseDto
    {
        public bool Decision { get; set; }
        public JsonElement Context { get; set; }
    }
}