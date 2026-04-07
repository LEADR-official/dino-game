using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr;
using Leadr.Models;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles all LEADR SDK interaction for the dino game demo.
///
/// This script demonstrates:
/// - Initializing the LeadrClient singleton
/// - Submitting scores to multiple boards in parallel
/// - Fetching leaderboard scores centered on the player's position
/// - Displaying scores in a UI list
///
/// See https://docs.leadr.gg/latest/sdks/unity/ for full SDK documentation.
/// </summary>
public class LeadrIntegration : MonoBehaviour
{
    [Header("LEADR Configuration")]
    [SerializeField] private LeadrSettings settings;

    [Header("Board IDs")]
    [Tooltip("Board that records every run")]
    [SerializeField] private string allRunsBoardId = "brd_956bad96-8a3c-4517-baa0-3c223a6f6e82";

    [Tooltip("Board that keeps only each player's best score")]
    [SerializeField] private string bestScoreBoardId = "brd_a44f93db-9d23-4308-a301-0e4166da2ddb";

    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardPanel;
    [SerializeField] private Transform scoreListContainer;
    [SerializeField] private GameObject scoreEntryPrefab;

    [Header("Settings")]
    [SerializeField] private int leaderboardLimit = 10;

    /// <summary>
    /// Initialize the LEADR SDK. Call once on startup.
    /// </summary>
    public void Initialize()
    {
        LeadrClient.Instance.Initialize(settings);
    }

    /// <summary>
    /// Submits the score to both boards and displays the leaderboard centered on the player's score.
    /// Called on game over.
    /// </summary>
    public async Task SubmitScoreAndShowLeaderboardAsync(
        long score,
        string playerName,
        string displayValue,
        Dictionary<string, object> metadata)
    {
        // Submit to both boards in parallel - the backend handles best-score deduplication
        var submitAllRuns = LeadrClient.Instance.SubmitScoreAsync(
            allRunsBoardId, score, playerName, displayValue, metadata);

        var submitBestScore = LeadrClient.Instance.SubmitScoreAsync(
            bestScoreBoardId, score, playerName, displayValue, metadata);

        // Fetch the leaderboard centered on the player's score value
        var fetchLeaderboard = LeadrClient.Instance.GetScoresAsync(
            bestScoreBoardId,
            limit: leaderboardLimit,
            aroundScoreValue: score);

        // Wait for all three requests to complete
        await Task.WhenAll(submitAllRuns, submitBestScore, fetchLeaderboard);

        // Log any submission errors
        if (!submitAllRuns.Result.IsSuccess)
            Debug.LogError($"[LEADR] Failed to submit to all-runs board: {submitAllRuns.Result.Error}");

        if (!submitBestScore.Result.IsSuccess)
            Debug.LogError($"[LEADR] Failed to submit to best-score board: {submitBestScore.Result.Error}");

        // Display the leaderboard
        if (fetchLeaderboard.Result.IsSuccess)
        {
            ShowLeaderboard(fetchLeaderboard.Result.Data, playerName);
        }
        else
        {
            Debug.LogError($"[LEADR] Failed to fetch leaderboard: {fetchLeaderboard.Result.Error}");
        }
    }

    /// <summary>
    /// Shows the leaderboard panel and populates it with scores.
    /// </summary>
    private void ShowLeaderboard(PagedResult<Score> scores, string currentPlayerName)
    {
        leaderboardPanel.gameObject.SetActive(true);
        ClearScoreList();

        foreach (var score in scores.Items)
        {
            var entry = Instantiate(scoreEntryPrefab, scoreListContainer);

            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                // First text: rank + player name, second text: score value
                texts[0].text = $"#{score.Rank}  {score.PlayerName}";

                var displayValue = !string.IsNullOrEmpty(score.ValueDisplay)
                    ? score.ValueDisplay
                    : score.Value.ToString("N0");
                texts[1].text = displayValue;

                // Highlight the current player's entry
                if (score.PlayerName == currentPlayerName)
                {
                    foreach (var text in texts)
                        text.color = Color.yellow;
                }
            }
        }
    }

    /// <summary>
    /// Hides the leaderboard panel. Called when starting a new game.
    /// </summary>
    public void HideLeaderboard()
    {
        leaderboardPanel.gameObject.SetActive(false);
    }

    private void ClearScoreList()
    {
        foreach (Transform child in scoreListContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
