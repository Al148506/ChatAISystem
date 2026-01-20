// ==============================
// 1. ESTADO GLOBAL
// ==============================
const state = {
    userId: parseInt(document.getElementById("userId").value),
    characterId: null,
    page: 1,
    loading: false
};

// ==============================
// 2. SIGNALR CONNECTION
// ==============================
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        accessTokenFactory: async () => {
            const response = await fetch('/api/negotiate');
            const data = await response.json();
            return data.accessToken || '';
        }
    })
    .withAutomaticReconnect()
    .build();

connection.start()
    .then(() => console.log("✅ Conectado a SignalR"))
    .catch(err => console.error("❌ Error SignalR:", err.toString()));


// ==============================
// 3. UI HELPERS
// ==============================
function disableInput() {
    document.getElementById("messageInput").disabled = true;
    document.getElementById("sendButton").disabled = true;
}

function enableInput() {
    document.getElementById("messageInput").disabled = false;
    document.getElementById("sendButton").disabled = false;
    document.getElementById("messageInput").focus();
}

function scrollToBottom() {
    const chatBox = document.getElementById("chatBox");
    chatBox.scrollTop = chatBox.scrollHeight;
}

function addMessageToChat(sender, message, mode = "append") {
    const chatBox = document.getElementById("chatBox");
    const msgContainer = document.createElement("div");

    msgContainer.classList.add("message", sender === "ai" ? "received" : "sent");
    msgContainer.innerHTML = `<span>${message}</span>`;

    if (mode === "prepend") {
        chatBox.prepend(msgContainer);
    } else {
        chatBox.appendChild(msgContainer);
        scrollToBottom();
    }
}

// ==============================
// 4. CHAT ACTIONS
// ==============================
function selectCharacter(id, name) {
    state.characterId = parseInt(id, 10);
    state.page = 1;

    document.getElementById("characterId").value = id;
    document.getElementById("chatTitle").innerText = `Chat with ${name}`;
    document.getElementById("chatBox").innerHTML = "";

    const input = document.getElementById("messageInput");
    input.disabled = false;
    input.placeholder = `Write a message for ${name}...`;

    document.getElementById("sendButton").disabled = false;

    loadChatHistory();
    startChatIfEmpty();
}

function sendMessage() {
    const message = document.getElementById("messageInput").value.trim();

    if (!state.characterId || !message) return;

    connection.invoke("SendMessage", state.userId, state.characterId, message)
        .catch(err => console.error("SendMessage error:", err));

    document.getElementById("messageInput").value = "";
}

// ==============================
// 5. API / SERVER CALLS
// ==============================
function loadChatHistory() {
    if (state.loading || !state.characterId) return;
    state.loading = true;

    connection.invoke(
        "LoadChatHistory",
        state.userId,
        state.characterId,
        state.page,
        10
    )
        .then(() => state.page++)
        .catch(err => console.error("LoadChatHistory error:", err))
        .finally(() => state.loading = false);
}

function startChatIfEmpty() {
    fetch('/Chat/StartChatIfEmpty', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            userId: state.userId,
            characterId: state.characterId
        })
    });
}

// ==============================
// 6. SIGNALR EVENTS
// ==============================
connection.on("ReceiveMessage", (sender, message) => {
    const role = sender.trim().toLowerCase() === "ai" ? "ai" : "user";
    addMessageToChat(role, message);
});

connection.on("AIWritingStarted", (characterName) => {
    const indicator = document.getElementById("typingIndicator");
    indicator.style.display = "block";
    indicator.innerHTML = `<em>${characterName} is writing, please wait...</em>`;
    disableInput();
});

connection.on("AIWritingFinished", () => {
    document.getElementById("typingIndicator").style.display = "none";
    enableInput();
});

connection.on("LoadChatHistory", (messages) => {
    if (!messages || messages.length === 0) return;

    messages.forEach(msg => {
        addMessageToChat(
            msg.role === "user" ? "user" : "ai",
            msg.messageText,
            "prepend"
        );
    });

    if (state.page === 1) scrollToBottom();
});

// ==============================
// 7. DOM EVENTS
// ==============================
document.getElementById("chatBox").addEventListener("scroll", function () {
    if (this.scrollTop === 0) loadChatHistory();
});

function handleKeyPress(event) {
    if (event.key === "Enter") sendMessage();
}

function filterCharacters() {
    const searchTerm = document.getElementById('characterSearch').value.toLowerCase();
    document.querySelectorAll('#characters-list .character').forEach(character => {
        const name = character.getAttribute('data-name');
        character.style.display = name.includes(searchTerm) ? 'flex' : 'none';
    });
}
