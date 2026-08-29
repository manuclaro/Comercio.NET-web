using System.Text.Json;

namespace Comercio.NET.Pizzeria.Server.Services
{
    internal static class JsonSerializerDefaults
    {
        public static readonly JsonSerializerOptions CaseInsensitive =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }
}
