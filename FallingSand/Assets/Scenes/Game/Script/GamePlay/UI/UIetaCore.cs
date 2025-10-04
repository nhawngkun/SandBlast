using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIetaCore : UICanvas
{
    [Header("Score Effect UI")]
    [SerializeField] private TextMeshProUGUI scoreEffectText;

    [Header("Animation Settings")]
    [SerializeField] private float moveUpDistance = 150f; // Khoảng cách di chuyển lên
    [SerializeField] private float animationDuration = 1.2f; // Tổng thời gian animation
    [SerializeField] private float fadeStartDelay = 0.6f; // Delay trước khi bắt đầu fade
    [SerializeField] private float scaleUpAmount = 1.3f; // Scale phóng to
    [SerializeField] private float scaleUpDuration = 0.2f; // Thời gian scale up
    [SerializeField] private Ease moveEase = Ease.OutCubic; // Kiểu easing cho di chuyển
    [SerializeField] private Ease scaleEase = Ease.OutBack; // Kiểu easing cho scale
    
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Sequence currentSequence;

    void Start()
    {
        if (scoreEffectText != null)
        {
            // Lưu vị trí và scale gốc
            originalPosition = scoreEffectText.rectTransform.anchoredPosition;
            originalScale = scoreEffectText.rectTransform.localScale;
            scoreEffectText.gameObject.SetActive(false);
        }
    }

    public void ShowScoreEffect(int score, Color color)
    {
        if (scoreEffectText == null) return;

        // Hủy animation đang chạy (nếu có)
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        // Reset về trạng thái ban đầu
        scoreEffectText.rectTransform.anchoredPosition = originalPosition;
        scoreEffectText.rectTransform.localScale = originalScale;
        scoreEffectText.alpha = 0f;

        // Set text và màu
        scoreEffectText.text = "+" + FormatScore(score);
        scoreEffectText.color = color;
        scoreEffectText.gameObject.SetActive(true);

        // Tạo sequence animation
        currentSequence = DOTween.Sequence();

        // 1. Fade In nhanh
        currentSequence.Append(
            scoreEffectText.DOFade(1f, 0.15f).SetEase(Ease.OutQuad)
        );

        // 2. Scale up với bounce effect (đồng thời với fade in)
        currentSequence.Join(
            scoreEffectText.rectTransform.DOScale(originalScale * scaleUpAmount, scaleUpDuration)
                .SetEase(scaleEase)
        );

        // 3. Scale về bình thường
        currentSequence.Append(
            scoreEffectText.rectTransform.DOScale(originalScale, 0.15f)
                .SetEase(Ease.InOutQuad)
        );

        // 4. Di chuyển lên (bắt đầu ngay sau scale)
        currentSequence.Join(
            scoreEffectText.rectTransform.DOAnchorPosY(originalPosition.y + moveUpDistance, animationDuration)
                .SetEase(moveEase)
        );

        // 5. Fade out (sau một khoảng delay)
        currentSequence.Insert(
            fadeStartDelay,
            scoreEffectText.DOFade(0f, animationDuration - fadeStartDelay)
                .SetEase(Ease.InQuad)
        );

        // 6. Callback khi hoàn thành
        currentSequence.OnComplete(() =>
        {
            scoreEffectText.gameObject.SetActive(false);
            scoreEffectText.rectTransform.anchoredPosition = originalPosition;
            scoreEffectText.rectTransform.localScale = originalScale;
        });
    }

    /// <summary>
    /// Hiệu ứng đơn giản hơn - chỉ bay lên và fade
    /// </summary>
    public void ShowScoreEffectSimple(int score, Color color)
    {
        if (scoreEffectText == null) return;

        // Hủy animation đang chạy
        scoreEffectText.DOKill();

        // Reset
        scoreEffectText.rectTransform.anchoredPosition = originalPosition;
        scoreEffectText.alpha = 1f;

        // Set text và màu
        scoreEffectText.text = "+" + FormatScore(score);
        scoreEffectText.color = color;
        scoreEffectText.gameObject.SetActive(true);

        // Animation đơn giản
        Sequence seq = DOTween.Sequence();
        
        seq.Append(
            scoreEffectText.rectTransform.DOAnchorPosY(originalPosition.y + moveUpDistance, animationDuration)
                .SetEase(Ease.OutQuad)
        );
        
        seq.Join(
            scoreEffectText.DOFade(0f, animationDuration * 0.7f)
                .SetDelay(animationDuration * 0.3f)
        );
        
        seq.OnComplete(() =>
        {
            scoreEffectText.gameObject.SetActive(false);
            scoreEffectText.rectTransform.anchoredPosition = originalPosition;
        });
    }

    /// <summary>
    /// Hiệu ứng với chuyển động cong (arc motion)
    /// </summary>
    public void ShowScoreEffectArc(int score, Color color, float arcAmount = 50f)
    {
        if (scoreEffectText == null) return;

        // Hủy animation đang chạy
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        // Reset
        scoreEffectText.rectTransform.anchoredPosition = originalPosition;
        scoreEffectText.rectTransform.localScale = originalScale;
        scoreEffectText.alpha = 0f;

        // Set text và màu
        scoreEffectText.text = "+" + FormatScore(score);
        scoreEffectText.color = color;
        scoreEffectText.gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();

        // Fade in + scale up
        currentSequence.Append(scoreEffectText.DOFade(1f, 0.15f));
        currentSequence.Join(
            scoreEffectText.rectTransform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack)
        );

        // Di chuyển theo đường cong (lên và sang phải)
        Vector2 endPos = new Vector2(originalPosition.x + arcAmount, originalPosition.y + moveUpDistance);
        
        currentSequence.Append(
            scoreEffectText.rectTransform.DOAnchorPos(endPos, animationDuration)
                .SetEase(Ease.OutQuad)
        );

        // Scale nhỏ dần
        currentSequence.Join(
            scoreEffectText.rectTransform.DOScale(originalScale * 0.8f, animationDuration)
                .SetEase(Ease.InQuad)
        );

        // Fade out
        currentSequence.Insert(
            fadeStartDelay,
            scoreEffectText.DOFade(0f, animationDuration - fadeStartDelay)
        );

        currentSequence.OnComplete(() =>
        {
            scoreEffectText.gameObject.SetActive(false);
            scoreEffectText.rectTransform.anchoredPosition = originalPosition;
            scoreEffectText.rectTransform.localScale = originalScale;
        });
    }

    /// <summary>
    /// Hiệu ứng popup với punch scale
    /// </summary>
    public void ShowScoreEffectPunch(int score, Color color)
    {
        if (scoreEffectText == null) return;

        scoreEffectText.DOKill();

        // Reset
        scoreEffectText.rectTransform.anchoredPosition = originalPosition;
        scoreEffectText.rectTransform.localScale = originalScale;
        scoreEffectText.alpha = 1f;

        scoreEffectText.text = "+" + FormatScore(score);
        scoreEffectText.color = color;
        scoreEffectText.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // Punch scale effect
        seq.Append(
            scoreEffectText.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f)
        );

        // Di chuyển lên
        seq.Join(
            scoreEffectText.rectTransform.DOAnchorPosY(originalPosition.y + moveUpDistance, animationDuration)
                .SetEase(Ease.OutCubic)
        );

        // Fade out
        seq.Insert(
            0.5f,
            scoreEffectText.DOFade(0f, 0.7f)
        );

        seq.OnComplete(() =>
        {
            scoreEffectText.gameObject.SetActive(false);
            scoreEffectText.rectTransform.anchoredPosition = originalPosition;
            scoreEffectText.rectTransform.localScale = originalScale;
        });
    }

    private string FormatScore(int score)
    {
        if (score >= 1_000_000)
            return (score / 1_000_000f).ToString("0.#") + "M";
        if (score >= 1_000)
            return (score / 1_000f).ToString("0.#") + "K";
        return score.ToString();
    }

    void OnDestroy()
    {
        // Cleanup: Kill tất cả tweens khi destroy
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        
        if (scoreEffectText != null)
        {
            scoreEffectText.DOKill();
        }
    }
}