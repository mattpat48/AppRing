#include <WiFi.h>
#include <HTTPClient.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <esp_wifi.h>
#include <arduino.h>

#include <JsonQueue.h>
#include <CryptographyTools.h>

const char* DEVICE_ID = "id1";

JsonQueue* allQueue = new JsonQueue();
JsonQueue* myQueue = new JsonQueue();

//const char* WIFI_SSID = "mexageWiFi2G";
//const char* WIFI_PASSWORD = "r&t&dim&x@g&";
//const char* WIFI_SSID = "TIM-66833537";
//const char* WIFI_PASSWORD = "PPRP9QPTk3EhtZA2AHTTfhRF";
const char* WIFI_SSID = "w48";
const char* WIFI_PASSWORD = "mattpatt";
NetworkServer server(80);

//const char* MQTT_BROKER_ADDRESS = "10.20.100.50";
const char* MQTT_BROKER_ADDRESS = "vainnhomeserver.ddns.net";
const int  MQTT_PORT = 1883;

const char* MQTT_USERNAME = "user";
const char* MQTT_PASSWORD = "ringuser";

// The MQTT topics that ESP32 should publish/subscribe
const char* PUBLISH_TOPIC = "ringRequest/gatePublish";
const char* SUBSCRIBE_TOPIC = "ringRequest/request";

const int PUBLISH_INTERVAL = 5000;  // 5 seconds

String API_ADDRESS = "https://192.168.111.150:7046";
String publicServerKey;

WiFiClient network;
HTTPClient http;
PubSubClient mqttClient(network);

CryptographyTools* keysManager = new CryptographyTools();

unsigned long lastPublishTime = 0;

int pin = 2;
int delayPoint = 1000;

bool connectToWiFi() {

  Serial.println("--- ESP32 - Connecting to WiFi ---");

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.print("Connected to ");
  Serial.println(WIFI_SSID);
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  server.begin();

  return true;
}

void callback(char *topic, byte *payload, unsigned int length) {
  String jsonString = "";
  Serial.print("Message arrived in topic: ");
  Serial.println(topic);
  //Serial.print("Message:");
  for (int i = 0; i < length; i++) {
      //Serial.print((char)payload[i]);
      jsonString += (char)payload[i];
  }

  JsonDocument doc;
  DeserializationError error = deserializeJson(doc, jsonString);

  if (error) {
    Serial.print(F("deserializeJson() failed: "));
    Serial.println(error.f_str());
    return;
  }
  else {
    JsonDocument* toPush = &doc;
    allQueue->push(*toPush);
    JsonDocument arrived = allQueue->first();
    const char* arrivedId = arrived["gate"];
    Serial.print("Gate id: ");
    Serial.println(arrivedId);
    Serial.print("allQueue size: ");
    Serial.println(allQueue->size());

    if (arrivedId == DEVICE_ID) {
      myQueue->push(*toPush);
      Serial.println("Log registered");
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

void getServerKey() {

  String keyPath = API_ADDRESS + "/api/v1/keys/getpublic";
  http.begin(keyPath.c_str());
  int httpResponseCode = http.GET();

  if (httpResponseCode>0) {
    Serial.print("HTTP Response code: ");
    Serial.println(httpResponseCode);
    String payload = http.getString();
    publicServerKey = payload;
    //Serial.println(payload);
  }
  else {
    Serial.print("Error code: ");
    Serial.println(httpResponseCode);
  }

  http.end();
}

void publishMessage(String message) {
  if(mqttClient.publish(PUBLISH_TOPIC, String(message).c_str())){
    digitalWrite(pin, HIGH);
    Serial.println("Message sent");
  }
}

void setup() {

  pinMode(pin, OUTPUT);

  Serial.begin(9600);
  Serial.println();
  Serial.println("-----------------------");

  mqttClient.setServer(MQTT_BROKER_ADDRESS, MQTT_PORT);
  mqttClient.setCallback(callback);
  
  connect();
  getServerKey();

}

void loop() {

  if (!mqttClient.connected()) {
    connectToMQTT();
  }
  mqttClient.loop();

  
  digitalWrite(pin, LOW);
  
  if (millis() - lastPublishTime > PUBLISH_INTERVAL) {
    getServerKey();
    //publishMessage("test message");
    lastPublishTime = millis();
  }
  
}