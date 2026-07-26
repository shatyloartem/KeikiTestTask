using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _content;

        private Tween _fadeTween;

        public bool IsVisible => _content.alpha > 0f;

        public CancellationToken LifetimeToken => destroyCancellationToken;
        
        public virtual void Show(float fadeTime = 0f)
        {
            StartFade(1f, fadeTime, true);
        }

        public virtual void Hide(float fadeTime = 0f)
        {
            StartFade(0f, fadeTime, false);
        }

        public virtual void SetInteractionEnabled(bool isEnabled)
        {
            _content.blocksRaycasts = isEnabled;
            _content.interactable = isEnabled;
        }

        protected virtual void OnDestroy()
        {
            KillFade();
        }

        private void StartFade(
            float targetAlpha,
            float fadeTime,
            bool enableInteractionOnComplete)
        {
            KillFade();

            if (fadeTime <= 0f)
            {
                _content.alpha = targetAlpha;
                SetInteractionEnabled(enableInteractionOnComplete);
                return;
            }

            _content.blocksRaycasts = false;
            _content.interactable = false;

            _fadeTween = DOTween
                .To(
                    () => _content.alpha,
                    alpha => _content.alpha = alpha,
                    targetAlpha,
                    fadeTime)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _content.alpha = targetAlpha;
                    SetInteractionEnabled(enableInteractionOnComplete);
                    _fadeTween = null;
                });
        }

        private void KillFade()
        {
            if (_fadeTween == null)
                return;

            _fadeTween.Kill();
            _fadeTween = null;
        }
    }
}
