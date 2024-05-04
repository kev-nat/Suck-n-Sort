// Establish WebSocket connection
const socket = new WebSocket("ws://localhost:8000/ws"); 

// Handle incoming messages from the WebSocket server
socket.onmessage = function(event) {
    const data = JSON.parse(event.data);
    // Update the UI with the new data
    updateTrashList(data);
};

// Update the trash list in the UI
function updateTrashList(trashTypes) {
    const trashList = document.getElementById("trashList");
    // Clear existing list items
    trashList.innerHTML = "";
    // Add new list items based on the updated data
    trashTypes.forEach(trashType => {
        const listItem = document.createElement("li");
        listItem.textContent = `Trash Type: ${trashType.Name}, Amount: ${trashType.Amount}`;
        trashList.appendChild(listItem);
    });
}