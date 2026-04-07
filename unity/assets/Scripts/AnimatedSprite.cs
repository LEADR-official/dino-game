using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    public Sprite[] sprites;
    private SpriteRenderer spriteRenderer;
    private int frame;
    bool isAnimating = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        TryStartAnimation();
    }
    private void Update()
    {
        if (GameManager.Instance.isGameStarted && !isAnimating)
        {
            TryStartAnimation();
        }
        else if (!GameManager.Instance.isGameStarted && isAnimating)
        {
            StopAnimation();
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }
    private void TryStartAnimation()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        isAnimating = true;
        frame = 0;

        InvokeRepeating(
            nameof(Animate),
            0f,
            1f / GameManager.Instance.gameSpeed
        );
    }

    private void StopAnimation()
    {
        isAnimating = false;
        CancelInvoke(nameof(Animate));
    }

    private void Animate()
    {
        frame = (frame + 1) % sprites.Length;
        spriteRenderer.sprite = sprites[frame];

        CancelInvoke(nameof(Animate));
        InvokeRepeating(
            nameof(Animate),
            1f / GameManager.Instance.gameSpeed,
            1f / GameManager.Instance.gameSpeed
        );
    }

}
