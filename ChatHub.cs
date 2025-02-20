using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using ChatAISystem.Models;
using System.Linq;

public class ChatHub : Hub
{
    private readonly ChatAIDBContext _dbContext;
    private readonly string apiKey = "892204d40fbd4262b8be46dfc16b4404";
    private const int MaxMessagesPerCharacter = 500; // Límite de mensajes por personaje

    public ChatHub(ChatAIDBContext context)
    {
        _dbContext = context;
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
        var httpContext = Context.GetHttpContext();
        int? lastCharacterId = httpContext.Session.GetInt32("LastCharacterId");
        string characterName = _dbContext.Characters.Find(characterId)?.Name;

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
        await Clients.All.SendAsync("ReceiveMessage", characterName, aiResponse);
    }

    /// <summary>
    /// Método para mantener solo los últimos X mensajes en la base de datos.
    /// </summary>
    private async Task EnsureMessageLimit(int userId, int characterId)
    {
        var messagesToDelete = _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderByDescending(c => c.Timestamp)
            .Skip(MaxMessagesPerCharacter)
            .ToList(); // Obtener los mensajes que exceden el límite

        if (messagesToDelete.Any())
        {
            _dbContext.Conversations.RemoveRange(messagesToDelete);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task<string> GetAIResponse(int userId, int characterId)
    {
        var client = new RestClient("https://api.chai-research.com/v1/chat/completions");
        var request = new RestRequest("", Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("X-API_KEY", apiKey);

        var messages = new List<Message>();

        // 1. Recuperar el personaje para obtener su descripción (prompt)
        var character = await _dbContext.Characters.FindAsync(characterId);
        if (character != null && !string.IsNullOrEmpty(character.Description))
        {
            messages.Add(new Message { role = "system", content = character.Description });
        }
        // Asegurar que la IA siempre responda en español
        messages.Insert(0, new Message
        {
            role = "system",
            content = "Todas las respuestas deben estar en español."
        });
        // 2. Obtener el historial desde la base de datos (hasta el límite de mensajes)
        var chatHistory = _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderBy(c => c.Timestamp)
            .Take(MaxMessagesPerCharacter) // Solo tomar los últimos mensajes
            .Select(c => new Message { role = c.Role, content = c.MessageText })
            .ToList();

        messages.AddRange(chatHistory);

        // 3. Construir la solicitud a la API
        var requestBody = new { model = "chai_v1", messages = messages };
        request.AddJsonBody(JsonSerializer.Serialize(requestBody));

        var response = await client.ExecuteAsync(request);

        if (response.IsSuccessful)
        {
            var jsonResponse = JsonSerializer.Deserialize<ChaiResponse>(response.Content);
            return jsonResponse?.choices?[0]?.message?.content ?? "Error al procesar la respuesta de la IA.";
        }
        else
        {
            return $"Error en la API: {response.StatusCode} - {response.Content}";
        }
    }
}

// Clases para deserializar la respuesta de la API
public class ChaiResponse
{
    public Choice[] choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}
