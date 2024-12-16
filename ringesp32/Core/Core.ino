#pragma once

#include <Arduino.h>

#include <WiFi.h>
#include <PubSubClient.h>
#include <HTTPClient.h>
#include <Base64.h>

#include "JsonQueue.h"
#include "Crypto.h"

const char* DEVICE_ID = "id1";

JsonQueue* allQueue = new JsonQueue();
JsonQueue* myQueue = new JsonQueue();

Crypto crypto;

//const char* WIFI_SSID = "mexageWiFi2G";
//const char* WIFI_PASSWORD = "r&t&dim&x@g&";
const char* WIFI_SSID = "TIM-66833537";
const char* WIFI_PASSWORD = "PPRP9QPTk3EhtZA2AHTTfhRF";
//const char* WIFI_SSID = "w48";
//const char* WIFI_PASSWORD = "mattpatt";
NetworkServer server(80);

//const char* MQTT_BROKER_ADDRESS = "10.20.100.50";
const char* MQTT_BROKER_ADDRESS = "vainnhomeserver.ddns.net";
const int  MQTT_PORT = 1883;

const char* MQTT_USERNAME = "user";
const char* MQTT_PASSWORD = "ringuser";

const char* PUBLISH_TOPIC = "ringRequest/acks";
const char* SUBSCRIBE_TOPIC = "ringRequest/request";

const int OPEN_INTERVAL = 500;
const int CLOSE_INTERVAL = 500;

String API_ADDRESS = "https://192.168.1.47:7046";
String publicServerKey;

WiFiClient network;
HTTPClient http;
PubSubClient mqttClient(network);

unsigned long lastOpenTime = 0;
unsigned long lastCloseTime = 0;

int pin = 2;
int delayPoint = 1000;

bool isOpening = false;
bool isClosing = false;

String openingNumber;

bool connectToWiFi() {

  Serial.println("--- ESP32 - Connecting to WiFi ---");

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println();

  Serial.print("Connected to ");
  Serial.println(WIFI_SSID);
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
  Serial.println("-----------------------");


  server.begin();

  return true;
}



void sendAck(String phoneNumber, String status) {
  JsonDocument toSend;
  toSend["phoneNumber"] = phoneNumber;
  toSend["gateId"] = (String)DEVICE_ID;
  toSend["status"] = status;
  String message;
  serializeJson(toSend, message);
  publishMessage(message);
}




void open(JsonDocument toPush) {

  if (isOpening) {
    Serial.println("Gate is already opening.");
    return;
  }

  isClosing = false;
  isOpening = true;
  lastOpenTime = millis();
  Serial.println("Gate is opening...");
  digitalWrite(pin, HIGH);
  openingNumber = (String)toPush["phoneNumber"];

  myQueue->push(toPush);
  Serial.print("myQueue size: ");
  Serial.println(myQueue->size());
  Serial.println("Log registered");

}




String adjustPackage(String &jsonString) {

  String toReturn = "";

  for (int i = 0; i < jsonString.length(); i++) {
    char c = jsonString[i];
    if (c != '\\') {
      toReturn += c;
    }
  }

  if (toReturn[0] != '{')
    toReturn = toReturn.substring(1, toReturn.length()-1);
  if (toReturn[toReturn.length()-1] != '}')
    toReturn = toReturn.substring(0, toReturn.length()-2);
  
  return toReturn;
}




void callback(char *topic, byte *payload, unsigned int length) {
  if (isOpening) {
    Serial.println("Gate is already opening!");
    return;
  }

  Serial.print("Message arrived in topic: ");
  Serial.println(topic);
  String jsonString = "";
  
  //Serial.println("Message:");
  for (int i = 0; i < length; i++) {
    //Serial.print((char)payload[i]);
    jsonString += (char)payload[i];
  }
  //Serial.println();

  JsonDocument doc;
  String adjustedJsonString = adjustPackage(jsonString);
  //Serial.println("Adjusted Message");
  //Serial.println(adjustedJsonString);
  DeserializationError error = deserializeJson(doc, adjustedJsonString);

  if (error) {
    Serial.print(F("core deserializeJson() 1 failed: "));
    Serial.println(error.f_str());
    return;
  }
  else {
    JsonDocument decrypted;
    if (!(doc.containsKey("phoneNumber"))) {
      String encryptedData = doc["EncryptedData"];
      String encryptedKey = doc["EncryptedKey"];
      String encryptedIv = doc["EncryptedIV"];

      String decryptedPlainText = crypto.totalDecrypt(encryptedData, encryptedKey, encryptedIv, API_ADDRESS);
      Serial.println(decryptedPlainText);
      if (decryptedPlainText == "") {
        return;
      }

      error = deserializeJson(decrypted, decryptedPlainText);
      if (error) {
        Serial.print(F("core deserializeJson() 2 failed: "));
        Serial.println(error.f_str());
        return;
      }
    } else {
      error = deserializeJson(decrypted, adjustedJsonString);
      if (error) {
        Serial.print(F("core deserializeJson() 2 failed: "));
        Serial.println(error.f_str());
        return;
      }
    }

    const char* arrivedId = decrypted["gate"];
    String phoneNumber = decrypted["phoneNumber"];
    Serial.print("arrivedId: ");
    Serial.println(arrivedId);

    allQueue->push(decrypted);
    JsonDocument arrived = allQueue->first();
    Serial.print("allQueue size: ");
    Serial.println(allQueue->size());

    if (strcmp(arrivedId, DEVICE_ID) == 0 && !isOpening) {
      open(decrypted);
    }
  }
  Serial.println("-----------------------");
}




