using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

public class ChatHub : Hub
{
    private readonly string apiKey = "892204d40fbd4262b8be46dfc16b4404";

    // Diccionario para almacenar el historial de cada usuario
    private static Dictionary<string, List<Message>> chatHistories = new();

    public async Task SendMessage(string user, string message)
    {
        // Enviar el mensaje del usuario a todos los clientes en tiempo real
        await Clients.All.SendAsync("ReceiveMessage", user, message);

        // Obtener respuesta de la IA con el historial incluido
        string aiResponse = await GetAIResponse(user, message);

        // Enviar respuesta de la IA a todos los clientes
        await Clients.All.SendAsync("ReceiveMessage", "IA", aiResponse);
    }

    private async Task<string> GetAIResponse(string user, string userMessage)
    {
        var client = new RestClient("https://api.chai-research.com/v1/chat/completions");
        var request = new RestRequest("", Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("X-API_KEY", "892204d40fbd4262b8be46dfc16b4404");

        // Si no existe historial del usuario, inicializarlo
        if (!chatHistories.ContainsKey(user))
        {
            chatHistories[user] = new List<Message>();
        }

        // Agregar el mensaje del usuario al historial
        chatHistories[user].Add(new Message { role = "user", content = userMessage });

        // Construir la solicitud con el historial completo
        var requestBody = new
        {
            model = "chai_v1",
            messages = chatHistories[user]
        };

        request.AddJsonBody(JsonSerializer.Serialize(requestBody));

        var response = await client.ExecuteAsync(request);

        Console.WriteLine($"Código HTTP: {response.StatusCode}");
        Console.WriteLine($"Respuesta: {response.Content}");

        if (response.IsSuccessful)
        {
            var jsonResponse = JsonSerializer.Deserialize<ChaiResponse>(response.Content);
            string aiReply = jsonResponse?.choices?[0]?.message?.content ?? "Error al procesar la respuesta de la IA.";

            // Agregar la respuesta de la IA al historial
            chatHistories[user].Add(new Message { role = "ai", content = aiReply });

            return aiReply;
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
