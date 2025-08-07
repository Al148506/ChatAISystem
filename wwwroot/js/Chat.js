// Conexión con SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        accessTokenFactory: async () => {
            const response = await fetch('/api/negotiate');
            const data = await response.json();

            console.log("Token recibido:", data.accessToken); // ✅ Depuración extra
            return data.accessToken || '';
        }
    })
    .withAutomaticReconnect()
    .build();


connection.start().then(() => {
    console.log("✅ Conectado a SignalR");
}).catch(err => console.error("❌ Error al conectar con SignalR:", err.toString()));


let userId = parseInt(document.getElementById("userId").value); // ID del usuario
let characterId = null; // ID del personaje seleccionado
let page = 1; // Página de mensajes cargados
let loading = false; // Evita múltiples cargas al mismo tiempo

// Función para seleccionar un personaje de la sidebar
function selectCharacter(id, name) {
    characterId = parseInt(id, 10);
    document.getElementById("characterId").value = id;
    document.getElementById("chatTitle").innerText = `Chat with ${name}`;

    let messageInput = document.getElementById("messageInput");
    messageInput.disabled = false;
    messageInput.placeholder = `Write a message for ${name}...`;

    document.getElementById("sendButton").disabled = false; // ✅ ACTIVA el botón

    page = 1;
    document.getElementById("chatBox").innerHTML = "";

    loadChatHistory();           // ✅ Primero carga historial (si existe)
    startChat(userId, characterId); // ✅ Luego llama a la IA si no hay historial
}

// Función para filtrar un personaje de la sidebar
function filterCharacters() {
    const searchTerm = document.getElementById('characterSearch').value.toLowerCase();
    const characters = document.querySelectorAll('#characters-list .character');

    characters.forEach(character => {
        const characterName = character.getAttribute('data-name');
        if (characterName.includes(searchTerm)) {
            character.style.display = 'flex'; // O el valor de display que uses originalmente
        } else {
            character.style.display = 'none';
        }
    });
}

// Función para enviar un mensaje
function sendMessage() {
    const userId = parseInt(document.getElementById("userId").value, 10);
    const characterId = parseInt(document.getElementById("characterId").value, 10);
    const message = document.getElementById("messageInput").value.trim();

    if (isNaN(characterId)) {
        alert("Por favor, selecciona un personaje de la lista.");
        return;
    }
    if (!message) {
        alert("Por favor, ingresa un mensaje.");
        return;
    }

    connection.invoke("SendMessage", userId, characterId, message)
        .catch(err => console.error(err.toString()));

    document.getElementById("messageInput").value = "";
}

// Función para cargar el historial de mensajes
function loadChatHistory() {
    if (loading || !characterId) return;
    loading = true;

    console.log("Enviando parámetros:", { userId, characterId, page, pageSize: 10 });

    connection.invoke("LoadChatHistory", userId, characterId, page, 10)
        .then(() => {
            page++;
        })
        .catch(err => {
            console.error("Error al invocar LoadChatHistory:", err.toString());
        })
        .finally(() => {
            loading = false; // Asegura que se reestablezca el flag
        });
}


// Función para manejar la tecla Enter
function handleKeyPress(event) {
    if (event.key === "Enter") {
        sendMessage();
    }
}

// Función para agregar un mensaje al chat
function addMessageToChat(sender, message, mode = "append") {
    const chatBox = document.getElementById("chatBox");
    const msgContainer = document.createElement("div");
    msgContainer.classList.add("message");

    if (sender === "ai") { // Cambia "IA" por "ai" para coincidir con el valor de Role
        msgContainer.classList.add("received");
    } else {
        msgContainer.classList.add("sent");
    }

    msgContainer.innerHTML = `<span>${message}</span>`;

    if (mode === "prepend") {
        chatBox.prepend(msgContainer);
    } else {
        chatBox.appendChild(msgContainer);
        scrollToBottom();
    }
}

// Función para hacer scroll al final del chat
function scrollToBottom() {
    const chatBox = document.getElementById("chatBox");
    chatBox.scrollTop = chatBox.scrollHeight;
}
function startChat(userId, characterId) {
    fetch('/Chat/StartChatIfEmpty', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ userId: userId, characterId: characterId })
    })
        .then(response => response.json())
        .then(data => {
            console.log("Intro result:", data);
            // La IA responderá automáticamente vía SignalR (no necesitas hacer más aquí)
        });
}

// Evento para cargar más mensajes al hacer scroll hacia arriba
document.getElementById("chatBox").addEventListener("scroll", function () {
    if (this.scrollTop === 0) {
        loadChatHistory();
    }
});

// Evento para recibir mensajes nuevos
connection.on("ReceiveMessage", (sender, message) => {
    console.log(`Sender recibido: ${sender}`);

    const normalizedSender = sender.trim().toLowerCase();
    const role = normalizedSender === "ai" || normalizedSender === "ia" ? "ai" : "user";

    addMessageToChat(role, message);
});


// Evento para cargar el historial de mensajes
connection.on("LoadChatHistory", (jsonMessages) => {
    try {
        const messages = JSON.parse(jsonMessages);
        console.log("Mensajes recibidos:", messages);
        if (messages.length === 0) return;

        messages.forEach(msg => {
            // Usa msg.Role y msg.MessageText en lugar de msg.role y msg.messageText
            addMessageToChat(msg.Role === "user" ? "user" : "ai", msg.MessageText, "prepend");
        });

        if (page === 1) {
            scrollToBottom();
        }
    } catch (err) {
        console.error("Error al parsear JSON:", err);
    } finally {
        loading = false;
    }
});