void connectToMQTT() {

  Serial.println("--- ESP32 - Connecting to MQTT broker ---");
  while (!mqttClient.connected()) {
    mqttClient.disconnect();
    mqttClient.unsubscribe(SUBSCRIBE_TOPIC);
    if (mqttClient.connect(DEVICE_ID, MQTT_USERNAME, MQTT_PASSWORD)) {
        Serial.println("Connected to MQTT broker successfully");
        Serial.println("-----------------------");
    }
    else {
        Serial.print("failed with state ");
        Serial.println(mqttClient.state());
        delay(2000);
    }
  }
  mqttClient.subscribe(SUBSCRIBE_TOPIC);
}




void connect() {
  bool wifiResult;
  wifiResult = connectToWiFi();
  if (wifiResult)
    connectToMQTT();
  else
    Serial.println("Error while connecting to WiFi.");
}




int postServerKey(String keyToSend) {

  String keyPath = API_ADDRESS + "/api/v1/gate/addgatekey";
  http.begin(keyPath.c_str());
  http.addHeader("Content-Type", "application/json");

  String payload = "{\"id\":\"" + (String)DEVICE_ID + "\",""\"key\":\"" + keyToSend + "\"}";
  int httpResponseCode = http.POST(payload);

  if (httpResponseCode == 200) {
    Serial.print("POST KEY Response code: ");
    Serial.println(httpResponseCode);
    String payload = http.getString();
    Serial.println(payload);
  }
  else {
    Serial.print("POST KEY Error code: ");
    Serial.println(httpResponseCode);
  }

  http.end();
  return httpResponseCode;
}




int getServerKey() {

  String keyPath = API_ADDRESS + "/api/v1/keys/getpublic";
  http.begin(keyPath.c_str());
  int httpResponseCode = http.GET();

  if (httpResponseCode == 200) {
    Serial.print("GET KEY Response code: ");
    Serial.println(httpResponseCode);
    String payload = http.getString();
    publicServerKey = payload;
    //Serial.println(payload);
  }
  else {
    Serial.print("GET KEY Error code: ");
    Serial.println(httpResponseCode);
  }

  http.end();
  return httpResponseCode;
}




void publishMessage(String message) {
  if(mqttClient.publish(PUBLISH_TOPIC, String(message).c_str())){
    Serial.println("Message sent");
  }
}




void keysGetAndPost() {
  int responseCode;

  responseCode = getServerKey();
  while (responseCode != 200) { responseCode = getServerKey(); }
  responseCode = postServerKey(crypto.publicKeyPem);
  while (responseCode != 200) { responseCode = postServerKey(crypto.publicKeyPem); }
}





void setup() {

  pinMode(pin, OUTPUT);

  Serial.begin(9600);
  Serial.println();
  Serial.println("-----------------------");

  mqttClient.setServer(MQTT_BROKER_ADDRESS, MQTT_PORT);
  mqttClient.setCallback(callback);
  mqttClient.setBufferSize(4096);

  connect();

  int ret = crypto.generateRSAKeys();
  if (ret != 0) {
    Serial.print("Error code: ");
    Serial.println(ret);
  }

  keysGetAndPost();

}




void loop() {

  if (WiFi.status() != WL_CONNECTED || !mqttClient.connected()) {
    Serial.println("Something went wrong! Reconnecting...");
    connect();
    keysGetAndPost();
  }

  mqttClient.loop();
  
  if (isOpening && (millis() - lastOpenTime >= OPEN_INTERVAL)) {
    isOpening = false;
    isClosing = true;
    Serial.println("Gate has finished opening.");
    sendAck(openingNumber, "opening");
    Serial.println("Gate is closing...");
    lastCloseTime = millis();
  }

  if (isClosing && (millis() - lastCloseTime >= CLOSE_INTERVAL)) {
    isClosing = false;
    Serial.println("Gate has finished closing.");
    sendAck(openingNumber, "closing");
    openingNumber = "";
    digitalWrite(pin, LOW);
  } 
}