using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class UIController : MonoBehaviour
{
    [Header("Top Bar")]
    public TMP_Text levelText;
    public TMP_Text movesText;
    public Button pauseButton;

    [Header("Bottom Bar")]
    public Button[] powerUpSlots; // 4 slot

    [Header("Overlay")]
    public GameObject levelCompletePanel;
    public Button nextButton;
    public Button restartButton;

    [Header("Fail Overlay")]
    public GameObject failPanel;
    public Button failRestartButton;


    void Awake()
    {
        // Otomatik referans bulma (Inspector boþ kalsa bile çalýþsýn)
        if (levelText == null)
            levelText = GameObject.Find("LevelText")?.GetComponent<TMPro.TMP_Text>();

        if (movesText == null)
            movesText = GameObject.Find("MovesText")?.GetComponent<TMPro.TMP_Text>();

        if (pauseButton == null)
            pauseButton = GameObject.Find("PauseButton")?.GetComponent<UnityEngine.UI.Button>();

        if (levelCompletePanel == null)
            levelCompletePanel = GameObject.Find("LevelCompletePanel");

        if (nextButton == null)
            nextButton = GameObject.Find("NextButton")?.GetComponent<UnityEngine.UI.Button>();

        if (restartButton == null)
            restartButton = GameObject.Find("RestartButton")?.GetComponent<UnityEngine.UI.Button>();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (failRestartButton != null)
            failRestartButton.onClick.AddListener(RestartLevel);

        if (failPanel != null)
            failPanel.SetActive(false);

        if (failRestartButton != null)
            failRestartButton.onClick.AddListener(() => Debug.Log("Fail Restart clicked"));


        Debug.Log($"UIController Awake | levelCompletePanel={(levelCompletePanel != null ? "OK" : "NULL")}");

        // Power-up slotlarý þimdilik kapalý
        if (powerUpSlots != null)
        {
            foreach (var b in powerUpSlots)
                if (b != null) b.interactable = false;
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        // Butonlar (þimdilik sadece log)
        if (pauseButton != null) pauseButton.onClick.AddListener(() => Debug.Log("Pause clicked"));
        if (restartButton != null) restartButton.onClick.AddListener(() => Debug.Log("Restart clicked"));

        if (nextButton != null)
            nextButton.onClick.AddListener(() =>
            {
                if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
                else Debug.LogError("LevelManager.Instance null!");
            });

        if (restartButton != null)
            restartButton.onClick.AddListener(() =>
            {
                if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
                else Debug.LogError("LevelManager.Instance null!");
            });

        if (failRestartButton != null)
            failRestartButton.onClick.AddListener(() =>
            {
                if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
                else Debug.LogError("LevelManager.Instance null!");
            });

    }


    public void SetLevel(int levelIndex)
    {
        if (levelText != null)
            levelText.text = $"Level {levelIndex}";
    }

    public void SetMoves(int moves)
    {
        if (movesText != null)
            movesText.text = $"Moves: {moves}";
    }

    public void ShowLevelComplete()
    {
        Debug.Log("UIController.ShowLevelComplete called");

        if (levelCompletePanel == null)
        {
            Debug.LogError("LevelCompletePanel reference NULL (UIController).");
            return;
        }

        levelCompletePanel.SetActive(true);
    }

    public void HideLevelComplete()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    public void ShowFail()
    {
        Debug.Log("UIController.ShowFail called");
        if (failPanel != null) failPanel.SetActive(true);
    }

    public void HideFail()
    {
        if (failPanel != null) failPanel.SetActive(false);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


}
