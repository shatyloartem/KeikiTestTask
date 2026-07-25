using System.Threading;
using UnityEngine;

namespace Core.UI
{
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _content;
        
        public bool IsVisible => gameObject.activeSelf;

        public CancellationToken LifetimeToken => destroyCancellationToken;
        
        public virtual void Show()
        {
            _content.alpha = 1;
            _content.blocksRaycasts = true;
            _content.interactable = true;
        }

        public virtual void Hide()
        {
            _content.alpha = 0;
            _content.blocksRaycasts = false;
            _content.interactable = false;
        }

        public virtual void SetInteractionEnabled(bool isEnabled)
        {
            _content.blocksRaycasts = isEnabled;
            _content.interactable = isEnabled;
        }
    }
}
