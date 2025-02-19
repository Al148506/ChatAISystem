using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using ChatAISystem.Models;
using System.Net.NetworkInformation;

public class ChatHub : Hub
{
    private readonly ChatAIDBContext _dbContext;
    private readonly string apiKey = "892204d40fbd4262b8be46dfc16b4404";

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

        // Guardar mensaje del usuario en la base de datos
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

        // Enviar el mensaje del usuario a los clientes
        await Clients.All.SendAsync("ReceiveMessage", user.Username, message);

        // Obtener respuesta de la IA con historial
        string aiResponse = await GetAIResponse(userId, characterId);

        // Guardar respuesta de la IA en la base de datos
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

        // Enviar la respuesta de la IA a los clientes
        await Clients.All.SendAsync("ReceiveMessage", "IA", aiResponse);
    }

    private async Task<string> GetAIResponse(int userId, int characterId)
    {
        var client = new RestClient("https://api.chai-research.com/v1/chat/completions");
        var request = new RestRequest("", Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("X-API_KEY", apiKey);

        // Obtener historial desde la base de datos (últimos 10 mensajes)
        var chatHistory = _dbContext.Conversations
            .Where(c => c.UserId == userId && c.CharacterId == characterId)
            .OrderBy(c => c.Timestamp)
            .Take(10)
            .Select(c => new Message { role = c.Role, content = c.MessageText })
            .ToList();

        var requestBody = new
        {
            model = "chai_v1",
            messages = chatHistory
        };

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
