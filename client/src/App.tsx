import {useEffect, useRef, useState} from "react";



export default function App() {

    //username state
    const [username, setUsername] = useState("");
    const [isUsernmeSet, setIsUsernameSet] = useState(false);

    const [messages, setMessages] = useState<any[]>([]);
    const [inputText, setInputText] = useState("");

    const [typingMessage, setTypingMessage] = useState("");
    const lastTypingTime = useRef(0);

    useEffect(() => {

        if (!isUsernmeSet) return;

        const es = new EventSource("http://localhost:5159/Chat/Connect");

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

    }, [isUsernmeSet])

    const handleTyping = () => {
        const now = Date.now();

        //send only if its more than 2 sec
        if (now - lastTypingTime.current > 2000) {
            lastTypingTime.current = now;

            //Fire and forget
            fetch("http://localhost:5159/Chat/UserTyping", {
                method: "POST",
                body: JSON.stringify({username: username }),
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
                username: username}),
            headers: {
                "Content-Type": "application/json"
            }
        })
    }

    //submit username
    const handleUsernameSubmit = () => {
        if (username.trim()){
            setIsUsernameSet(true);
        }
    }

    if(!isUsernmeSet) {
        return (
            <div style={{padding: "20px"}}>
                <h2>Enter your name</h2>
                <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Your name..."
                style={{margin: "10px", padding: "5px"}}
                />
                <button onClick={handleUsernameSubmit}>Join Chat</button>
                </div>
        );
    }




    return (
        <div>
            <>
                {messages.map((message, index) => (
                    <div key={index}>{message}</div>
                    ))}
            </>

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
                style={{margin: "10px", padding: "5px"}}
            />
        </div>
    )

}

