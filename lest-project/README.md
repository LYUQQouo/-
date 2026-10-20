# lest-project — BurnDevice Control（ClientApp 骨架）

對應本機路徑建議：

`C:\Users\e6170\OneDrive\桌面\嘉明利\WNC\lest-project`

比照 BurnDevice Control 截圖的三種模式，並加上 **MES 以瀏覽資料夾為主、檔名／副檔名不限**（資料由客戶提供）。

## 功能對照

| 區塊 | 說明 |
|------|------|
| DLL 目錄 | 預設 `C:\ACROVIEW\MultiAprog\`，套用後載入同目錄 `ProgCtrl.dll` |
| MES 資料夾 | `FolderBrowserDialog`；列出資料夾內所有檔案（`*.*`）；雙擊可填入欄位 |
| 1 FullProgrammingFlow | CreateProject → StartProject → Report；可選 `.tsk` → `LoadTaskDataToAutomatic` |
| 2 LoadAndProgram | LoadProject → StartProject；可選 `.tsk` |
| 3 ChecksumOnlyFlow | CreateProject → GetChecksum（可用 StartServiceSimple） |

瀏覽檔案預設 **所有檔案 (*.*)**，另提供 `.apr/.apj`、`.tsk`、韌體副檔名作偏好，不強制。

## 預建執行檔（下載即可試）

倉庫內已附 **framework-dependent** 執行檔（需本機安裝 [.NET 8 Desktop Runtime x86](https://dotnet.microsoft.com/download/dotnet/8.0)）：

```text
lest-project\dist\win-x86\BurnDeviceControl.exe
```

完整路徑（放到本機後）：

```text
C:\Users\e6170\OneDrive\桌面\嘉明利\WNC\lest-project\dist\win-x86\BurnDeviceControl.exe
```

同目錄請保留：`BurnDeviceControl.dll`、`BurnDeviceControl.deps.json`、`BurnDeviceControl.runtimeconfig.json`（四個檔放一起）。

雙擊 `BurnDeviceControl.exe` → 應出現 **BurnDevice Control** 視窗（三個流程區塊 + MES 資料夾）。  
真機燒錄還需 `C:\ACROVIEW\MultiAprog\ProgCtrl.dll`；沒有 DLL 時可先確認 UI 有開起來。

> 雲端無法寫入你的 OneDrive；請從 GitHub PR / 分支下載 `lest-project/dist/win-x86/`。

## 在 Windows 上從原始碼建置

需 [.NET 8 SDK](https://dotnet.microsoft.com/download) 與昂科 MultiAprog（含 `ProgCtrl.dll`）。

```bat
cd /d "C:\Users\e6170\OneDrive\桌面\嘉明利\WNC\lest-project"
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

或用 Visual Studio 2022 開啟 `lest-project.csproj`（平台建議 **x86**，與多數 ProgCtrl 一致）。

### Multi 側前置

1. MultiAprog → Settings → MES → 勾選 MESEnable / Standard MES  
2. 連自動機時勾選 AutomaticEnable  
3. 自動燒錄時 **IPS 需開啟**；`.tsk` 由 IPS 製作，本程式只傳路徑  

## 架構備註

- `.apr`／工程 → **Multi**（經 ProgCtrl）  
- `.tsk` → **IPS** 執行；本程式經 ProgCtrl `LoadTaskDataToAutomatic` 傳路徑（StartProject 成功後）  
- MES 真實 API 未接：目前以「客戶提供的資料夾」代替派工檔來源  

詳見專案文件：`docs/acroview-mes-architecture.md`（Agent Store）。
