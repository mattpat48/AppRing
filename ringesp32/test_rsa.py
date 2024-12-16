import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)

from Crypto.PublicKey import RSA
from Crypto.Cipher import PKCS1_v1_5, AES
from Crypto.Signature import pkcs1_15
from Crypto.Hash import SHA256
from Crypto.Util.Padding import pad
from Crypto.Random import get_random_bytes
from datetime import datetime
import paho.mqtt.client as mqtt
import matplotlib.pyplot  as plt
import numpy as np
import time
import json
import base64
import requests

deviceId = "testDeviceId"
phoneNumber = "testPhoneNumber"
lat = "0,0"
language = "en"
gate = "id1"

size = 100
publish_timestamps = np.zeros(size)
receive_timestamps = np.zeros(size)
counter = 0
withrsa = True

server_address = "192.168.1.47:7046"

def generate_rsa_keypair():
    key = RSA.generate(1024)
    private_key = key.export_key()
    public_key = key.publickey().export_key()
    #print("generate_rsa_keypair OK")
    return private_key, public_key

private_key, public_key = generate_rsa_keypair()

def sign_json(data):
    serialized_data = json.dumps(data).encode('utf-8')
    h = SHA256.new(serialized_data)
    hash_hex = h.hexdigest()
    toimport = private_key.decode('utf-8')
    key = RSA.import_key(toimport)
    signature = pkcs1_15.new(key).sign(h)
    return signature

def encrypt_with_aes(data, key, iv):
    cipher = AES.new(key, AES.MODE_CBC, iv)
    padded_data = pad(data, AES.block_size)
    encrypted_data = cipher.encrypt(padded_data)
    #print("encrypt_with_aes OK")
    return encrypted_data

def encrypt_with_rsa(public_key, data):
    key = RSA.import_key(public_key)
    cipher = PKCS1_v1_5.new(key)
    encrypted_data = cipher.encrypt(data)
    #print("encrypt_with_rsa OK")
    return encrypted_data



def get_gate_key():
    url = "https://" + server_address + "/api/v1/gate/getgatekeynoauth"
    payload = "id1"
    headers = {
        "Content-Type": "application/json",
    }

    response = requests.post(url, json=payload, headers=headers, verify=False)

    if response.status_code == 200:
        #print("get_gate_key OK")
        return response.text
    else:
        print(f"get_gate_key response: {response.status_code}")
        print("Details:", response.text)
        return ""
    
external_public_key = get_gate_key()
if (external_public_key == ""):
    print("Error: null external key")

def get_server_key():
    url = "https://" + server_address + "/api/v1/keys/getpublic"

    response = requests.get(url, verify=False)

    if response.status_code == 200:
        #print("get_server_key OK")
        return response.text
    else:
        print(f"get_server_key response: {response.status_code}")
        print("Details:", response.text)
        return ""
    


def post_public_key(public_key):
    url = "https://" + server_address + "/api/v1/auth/postuserkey"
    payload = {
        "PhoneNumber": phoneNumber,
        "Id": deviceId,
        "PKey": public_key.decode('utf-8')
    }
    string_to_send = json.dumps(payload)

    headers = {
        "Content-Type": "application/json",
    }
    response = requests.post(url, json=string_to_send, headers=headers, verify=False)

    if response.status_code == 200:
        #print("post_public_key OK")
        return response.status_code
    else:
        print(f"post_public_key response: {response.status_code}")
        print("Details:", response.text)
        return response.status_code



def total_encrypt(toSend, external_public_key):

    signature = sign_json(toSend)
    payload = {
        "Data": toSend,
        "Signature": base64.b64encode(signature).decode('utf-8')
    }
    completed_payload = {
        "Number": phoneNumber,
        "Id": deviceId,
        "Payload": payload
    }
    
    serialized_payload = json.dumps(completed_payload).encode('utf-8')
    aes_key = get_random_bytes(16)
    aes_iv = get_random_bytes(16)

    encrypted_data = encrypt_with_aes(serialized_payload, aes_key, aes_iv)
    encrypted_key = encrypt_with_rsa(external_public_key, aes_key)
    encrypted_iv = encrypt_with_rsa(external_public_key, aes_iv)

    final_payload = {
        "EncryptedData": base64.b64encode(encrypted_data).decode('utf-8'),
        "EncryptedKey": base64.b64encode(encrypted_key).decode('utf-8'),
        "EncryptedIV": base64.b64encode(encrypted_iv).decode('utf-8')
    }

    print("total_enrypt OK")
    return json.dumps(final_payload)


def on_connect(client, userdata, flags, rc):
    print(f"Connected to mqtt broker {rc}")
    client.subscribe("ringRequest/acks")

def on_message(client, userdata, msg):
    timestamp = int(time.time() * 1000)
    global size
    global counter
    if (counter < size):
        text = json.loads(msg.payload.decode())
        if (text['status'] == "closing"):
            print(f"Closing ack received, counter: {counter}")
            receive_timestamps[counter] = timestamp
            print("timestamp: ", receive_timestamps[counter])
            counter = counter + 1
            print("-------------\n")
            if (counter < size):
                publish_message()
        if (text['status'] == "opening"):
            print("Opening ack received")

def on_publish(client, userdata, mid):
    print(f"Message sent (MID: {mid}, Counter: {counter})")

def publish_message():
    global withrsa
    global counter
    global size
    data_to_send = {
        "id": deviceId,
        "phoneNumber": phoneNumber,
        "datetime": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "lat": lat,
        "language": language,
        "gate": gate
    }
    timestamp = int(time.time() * 1000)
    if (counter < size):
        publish_timestamps[counter] = timestamp
        print("timestamp: ", publish_timestamps[counter])
    if (withrsa):
        message = total_encrypt(data_to_send, external_public_key)
        print("ENCRYPTED")
    else:
        message = json.dumps(data_to_send)
        print("NOT ENCRYPTED")
    result = client.publish("ringRequest/request", message)


