using UnityEngine;
using UnityEngine.UI;
using TMPro;
// using System.IO; // WordDataManager内でPath.Combineを使うため、こちらでは直接は不要

public class DataLoadController : MonoBehaviour
{
    [Header("UI参照（インスペクターで設定）")]
    public TMP_InputField selectableWordsPathInput;
    public TMP_InputField masterQuizDataPathInput;
    public Button loadDataButton;
    public TextMeshProUGUI loadStatusText;
    public Button proceedToNextScreenButton;

    [Header("次に表示するCanvas（インスペクターで設定）")]
    public GameObject dataLoadingCanvasObject;
    public GameObject nextScreenCanvasObject; // PlayerSetupCanvasへの参照

    private bool selectableWordsFileSuccessfullyLoaded = false;
    private bool masterQuizDataFileSuccessfullyLoaded = false;

    // ★↓ここからドロップダウンの参照を追加↓★
public TMP_Dropdown genjiPlayerCountDropdown_DataLoad; // データ読み込み画面の源氏軍人数ドロップダウン
public TMP_Dropdown heishiPlayerCountDropdown_DataLoad; // データ読み込み画面の平氏軍人数ドロップダウン
                                                        // ★↑ここまで↑★

    void Start()
    {
        Debug.Log("DataLoadController: Start() が呼び出されました。");
        if (WordDataManager.Instance == null)
        {
            Debug.LogError("DataLoadController: WordDataManagerのインスタンスが見つかりません！");
            SetUIInteractable(false);
            if (loadStatusText != null) loadStatusText.text = "エラー: データ管理システム準備不足。";
            return;
        }
        ValidateInspectorReferences_DLC();
        if (loadDataButton != null) loadDataButton.onClick.AddListener(OnLoadDataButtonClicked);
        if (proceedToNextScreenButton != null)
        {
            proceedToNextScreenButton.onClick.AddListener(OnProceedToNextScreenButtonClicked);
            proceedToNextScreenButton.interactable = false;
        }
        if (loadStatusText != null) loadStatusText.text = "各CSVファイルのフルパスを入力し、「データ読み込み」を押してください。";
        
            if (genjiPlayerCountDropdown_DataLoad != null)
    {
        // genjiPlayerCountDropdown_DataLoad.onValueChanged.AddListener(delegate { OnPlayerCountDropdownChanged(); });
    }
    if (heishiPlayerCountDropdown_DataLoad != null)
    {
        // heishiPlayerCountDropdown_DataLoad.onValueChanged.AddListener(delegate { OnPlayerCountDropdownChanged(); });
    }
    // ...
    }

    void ValidateInspectorReferences_DLC() {
        if (selectableWordsPathInput == null) Debug.LogError("DataLoadController: selectableWordsPathInputが未設定！");
        if (masterQuizDataPathInput == null) Debug.LogError("DataLoadController: masterQuizDataPathInputが未設定！");
        if (loadDataButton == null) Debug.LogError("DataLoadController: loadDataButtonが未設定！");
        if (proceedToNextScreenButton == null) Debug.LogError("DataLoadController: proceedToNextScreenButtonが未設定！");
        if (dataLoadingCanvasObject == null) Debug.LogError("DataLoadController: dataLoadingCanvasObjectが未設定！");
        if (nextScreenCanvasObject == null) Debug.LogError("DataLoadController: nextScreenCanvasObjectが未設定！");
    }

    void SetUIInteractable(bool interactable) {
        if (selectableWordsPathInput != null) selectableWordsPathInput.interactable = interactable;
        if (masterQuizDataPathInput != null) masterQuizDataPathInput.interactable = interactable;
        if (loadDataButton != null) loadDataButton.interactable = interactable;
    }

    void OnLoadDataButtonClicked()
    {
        if (WordDataManager.Instance == null) { UpdateLoadStatus("エラー: データ管理システム準備不足。", false); return; }
        string selectablePath = (selectableWordsPathInput != null) ? selectableWordsPathInput.text.Trim() : "";
        string masterPath = (masterQuizDataPathInput != null) ? masterQuizDataPathInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(selectablePath) || string.IsNullOrWhiteSpace(masterPath)) { UpdateLoadStatus("エラー: 両方のCSVパスを指定してください。", false); return; }

        UpdateLoadStatus("データを読み込み中...", false);
        SetUIInteractable(false);

        WordDataManager.LoadResult resultSelectable = WordDataManager.Instance.LoadSelectableWordsFromCSV(selectablePath);
        selectableWordsFileSuccessfullyLoaded = (resultSelectable == WordDataManager.LoadResult.Success);
        string statusMsg = selectableWordsFileSuccessfullyLoaded ? $"ワードリスト成功 ({WordDataManager.Instance.SelectableWordsList.Count}件)" : $"ワードリスト失敗: {resultSelectable} ({System.IO.Path.GetFileName(selectablePath)})";

