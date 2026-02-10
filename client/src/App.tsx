import {useEffect, useState} from "react";



export default function App() {

    const [messages, setMessages] = useState<any[]>([]);
    const [inputText, setInputText] = useState("");

    useEffect(() => {

        const es = new EventSource("http://localhost:5159/Chat/Connect");

        es.onmessage = (event) => {
            setMessages((prev) => [...prev, event.data])

        };
        return () => es.close();

    }, [])

    const handleSend = async () => {
        if (!inputText) return;

        await fetch("http://localhost:5159/Chat/SendMessage", {
            method: "POST",
            body: JSON.stringify({content: inputText}),
            headers: {
                "Content-Type": "application/json"
            }
        })
    }

    return (
        <div>
            <>
                {JSON.stringify(messages)}
                {messages.map((msg, index) => (
                    <div key={index}>{msg}</div>
                ))}
            </>

            <input
                type="text"
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSend()}
                placeholder="Enter Message..."
                style={{margin: "10px", padding: "5px"}}
            />
        </div>
    )

}