client_id = "+testNumber_testId"
client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION1, client_id)
client.on_connect = on_connect
client.on_message = on_message
client.on_publish = on_publish

def analyze_latency(latencies):
    mean_latency = np.mean(latencies)
    std_latency = np.std(latencies)
    min_latency = np.min(latencies)
    max_latency = np.max(latencies)
    min_index = np.argmin(latencies)
    max_index = np.argmax(latencies)
    return mean_latency, std_latency, min_latency, max_latency, min_index, max_index

def main():
    global size
    global counter
    global withrsa
    global publish_timestamps
    global receive_timestamps

    server_public_key = get_server_key()
    if (server_public_key == ""):
        print("Error: null server key")
        return

    response = post_public_key(public_key)
    if (response != 200):
        print("Error: post_public_key failed")
        return

    username = "user"
    password = "ringuser"
    client.username_pw_set(username, password)

    broker = "vainnhomeserver.ddns.net"
    port = 1883
    client.connect(broker, port, 60)

    """
    client.loop_start()
    publish_message()

    try:
        while (counter < size):
            pass
    except KeyboardInterrupt:
        print("Stop listening...")
    finally:
        client.loop_stop()
        client.disconnect()

    rsa_latencies = np.array(receive_timestamps - publish_timestamps)[:size]
    """

    counter = 0
    publish_timestamps = np.zeros(size)
    receive_timestamps = np.zeros(size)
    withrsa = False

    client.connect(broker, port, 60)
    client.loop_start()
    publish_message()

    try:
        while (counter < size):
            pass
    except KeyboardInterrupt:
        print("Stop listening...")
    finally:
        client.loop_stop()
        client.disconnect()

    norsa_latencies = np.zeros(size)
    for i in np.arange(0, size):
        if (receive_timestamps[i] - publish_timestamps[i] >= 0):
            norsa_latencies[i] = receive_timestamps[i] - publish_timestamps[i]
        else:
            norsa_latencies[i] = publish_timestamps[i] - receive_timestamps[i]

    #mean_rsa, std_rsa, min_rsa, max_rsa, min_index_rsa, max_index_rsa = analyze_latency(rsa_latencies)
    mean_norsa, std_norsa, min_norsa, max_norsa, min_index_norsa, max_index_norsa = analyze_latency(norsa_latencies)
    msg_id = np.arange(size)

    """
    plt.figure(figsize=(10, 6))
    plt.plot(rsa_latencies, label="con RSA", color='blue')
    plt.fill_between(
        msg_id,
        mean_rsa - std_rsa,
        mean_rsa + std_rsa,
        color='green',
        alpha=0.2,
        label="±1σ"
    )
    # Linea tratteggiata per la media
    plt.axhline(mean_rsa, color='purple', linestyle='--', label=f"Media ({mean_rsa:.2f} ms)")
    plt.xlabel("Messaggio")
    plt.ylabel("Tempo di latenza (ms)")
    plt.title("Latenza con RSA")
    plt.scatter([min_index_rsa], [min_rsa], color='orange', label="MIN con RSA", zorder=5)
    plt.scatter([max_index_rsa], [max_rsa], color='red', label="MAX con RSA", zorder=5)
    plt.text(min_index_rsa, min_rsa, f"{min_rsa:.2f} ms", color='green', fontsize=10, ha='center')
    plt.text(max_index_rsa, max_rsa, f"{max_rsa:.2f} ms", color='red', fontsize=10, ha='center')
    plt.legend()
    plt.savefig("msg_latenxa_rsa.png", format="png", dpi=300, bbox_inches="tight")
    plt.close()
    """

    plt.figure(figsize=(10, 6))
    plt.plot(norsa_latencies, label="senza RSA", color='purple')
    plt.fill_between(
        msg_id,
        mean_norsa - std_norsa,
        mean_norsa + std_norsa,
        color='green',
        alpha=0.2,
        label="±1σ"
    )
    # Linea tratteggiata per la media
    plt.axhline(mean_norsa, color='blue', linestyle='--', label=f"Media ({mean_norsa:.2f} ms)")
    plt.xlabel("Messaggio")
    plt.ylabel("Tempo di latenza (ms)")
    plt.title("Latenza senza RSA")
    plt.scatter([min_index_norsa], [min_norsa], color='orange', label="MIN senza RSA", zorder=5)
    plt.scatter([max_index_norsa], [max_norsa], color='red', label="MAX senza RSA", zorder=5)
    plt.text(min_index_norsa, min_norsa, f"{min_norsa:.2f} ms", color='green', fontsize=10, ha='center')
    plt.text(max_index_norsa, max_norsa, f"{max_norsa:.2f} ms", color='red', fontsize=10, ha='center')
    plt.legend()
    plt.savefig("msg_latenxa_norsa.png", format="png", dpi=300, bbox_inches="tight")
    plt.close()

    """
    plt.figure(figsize=(10, 6))
    plt.hist(rsa_latencies, bins=15, alpha=0.5, label="con RSA", color='blue', edgecolor='black')
    plt.legend()
    plt.savefig("frequenza_rsa.png", format="png", dpi=300, bbox_inches="tight")
    plt.close()
    """

    plt.figure(figsize=(10, 6))
    plt.hist(norsa_latencies, bins=15, alpha=0.5, label="senza RSA", color='purple', edgecolor='black')
    plt.legend()
    plt.savefig("frequenza_norsa.png", format="png", dpi=300, bbox_inches="tight")
    plt.close()

    print("End of main")

if __name__ == "__main__":
    main()
