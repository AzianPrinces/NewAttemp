import {useEffect, useRef, useState} from "react";



export default function App() {

    //username state and room
    const [username, setUsername] = useState("");
    const [isJoined, setIsJoined] = useState(false);
    const [room, setRoom] = useState("");


    const [messages, setMessages] = useState<any[]>([]);
    const [inputText, setInputText] = useState("");

    const [typingMessage, setTypingMessage] = useState("");
    const lastTypingTime = useRef(0);

    useEffect(() => {

        if (!isJoined) return;

        const es = new EventSource(`http://localhost:5159/Chat/Connect?room=${encodeURIComponent(room)}`);

        es.onmessage = (event) => {
            setMessages((prev) => [...prev, event.data])

        };

        es.addEventListener("typing", (event: MessageEvent)=> {
            const data = JSON.parse(event.data);

            setTypingMessage(`${data.username} is typing...`);

            setTimeout(() => {
                setTypingMessage("");
            }, 3000);
        })

        return () => es.close();

    }, [isJoined, room])

    const handleTyping = () => {
        const now = Date.now();

        //send only if its more than 2 sec
        if (now - lastTypingTime.current > 2000) {
            lastTypingTime.current = now;

            fetch("http://localhost:5159/Chat/UserTyping", {
                method: "POST",
                body: JSON.stringify({

                    username: username,
                    room: room
                }),
                headers: { "Content-Type": "application/json" }
            });
        }
    }

    const handleSend = async () => {
        if (!inputText) return;

        const textToSend = inputText;
        setInputText("")

        await fetch("http://localhost:5159/Chat/SendMessage", {
            method: "POST",
            body: JSON.stringify({
                content: textToSend,
                username: username,
                room: room
            }),
            headers: {
                "Content-Type": "application/json"
            }
        })
    }

    //submit username
    const handleJoinChat = () => {
        if (username.trim() && room.trim()){
            setIsJoined(true);
        }
    }

    if(!isJoined) {
        return (
            <div style={{padding: "20px"}}>
                <h2>Join chat</h2>
                <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Your name..."
                style={{margin: "10px", padding: "5px"}}
                />

                <input
                type="text"
                value={room}
                onChange={(e) => setRoom(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleJoinChat()}
                placeholder="Room name..."
                style={{margin: "10px", padding: "5px", display: "block"}}
                />

                <button onClick={handleJoinChat}>Join Chat</button>
                </div>
        );
    }




    return (
        <div style={{padding: "20px"}}>

            <h3>Room: {room} | User: {username}</h3>

            <div style={{ border: "1px solid #ccc", padding: "10px", minHeight: "300px", marginBottom: "10px"}}>

                {messages.map((message, index) => (
                    <div key={index}>{message}</div>
                    ))}
            </div>

            {typingMessage && (<div style={{ color: "gray", fontStyle: "italic", margin: "10px"}}>
                {typingMessage}
            </div>)}

            <input
                type="text"
                value={inputText}
                onChange={(e) => {
                    setInputText(e.target.value);
                    handleTyping();
                }}
                onKeyDown={(e) => e.key === 'Enter' && handleSend()}
                placeholder="Enter Message..."
                style={{margin: "10px", padding: "5px", width: "80%"}}
            />
        </div>
    )

}

