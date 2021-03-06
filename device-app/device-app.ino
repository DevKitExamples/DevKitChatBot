#include "mbed.h"
#include "AZ3166WiFi.h"
#include "http_client.h"
#include "AudioClassV2.h"
#include "RingBuffer.h"
#include "SystemTickCounter.h"
#include "WebSocketClient.h"

#define HEARTBEAT_INTERVAL 60000
#define RING_BUFFER_SIZE 32000
#define PLAY_DELAY_RATE 0.1

static bool hasWifi;
static bool isWsConnected;
static int lastButtonAState;
static int buttonAState;
static int lastButtonBState;
static int buttonBState;
static volatile int status;
static uint64_t hb_interval_ms;

static AudioClass &Audio = AudioClass::getInstance();
static WebSocketClient *wsClient;

RingBuffer ringBuffer(RING_BUFFER_SIZE);
char readBuffer[AUDIO_CHUNK_SIZE];
char websocketBuffer[4096];

static char emptyAudio[AUDIO_CHUNK_SIZE];
bool startPlay = false;

// If you have multiple devices(clients) using the same WebSocket server,
// Please ensure the nickName for each client is unique
static char *nickName = "devkit-test";
static char *webAppUrl = "ws://[web-app-name].azurewebsites.net";

void initWiFi()
{
    Screen.print("IoT DevKit\r\n \r\nConnecting...\r\n");

    if (WiFi.begin() == WL_CONNECTED)
    {
        IPAddress ip = WiFi.localIP();
        Screen.print(1, ip.get_address());
        hasWifi = true;
        Screen.print(2, "Running... \r\n");
    }
    else
    {
        Screen.print(1, "No Wi-Fi\r\n ");
    }
}

bool connectWebSocket()
{
    Screen.clean();
    Screen.print(0, "Connect to WS...");

    char *webSocketServerUrl = getWebSocketUrl();
    if (wsClient == NULL)
    {
        wsClient = new WebSocketClient(webSocketServerUrl);
    }

    isWsConnected = wsClient->connect();
    if (isWsConnected)
    {
        Serial.println("connect WS successfully.");
        Screen.print(1, "connect WS successfully.");

        // Trigger heart beat immediately
        hb_interval_ms = -(HEARTBEAT_INTERVAL);
        sendHeartbeat();
        return true;
    }
    else
    {
        Screen.print(1, "Connect WS fail");
        Screen.print(2, "Press A to retry");
        Serial.println("Connect WS fail");

        return false;
    }
}

void sendHeartbeat()
{
    if ((int)(SystemTickCounterRead() - hb_interval_ms) < HEARTBEAT_INTERVAL)
    {
        return;
    }

    // Send haertbeart message
    Serial.println(">>Heartbeat<<");
    int ret = wsClient->send("heartbeat", 9, WS_Message_Text);
    if (ret < 0)
    {
        // Heartbeat failure, disconnet from WS
        enterIdleState();
    }

    // Reset heartbeat
    hb_interval_ms = SystemTickCounterRead();
}

void record()
{
    ringBuffer.clear();
    Audio.format(8000, 16);
    Audio.startRecord(recordCallback);
}

void play()
{
    Serial.println("start playing");
    enterPlayingState();

    Audio.format(8000, 16);
    Audio.startPlay(playCallback);
    startPlay = true;
}

void stop()
{
    Audio.stop();
    startPlay = false;
}

void playCallback(void)
{
    if (ringBuffer.use() < AUDIO_CHUNK_SIZE)
    {
        Audio.writeToPlayBuffer(emptyAudio, AUDIO_CHUNK_SIZE);
        return;
    }
    ringBuffer.get((uint8_t *)readBuffer, AUDIO_CHUNK_SIZE);
    Audio.writeToPlayBuffer(readBuffer, AUDIO_CHUNK_SIZE);
}

void recordCallback(void)
{
    Audio.readFromRecordBuffer(readBuffer, AUDIO_CHUNK_SIZE);
    ringBuffer.put((uint8_t *)readBuffer, AUDIO_CHUNK_SIZE);
}

void setResponseBodyCallback(const char *data, size_t dataSize)
{
    if (status == 3)
    {
        enterReceivingState();
    }

    while (ringBuffer.available() < dataSize)
    {
        delay(10);
    }

    ringBuffer.put((uint8_t *)data, dataSize);
    if (ringBuffer.use() > RING_BUFFER_SIZE * PLAY_DELAY_RATE && startPlay == false)
    {
        play();
    }
}

