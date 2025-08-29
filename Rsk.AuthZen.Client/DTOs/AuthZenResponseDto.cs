using System.Collections.Generic;
using System.Text.Json;

namespace Rsk.AuthZen.Client.DTOs
{
    internal class AuthZenResponseDto
    {
        public bool Decision { get; set; }
        public JsonElement Context { get; set; }
    }

    internal class AuthZenBoxcarResponseDto
    {
        public List<AuthZenResponseDto> Evaluations { get; set; }
    }
}