using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Button retryButton;

    [Header("LEADR")]
    [SerializeField] private LeadrIntegration leadr;

    private Player player;
    private Spawner spawner;

    private float score;
    private int playTime;

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
        leadr.Initialize();
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

        score = 0f;
        playTime = 0;
        gameSpeed = initialGameSpeed;

        player.gameObject.SetActive(true);
        spawner.gameObject.SetActive(true);

        gameOverText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        leadr.HideLeaderboard();

        UpdateHighScore();
    }

    public void GameOver()
    {
        isGameStarted = false;
        gameSpeed = 0f;

        player.gameObject.SetActive(false);
        spawner.gameObject.SetActive(false);

        gameOverText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        UpdateHighScore();

        // Submit score to LEADR and show the leaderboard
        _ = leadr.SubmitScoreAndShowLeaderboardAsync(
            Mathf.FloorToInt(score),
            PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player"),
            scoreText?.text,
            BuildMetadata());
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
}
