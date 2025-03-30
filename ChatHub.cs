using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using ChatAISystem.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ChatAISystem.Models;
using Microsoft.AspNetCore.Http;

public class ChatHub : Hub
{
    private readonly ChatAIDBContext _dbContext;
    private readonly string _apiKey;
    private const int MaxMessagesPerCharacter = 1000; // Límite de mensajes por personaje
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHub(ChatAIDBContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = context;
        _apiKey = configuration["ApiKeys:Gemini"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey no puede ser nulo");
        _httpContextAccessor = httpContextAccessor;
    }
    //Carga mensajes paginados para el historial de chat.

    public async Task LoadChatHistory(int userId, int characterId, int page, int pageSize)
    {
        try
        {
            // Validar parámetros
            if (userId <= 0 || characterId <= 0 || page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("Parámetros no válidos");
            }

            int skipMessages = (page - 1) * pageSize;

            var messages = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderByDescending(c => c.Timestamp)
                .Skip(skipMessages)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Role,
                    c.MessageText,
                    Timestamp = c.Timestamp.ToUniversalTime().ToString("o")
                })
                .ToListAsync();

            var jsonMessages = JsonSerializer.Serialize(messages);
            await Clients.Caller.SendAsync("LoadChatHistory", jsonMessages);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar el historial: {ex.Message}");
            await Clients.Caller.SendAsync("LoadChatHistoryError", ex.Message);
        }
    }



    public async Task SendMessage(int userId, int characterId, string message)
    {
        // Obtener el usuario de la base de datos
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Error: Usuario no encontrado.");
            return;
        }
        // Obtener el HttpContext para acceder a la sesión desde el hub
        var httpContext = _httpContextAccessor.HttpContext;
        int? lastCharacterId = httpContext?.Session.GetInt32("LastCharacterId");
        string? characterName = _dbContext.Characters.Find(characterId)?.Name;

        // Guardar el mensaje del usuario en la base de datos
        var userMessage = new Conversation
        {
            UserId = userId,
            CharacterId = characterId,
            Role = "user",
            MessageText = message,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(userMessage);
        await _dbContext.SaveChangesAsync();

        // Eliminar mensajes antiguos si exceden el límite
        await EnsureMessageLimit(userId, characterId);

        // Enviar el mensaje del usuario a los clientes
        await Clients.All.SendAsync("ReceiveMessage", user.Username, message);

        // Obtener respuesta de la IA con historial
        string aiResponse = await GetAIResponse(userId, characterId);

        // Guardar la respuesta de la IA en la base de datos
        var aiMessage = new Conversation
        {
            UserId = userId,
            CharacterId = characterId,
            Role = "ai",
            MessageText = aiResponse,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Conversations.Add(aiMessage);
        await _dbContext.SaveChangesAsync();

        // Eliminar mensajes antiguos si exceden el límite nuevamente
        await EnsureMessageLimit(userId, characterId);

        // Enviar la respuesta de la IA a los clientes
        await Clients.All.SendAsync("ReceiveMessage", "IA", aiResponse);
    }

    /// <summary>
    /// Método para mantener solo los últimos X mensajes en la base de datos.
    /// </summary>
    private async Task EnsureMessageLimit(int userId, int characterId)
    {
        var messagesToDelete = await _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderByDescending(c => c.Timestamp)
            .Skip(MaxMessagesPerCharacter)
            .ToListAsync(); // Obtener los mensajes que exceden el límite

        if (messagesToDelete.Any())
        {
            _dbContext.Conversations.RemoveRange(messagesToDelete);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task<string> GetAIResponse(int userId, int characterId)
    {
        try
        {
            // 1. Recuperar la descripción del personaje y el historial del chat
            var messages = new List<APIMessage.Message>();

            // Obtener el personaje para su prompt inicial
            var character = await _dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == characterId);
            if (character?.Description != null)
            {
                // Creamos un prompt más detallado que incluya instrucciones sobre el formato de acciones/emociones
                string enhancedPrompt = $@"Actúa como el siguiente personaje: {character.Description}

            Instrucciones adicionales:
            1. Mantente siempre fiel a la personalidad, conocimientos y estilo de habla del personaje.
            2. Muestra emociones, acciones físicas o expresiones corporales entre asteriscos. Ejemplo: *sonríe* o *mira pensativamente por la ventana*.
            3. Integra estas acciones de forma natural en tu respuesta, no las agregues solo al final.
            4. Nunca salgas del personaje ni menciones que eres una IA.
            5. Responde de manera concisa y directa, como lo haría este personaje en una conversación real.";

                // Usamos el rol "system" para dar instrucciones iniciales
                messages.Add(new APIMessage.Message { role = "system", content = enhancedPrompt });
            }

            // Obtener el historial de conversación (hasta 50 mensajes, por ejemplo)
            var chatHistory = await _dbContext.Conversations.AsNoTracking()
                .Where(c => c.UserId == userId && c.CharacterId == characterId)
                .OrderBy(c => c.Timestamp)
                .Select(c => new APIMessage.Message { role = c.Role, content = c.MessageText })
                .ToListAsync();

            messages.AddRange(chatHistory);

            // Combinar el prompt y el historial en un solo texto o enviarlos separados, según lo requiera la API.
            string combinedPrompt = string.Join("\n", messages.Select(m => $"{m.role}: {m.content}"));

            // 2. Configurar el cliente y el endpoint para la API Gemini
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            using var client = new RestClient(endpoint);
            var request = new RestRequest("", Method.Post)
            {
                Timeout = TimeSpan.FromSeconds(15) // 15 segundos de timeout
            };
            request.AddHeader("Content-Type", "application/json");

            // 3. Construir el cuerpo de la solicitud
            var requestBody = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text = combinedPrompt }
                    }
                }
            }
            };
            request.AddJsonBody(requestBody);

            // 4. Ejecutar la solicitud y procesar la respuesta
            var response = await client.ExecuteAsync(request);
            Console.WriteLine("Respuesta cruda de Gemini: " + response.Content);

            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                var geminiResponse = JsonSerializer.Deserialize<APIMessage.GeminiResponse>(response.Content);
                if (geminiResponse?.candidates != null && geminiResponse.candidates.Any())
                {
                    var candidate = geminiResponse.candidates.FirstOrDefault();
                    if (candidate?.content?.parts != null && candidate.content.parts.Any())
                    {
                        var outputText = candidate.content.parts.First().text;
                        if (!string.IsNullOrEmpty(outputText))
                        {
                            // Asegurarse de que la respuesta venga del personaje (eliminar prefijos como "assistant:")
                            if (outputText.StartsWith("assistant:", StringComparison.OrdinalIgnoreCase))
                            {
                                outputText = outputText.Substring("assistant:".Length).Trim();
                            }

                            return outputText;
                        }
                    }
                }
                return "Error: La respuesta de la IA no contiene mensajes válidos.";
            }
            else
            {
                return $"Error en la API: {response.StatusCode} - {response.Content}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAIResponse: {ex.Message}");
            return "Error al obtener respuesta de la IA.";
        }
    }

}
