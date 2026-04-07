using Leadr;
using Leadr.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gameplay")]
    public float initialGameSpeed = 5f;
    public float gameSpeedIncrease = 0.1f;
    public float gameSpeed { get; private set; }
    public bool isGameStarted;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TMP_InputField inputPlayerNameText;
    [SerializeField] private Transform submitNamePanel;
    [SerializeField] private Transform leaderboardPanel;
    [SerializeField] private Transform leaderBoardBtn;
    [SerializeField] private Button retryButton;

    

    [Header("LEADR")]
    [SerializeField] private LeadrSettings settings;
    public const string BOARD_ID = "brd_956bad96-8a3c-4517-baa0-3c223a6f6e82";
    public const string BEST_SCORE_BOARD_ID = "brd_a44f93db-9d23-4308-a301-0e4166da2ddb";

    private LeadrClient leadrClient;

    private Player player;
    private Spawner spawner;

    private float score;
    private int playTime;
    private bool isSubmitting;

    private const string PLAYER_NAME_KEY = "PLAYER_NAME";

    public float Score => score;

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;

        // LEADR SDK initialization
        leadrClient = new LeadrClient();
        leadrClient.Initialize(settings);

      


    }

    private void OnEnable()
    {
       
    }

    private void OnDisable()
    {
       
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

       
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            ShowNameInput();
            return;
        }

        playerNameText.text = PlayerPrefs.GetString(PLAYER_NAME_KEY);

        player = FindAnyObjectByType<Player>();
        spawner = FindAnyObjectByType<Spawner>();

        spawner.StartSpawn();
        NewGame();
    }

    public void ToggleLeaderboard()
    {
        leaderboardPanel.gameObject.SetActive(!leaderboardPanel.gameObject.activeSelf);
    }

    public void CloseLeaderBoard()
    {
        leaderboardPanel.gameObject.SetActive(false);
    }

    public async Task GetLeadrBoardAsync()
    {
        // LEADR SDK: Fetch board by slug
        var result = await leadrClient.GetBoardAsync("highscore");

        if (!result.IsSuccess)
            Debug.LogError(result.Error.Message);
    }

    private async Task SubmitScoreAsync()
    {
        if (isSubmitting)
            return;

        isSubmitting = true;

        try
        {
            string playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");

            long runScore = Mathf.FloorToInt(score);
            long savedHighScore = Mathf.RoundToInt(PlayerPrefs.GetFloat("hiscore", 0));

            var metadata = BuildMetadata();
            string displayValue = scoreText?.text;

            // LEADR SDK: Submit score to board
            var result = await leadrClient.SubmitScoreAsync(
                BOARD_ID,
                runScore,
                playerName,
                displayValue,
                metadata
            );

            if (!result.IsSuccess)
                Debug.LogError(result.Error.Message);

            if (runScore > savedHighScore)
            {
                Debug.Log("New high score! Submitting to best scores board.");
                await SubmitBestScoreAsync(
                    runScore,
                    playerName,
                    displayValue,
                    metadata
                );
            }

        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task SubmitBestScoreAsync(
    long score,
    string playerName,
    string displayValue,
    Dictionary<string, object> metadata)
    {
        Debug.Log("Submitting best score to best scores board.");
        var result = await leadrClient.SubmitScoreAsync(
            BEST_SCORE_BOARD_ID,
            score,
            playerName,
            displayValue,
            metadata
        );

        if (!result.IsSuccess)
            Debug.LogError(result.Error.Message);
    }

    private Dictionary<string, object> BuildMetadata()
    {
        return new Dictionary<string, object>
        {
            { "level_reached", Mathf.FloorToInt(gameSpeed) },
            { "play_time_seconds", playTime },
            { "difficulty", Mathf.FloorToInt(gameSpeed / initialGameSpeed) },
            { "client_version", Application.version },
            { "platform", Application.platform.ToString() }
        };
    }

    private void ShowNameInput()
    {
        submitNamePanel.gameObject.SetActive(true);
    }

    public void SubmitName()
    {
        submitNamePanel.gameObject.SetActive(false);

        PlayerPrefs.SetString(PLAYER_NAME_KEY, inputPlayerNameText.text);
        PlayerPrefs.Save();

        playerNameText.text = inputPlayerNameText.text;

        player = FindAnyObjectByType<Player>();
        spawner = FindAnyObjectByType<Spawner>();

        spawner.StartSpawn();
        NewGame();
    }

    public void NewGame()
    {
        isGameStarted = true;

        foreach (var obstacle in FindObjectsOfType<Obstacle>())
            Destroy(obstacle.gameObject);
        leaderBoardBtn.gameObject.SetActive(false);

        score = 0f;
        playTime = 0;
        gameSpeed = initialGameSpeed;

        player.gameObject.SetActive(true);
        spawner.gameObject.SetActive(true);

        gameOverText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        UpdateHighScore();
    }

    public void GameOver()
    {
        _ = SubmitScoreAsync();

        isGameStarted = false;
        gameSpeed = 0f;

        player.gameObject.SetActive(false);
        spawner.gameObject.SetActive(false);

        gameOverText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
        leaderBoardBtn.gameObject.SetActive(true);

        UpdateHighScore();
    }

    private void Update()
    {
        if (!isGameStarted)
            return;

        playTime++;
        gameSpeed += gameSpeedIncrease * Time.deltaTime;
        score += gameSpeed * Time.deltaTime;

        scoreText.text = Mathf.FloorToInt(score).ToString("D5");
    }

    private void UpdateHighScore()
    {
        float highScore = PlayerPrefs.GetFloat("hiscore", 0f);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("hiscore", highScore);
        }

        hiscoreText.text = Mathf.FloorToInt(highScore).ToString("D5");
    }
    private bool IsNewHighScore(long score)
    {
        long savedHighScore = Mathf.RoundToInt(
            PlayerPrefs.GetFloat("hiscore", 0)
        );

        return score > savedHighScore;
    }
}