char *getWebSocketUrl()
{
    char *url;
    url = (char *)malloc(300);

    if (url == NULL)
    {
        return NULL;
    }
    HTTPClient guidRequest = HTTPClient(HTTP_GET, "http://www.fileformat.info/tool/guid.htm?count=1&format=text&hyphen=true");
    const Http_Response *_response = guidRequest.send();
    if (_response == NULL)
    {
        Serial.println("Guid generator HTTP request failed.");
        return NULL;
    }

    snprintf(url, 300, "%s/chat?nickName=%s", webAppUrl, _response->body);
    Serial.print("WebSocket server url: ");
    Serial.println(url);
    return url;
}

void enterIdleState()
{
    status = 0;
    Screen.clean();
    Screen.print(0, "DevKit-luis.ai");
    Screen.print(1, "Press A to start\r\nconversation");
}

void enterActiveState()
{
    status = 1;
    Screen.print(0, "Active");
    Screen.print(1, "> Hold B to talk");
    Screen.print(2, "> Press A to end  conversation", true);
    Serial.println("Hold B to talk or press A to end conversation");
}

void enterRecordingState()
{
    status = 2;
    Screen.clean();
    Screen.print(0, "Recording...");
    Screen.print(1, "Release B to send    ");
    Serial.println("Release B to send    ");
}

void enterServerProcessingState()
{
    status = 3;
    Screen.clean();
    Screen.print(0, "Processing...");
    Screen.print(1, "Thinking...", true);
}

void enterReceivingState()
{
    status = 4;
    Screen.clean();
    Screen.print(0, "Processing...");
    Screen.print(1, "Receiving...");
}

void enterPlayingState()
{
    status = 5;
    Screen.print(0, "Processing...");
    Screen.print(1, "Playing...");
}

void setup()
{
    Screen.init();
    Screen.print(0, "IoT DevKit");
    Screen.print(2, "Initializing...");

    Screen.print(3, " > Serial");
    Serial.begin(115200);

    // Initialize the WiFi module
    Screen.print(3, " > WiFi");
    hasWifi = false;
    initWiFi();
    if (!hasWifi)
    {
        return;
    }

    pinMode(USER_BUTTON_A, INPUT);
    lastButtonAState = digitalRead(USER_BUTTON_A);
    pinMode(USER_BUTTON_B, INPUT);
    lastButtonBState = digitalRead(USER_BUTTON_B);

    memset(emptyAudio, 0x0, AUDIO_CHUNK_SIZE);
    enterIdleState();
}

void loop()
{
    if (hasWifi)
    {
        doWork();
    }
}

void doWork()
{
    switch (status)
    {
    // Idle
    case 0:
        buttonAState = digitalRead(USER_BUTTON_A);
        if (buttonAState == LOW)
        {
            if (connectWebSocket())
            {
                enterActiveState();
            }
        }
        break;

    // Active state, ready for conversation
    case 1:
        buttonBState = digitalRead(USER_BUTTON_B);
        if (buttonBState == LOW)
        {
            record();
            enterRecordingState();
        }

        buttonAState = digitalRead(USER_BUTTON_A);
        if (buttonAState == LOW)
        {
            if (wsClient->close())
            {
                Serial.println("WebSocket close succeeded.");
                Screen.print("End conversation");
                delay(200);
                isWsConnected = false;
                enterIdleState();
            }
            else
            {
                Serial.println("WebSocket close failed.");
            }
        }

        sendHeartbeat();
        break;

    // Recording state
    case 2:
        while (digitalRead(USER_BUTTON_B) == LOW || ringBuffer.use() > 0)
        {
            if (digitalRead(USER_BUTTON_B) == HIGH)
            {
                stop();
            }

            int sz = ringBuffer.get((uint8_t *)websocketBuffer, AUDIO_CHUNK_SIZE);
            if (sz > 0)
            {
                wsClient->send(websocketBuffer, sz, WS_Message_Binary, false);
            }
        }

        if (Audio.getAudioState() == AUDIO_STATE_RECORDING)
        {
            stop();
        }

        // Mark binary message end
        wsClient->send("0x00", 4, WS_Message_Binary, true);
        Serial.println("Your voice message is sent.");
        enterServerProcessingState();
        break;

    // Receiving and playing
    case 3:
        bool isEndOfMessage = false;
        WebSocketReceiveResult *recvResult = NULL;
        int len = 0;

        while (!isEndOfMessage)
        {
            recvResult = wsClient->receive(websocketBuffer, sizeof(websocketBuffer));
            if (recvResult != NULL && recvResult->length > 0)
            {
                len = recvResult->length;
                isEndOfMessage = recvResult->isEndOfMessage;
                setResponseBodyCallback(websocketBuffer, len);
            }
        };

        if (startPlay == false)
        {
            play();
        }

        while (ringBuffer.use() >= AUDIO_CHUNK_SIZE)
        {
            delay(100);
        }
        stop();
        enterActiveState();
    }
}
