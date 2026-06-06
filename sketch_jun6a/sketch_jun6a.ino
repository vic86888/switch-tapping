// 定義腳位
const int joyXPin = A0;
const int joyYPin = A1;
const int btnRedPin = 2;
const int btnBluePin = 3;

void setup() {
  // 啟動序列埠通訊，設定 Baud Rate 為 115200 (為了未來傳給 Unity 能有更低的延遲)
  Serial.begin(115200);

  // 設定按鈕腳位為「輸入 + 內部上拉電阻」
  // 這樣接線只需一端接腳位、一端接地，不需外加電阻。
  // 注意：使用 INPUT_PULLUP 時，沒按鈕時讀取為 HIGH (1)，按下時為 LOW (0)
  pinMode(btnRedPin, INPUT_PULLUP);
  pinMode(btnBluePin, INPUT_PULLUP);
}

void loop() {
  // 1. 讀取搖桿類比數值 (範圍 0 ~ 1023，置中大約是 512)
  int joyX = analogRead(joyXPin);
  int joyY = analogRead(joyYPin);

  // 2. 判斷搖桿方向 (設定閥值 300 與 700 避免搖桿些微晃動造成的誤判)
  String dirText = "Center";
  int dirCode = -1; // -1代表置中，未來給 Unity 用的代碼

  // 注意：X和Y的上下左右對應，會因為你手拿搖桿的方向而有差異，如果測試時發現相反，交換程式碼裡的 X/Y 或大於/小於即可
  if (joyY < 300) {
    dirText = "UP";
    dirCode = 0;
  } else if (joyY > 700) {
    dirText = "DOWN";
    dirCode = 1;
  } else if (joyX < 300) {
    dirText = "LEFT";
    dirCode = 2;
  } else if (joyX > 700) {
    dirText = "RIGHT";
    dirCode = 3;
  }

  // 3. 讀取按鈕狀態
  // 因為使用 INPUT_PULLUP，我們加上 ! (反相) 讓數值變直覺：1代表按下，0代表放開
  int redPressed = !digitalRead(btnRedPin);
  int bluePressed = !digitalRead(btnBluePin);

  // 4. 將結果印出到 Serial Monitor (序列埠監控視窗)
  // 這裡印出人類易讀的格式，確認硬體沒問題後，之後我們再改成 Unity 專用的簡短格式
  Serial.print("搖桿方向: ");
  Serial.print(dirText);
  Serial.print("\t(X:");
  Serial.print(joyX);
  Serial.print(" Y:");
  Serial.print(joyY);
  Serial.print(")\t紅按鈕: ");
  Serial.print(redPressed ? "按下!" : "放開 ");
  Serial.print("\t藍按鈕: ");
  Serial.println(bluePressed ? "按下!" : "放開 ");

  // 稍微延遲避免洗頻太快
  delay(50); 
}