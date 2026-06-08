// === 定義腳位 ===
const int btnRedPin = 2;   // 紅色按鈕 (對應 J 鍵)
const int btnBluePin = 3;  // 藍色按鈕 (對應 K 鍵)
const int joySwPin = A2;   // 🌟 搖桿按壓開關 (SW) 改接 A2，線路更整齊
const int pinX = A0;       // 搖桿 X 軸
const int pinY = A1;       // 搖桿 Y 軸

// === 紀錄狀態的變數 ===
int lastRedState = HIGH;
int lastBlueState = HIGH;
int lastJoySwState = HIGH; // 🌟 新增：紀錄搖桿按鍵狀態
String lastDirection = "";

void setup() {
  pinMode(btnRedPin, INPUT_PULLUP);
  pinMode(btnBluePin, INPUT_PULLUP);
  pinMode(joySwPin, INPUT_PULLUP); // 設定搖桿按鈕為上拉電阻

  Serial.begin(115200); 
}

void loop() {
  // ==========================================
  // 1. 處理紅圈按鈕 (J)
  // ==========================================
  int redState = digitalRead(btnRedPin);
  if (redState != lastRedState) {
    lastRedState = redState;
    if (redState == LOW) {
      Serial.println("J_DOWN");
    } else {
      Serial.println("J_UP");
    }
  }

  // ==========================================
  // 2. 處理藍圈按鈕 (K)
  // ==========================================
  int blueState = digitalRead(btnBluePin);
  if (blueState != lastBlueState) {
    lastBlueState = blueState;
    if (blueState == LOW) {
      Serial.println("K_DOWN");
    } else {
      Serial.println("K_UP");
    }
  }

  // ==========================================
  // 🌟 3. 處理搖桿按壓 (當作獨立的第 3 顆按鈕)
  // ==========================================
  int joySwState = digitalRead(joySwPin);
  if (joySwState != lastJoySwState) {
    lastJoySwState = joySwState;
    if (joySwState == LOW) {
      Serial.println("JOY_BTN_DOWN");
    } else {
      Serial.println("JOY_BTN_UP");
    }
  }

  // ==========================================
  // 4. 處理搖桿方向 (純粹讀取 X Y 軸)
  // ==========================================
  static unsigned long lastJoyTime = 0;
  if (millis() - lastJoyTime > 50) {
    lastJoyTime = millis();

    int xVal = analogRead(pinX);
    int yVal = analogRead(pinY);

    String currentDir = "CENTER";

    // 正常判斷方向
    if (yVal < 300) {
      currentDir = "UP";
    } else if (yVal > 700) {
      currentDir = "DOWN";
    } else if (xVal < 300) {
      currentDir = "LEFT";
    } else if (xVal > 700) {
      currentDir = "RIGHT";
    }

    // 防洗頻機制：方向改變時才輸出
    if (currentDir != lastDirection) {
      Serial.print("DIR_");
      Serial.println(currentDir);
      lastDirection = currentDir;
    }
  }

  delay(10);
}