// wwwroot/cryptoHelper.js

async function generateKey() {
    const key = await window.crypto.subtle.generateKey(
        {
            name: "AES-GCM",
            length: 256
        },
        true,
        ["encrypt", "decrypt"]
    );

    const exportedKey = await window.crypto.subtle.exportKey("raw", key);
    return btoa(String.fromCharCode(...new Uint8Array(exportedKey))); // Encode to base64
}

async function encryptData(data, base64Key) {
    const key = await importKey(base64Key);
    const iv = window.crypto.getRandomValues(new Uint8Array(12));
    const encodedData = new TextEncoder().encode(data);

    const encryptedData = await window.crypto.subtle.encrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        key,
        encodedData
    );

    return {
        iv: btoa(String.fromCharCode(...iv)), // Encode IV to base64
        encryptedData: btoa(String.fromCharCode(...new Uint8Array(encryptedData))) // Encode encrypted data to base64
    };
}

async function decryptData(encryptedData, base64Key, base64Iv) {
    const key = await importKey(base64Key);
    const iv = Uint8Array.from(atob(base64Iv), c => c.charCodeAt(0));
    const encodedData = Uint8Array.from(atob(encryptedData), c => c.charCodeAt(0));

    const decryptedData = await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        key,
        encodedData
    );

    return new TextDecoder().decode(decryptedData);
}

async function importKey(base64Key) {
    const rawKey = Uint8Array.from(atob(base64Key), c => c.charCodeAt(0));
    return await window.crypto.subtle.importKey(
        "raw",
        rawKey,
        "AES-GCM",
        true,
        ["encrypt", "decrypt"]
    );
}