        if (selectableWordsFileSuccessfullyLoaded) {
            WordDataManager.LoadResult resultMaster = WordDataManager.Instance.LoadMasterQuizDataFromCSV(masterPath, true);
            masterQuizDataFileSuccessfullyLoaded = (resultMaster == WordDataManager.LoadResult.Success);
            statusMsg += masterQuizDataFileSuccessfullyLoaded ? $"\n問題データ成功 ({WordDataManager.Instance.MasterQuizDataList.Count}件)" : $"\n問題データ失敗: {resultMaster} ({System.IO.Path.GetFileName(masterPath)})";
        } else {
            masterQuizDataFileSuccessfullyLoaded = false;
            statusMsg += "\n問題データは読み込みませんでした。";
        }
        bool bothLoaded = selectableWordsFileSuccessfullyLoaded && masterQuizDataFileSuccessfullyLoaded;
        statusMsg += bothLoaded ? "\n\n読み込み完了！「次へ進む」を押してください。" : "\nファイルパスやCSV内容を確認し再試行してください。";
        UpdateLoadStatus(statusMsg, bothLoaded);
        SetUIInteractable(true);
    }
void OnProceedToNextScreenButtonClicked()
    {
        // 両方のCSVファイルがちゃんと読み込めていたら
        if (selectableWordsFileSuccessfullyLoaded && masterQuizDataFileSuccessfullyLoaded) 
        {
            Debug.Log("データ読み込み完了。次の画面（プレイヤー設定画面のはず）へ移行します。");

            // ★↓ドロップダウンから参加人数を取得し、WordDataManagerに保存する処理はここに必要です↓★
            //    (これは前回のコードで正しく入っていたはずなので、そのまま残してください)
            if (WordDataManager.Instance != null)
            {
                if (genjiPlayerCountDropdown_DataLoad != null && genjiPlayerCountDropdown_DataLoad.options.Count > 0)
                {
                    WordDataManager.Instance.GenjiPlayerCount = genjiPlayerCountDropdown_DataLoad.value + 1; 
                }
                // ... (平氏軍の人数も同様に保存) ...
                Debug.Log($"参加人数をWordDataManagerに保存: 源氏={WordDataManager.Instance.GenjiPlayerCount}人, 平氏={WordDataManager.Instance.HeishiPlayerCount}人");
            }
            else
            {
                Debug.LogError("DataLoadController: WordDataManagerのインスタンスが見つかりません。参加人数を保存できません。");
                // return; // エラーなら進まないようにするのも手
            }
            // ★↑ここまで↑★
           
            // このデータ読み込み画面のCanvasを隠す
            if (dataLoadingCanvasObject != null) 
            {
                dataLoadingCanvasObject.SetActive(false);
                Debug.Log($"DataLoadController: {dataLoadingCanvasObject.name} を非表示にしました。");
            }
            else 
            {
                Debug.LogWarning("DataLoadController: dataLoadingCanvasObject（この画面自身のCanvas）がInspectorで設定されていません。");
            }

            // ★↓次に表示する画面のCanvas（PlayerSetupCanvasのはず）を表示する【だけ】にする↓★
            if (nextScreenCanvasObject != null) 
            {
                nextScreenCanvasObject.SetActive(true); // 次の画面のCanvasを表示
                Debug.Log($"DataLoadController: 次の画面として {nextScreenCanvasObject.name} を表示しました。");
                
                // ★ PlayerSetupManager の UI 更新をここで明示的に呼ぶ ★
                //   (PlayerSetupManager の OnEnable() でも呼んでいますが、こちらの方が確実な場合もあります)
                PlayerSetupManager playerSetupMgr = nextScreenCanvasObject.GetComponent<PlayerSetupManager>(); // PlayerSetupCanvasにアタッチされているはず
                if (playerSetupMgr != null) {
                    playerSetupMgr.UpdatePlayerUIBasedOnDropdown(); // ★正しい関数名 UpdatePlayerUIBasedOnDropdown() に修正★
                    Debug.Log("DataLoadController: PlayerSetupManagerのUI更新 (UpdatePlayerUIBasedOnDropdown) を呼び出しました。");
                } else {
                    Debug.LogError($"DataLoadController: 表示した次の画面「{nextScreenCanvasObject.name}」にPlayerSetupManagerが見つかりません。");
                }
            }
            else 
            { 
                Debug.LogError("DataLoadController: nextScreenCanvasObject（次に表示する画面のCanvas）がInspectorで設定されていません！"); 
            }
            // ★↑ここまで↑★
        } 
        else // データがちゃんと読み込めていなかったら
        { 
            UpdateLoadStatus("エラー: データ未読み込み。「データ読み込み」を再試行してください。", false); 
            Debug.LogError("DataLoadController: データ未読み込みのため画面遷移できません。");
        }
    }
    void UpdateLoadStatus(string message, bool allowProceed) {
        if (loadStatusText != null) loadStatusText.text = message;
        if (proceedToNextScreenButton != null) proceedToNextScreenButton.interactable = allowProceed;
    }
}
