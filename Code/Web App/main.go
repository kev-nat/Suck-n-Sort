package main

import (
	"context"
	"encoding/base64"
	"html/template"
	"log"
	"net/http"

	"github.com/gorilla/websocket"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
}

var tmpl = template.Must(template.ParseFiles("index.html"))

func wsEndpoint(w http.ResponseWriter, r *http.Request) {
	upgrader.CheckOrigin = func(r *http.Request) bool { return true }

	ws, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println(err)
		return
	}
	defer ws.Close()

	// You might want to handle WebSocket connections here, e.g., register clients
	// For simplicity, I've omitted the client registration logic
}

func setupSockets() {
	http.HandleFunc("/ws", wsEndpoint)
}

type TrashType struct {
	Name   string
	Amount int
	Img    []byte
}

// Define a global map to store WebSocket connections
var clients = make(map[*websocket.Conn]bool)
var broadcast = make(chan []byte)

// Function to broadcast messages to all connected clients
func handleMessages() {
	for {
		// Get the next message from the broadcast channel
		msg := <-broadcast
		// Send the message to all connected clients
		for client := range clients {
			err := client.WriteMessage(websocket.TextMessage, msg)
			if err != nil {
				log.Println("Error broadcasting message:", err)
				return
			}
		}
	}
}

func main() {

	fs := http.FileServer(http.Dir("static/"))
	http.Handle("/static/", http.StripPrefix("/static/", fs))

	// MongoDB connection setup
	uri := "mongodb+srv://kev:GeRNWT62OhjvQGzg@sucknsort.1lsxt0v.mongodb.net/?retryWrites=true&w=majority"
	client, err := mongo.Connect(context.TODO(), options.Client().ApplyURI(uri))
	if err != nil {
		log.Fatal(err)
	}
	defer func() {
		if err := client.Disconnect(context.TODO()); err != nil {
			log.Fatal(err)
		}
	}()

	// Database and collection setup
	db := client.Database("WasteSorting")
	coll := db.Collection("TypesofTrash")

	// HTTP handler setup
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {

		// Filter for organic amount value
		filterOrg := bson.D{{Key: "Name", Value: "Organic"}}
		optsOrg := options.FindOne().SetProjection(bson.D{{Key: "Amount", Value: 1}, {Key: "Img", Value: 1}})
		var resultOrg TrashType
		err = coll.FindOne(context.TODO(), filterOrg, optsOrg).Decode(&resultOrg)
		if err != nil {
			if err == mongo.ErrNoDocuments {
				log.Println("No documents found")
			} else {
				log.Fatal(err)
			}
		}
		orgImg := base64.StdEncoding.EncodeToString(resultOrg.Img)

		// Filter for inorganic amount value
		filterInorg := bson.D{{Key: "Name", Value: "Inorganic"}}
		optsInorg := options.FindOne().SetProjection(bson.D{{Key: "Amount", Value: 1}, {Key: "Img", Value: 1}})
		var resultInorg TrashType
		err = coll.FindOne(context.TODO(), filterInorg, optsInorg).Decode(&resultInorg)
		if err != nil {
			if err == mongo.ErrNoDocuments {
				log.Println("No documents found")
			} else {
				log.Fatal(err)
			}
		}
		inorgImg := base64.StdEncoding.EncodeToString(resultInorg.Img)

		// Execute the template and pass data to it
		err = tmpl.ExecuteTemplate(w, "index.html", map[string]interface{}{
			"Organic":      resultOrg.Amount,
			"Inorganic":    resultInorg.Amount,
			"OrganicImg":   orgImg,
			"InorganicImg": inorgImg,
		})

		// Error Handling
		if err != nil {
			log.Fatal(err)
		}

	})

	setupSockets()

	// Start the WebSocket message handler
	go handleMessages()

	// Start the web server
	http.ListenAndServe(":3001", nil)
}
