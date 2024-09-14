#include <WiFi.h>
#include <HTTPClient.h>
#include <Wire.h>
#include <Adafruit_PN532.h>

// PARAMETERS:

const char* ssid = "";
const char* password = "";
const char* serverUrl = "";
// Dvorana ID
#define DvoranaID 3

// Pins for the PN532
#define SDA_PIN 21
#define SCL_PIN 22

// Create an instance of the PN532
Adafruit_PN532 nfc(SDA_PIN, SCL_PIN);

void setup() {
  Serial.begin(115200);
  WiFi.mode(WIFI_STA);  // Set WiFi to station mode (client mode)
  WiFi.begin(ssid, password);

  unsigned long startTime = millis();
  unsigned long timeout = 30000; // 30 seconds timeout

  // Wait for the connection to be established or timeout
  while (WiFi.status() != WL_CONNECTED && millis() - startTime < timeout) {
    delay(1000);
    Serial.print("Connecting to WiFi: ");
    Serial.println(ssid);  // Print SSID name
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("Connected to WiFi");
  } else {
    Serial.println("Failed to connect to WiFi");
    // Handle connection failure (e.g., restart the Arduino, attempt reconnection, etc.)
  }

  nfc.begin();
  uint32_t versiondata = nfc.getFirmwareVersion();
  if (!versiondata) {
    Serial.print("Didn't find PN53x board");
    while (1);
  }

  nfc.SAMConfig();
  Serial.println("Waiting for an NFC card...");
}

void loop() {
  uint8_t success;
  uint8_t uid[] = { 0, 0, 0, 0, 0, 0, 0 };
  uint8_t uidLength;

  // Wait for an RFID card
  success = nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, uid, &uidLength);

  if (success) {
    Serial.println("RFID card detected!");

    // Convert UID to a string
    String uidStr = "";
    for (uint8_t i = 0; i < uidLength; i++) {
      uidStr += String(uid[i], HEX);
    }

    // Send HTTP POST request to server with card UID
    if (WiFi.status() == WL_CONNECTED) {
      HTTPClient http;

      http.begin(serverUrl);
      http.addHeader("Content-Type", "application/json");

      // Create JSON payload
      String jsonPayload = "{\"uid\": \"" + uidStr + "\", \"dvoranaId\": " + String(DvoranaID) + "}";

      int httpResponseCode = http.POST(jsonPayload);

      if (httpResponseCode > 0) {
        String response = http.getString();
        Serial.println("Response: " + response);
      } else {
        Serial.println("Error on sending POST: " + String(httpResponseCode));

        // Error handling: Restart WiFi and retry
        if (httpResponseCode == -11) {
          Serial.println("Resetting WiFi and HTTP client after error...");
          WiFi.disconnect();
          delay(2000);
          WiFi.begin(ssid, password);
          while (WiFi.status() != WL_CONNECTED) {
            delay(1000);
            Serial.print(".");
          }
          Serial.println("Reconnected to WiFi");
        }
      }

      http.end(); // Ensure this is always called
    } else {
      Serial.println("WiFi not connected. Unable to send data.");
    }
    delay(1000);  // Delay to prevent multiple scans from being sent rapidly
  }
}
