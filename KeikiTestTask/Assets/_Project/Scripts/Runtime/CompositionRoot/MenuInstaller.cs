using System;
using Core.UI;
using Runtime.UI.Menu;
using UnityEngine;
using Zenject;

namespace Runtime.CompositionRoot
{
    public sealed class MenuInstaller : MonoInstaller
    {
        [SerializeField] private UIView[] _views;

        public override void InstallBindings()
        {
            ValidateViews();

            BindViews();

            BindPresenters();
        }

        private void ValidateViews()
        {
            if (_views == null || _views.Length == 0)
            {
                throw new InvalidOperationException(
                    $"At least one view must be assigned in {nameof(MenuInstaller)}.");
            }

            for (int i = 0; i < _views.Length; i++)
            {
                if (!_views[i])
                {
                    throw new InvalidOperationException(
                        $"View at index {i} is not assigned in {nameof(MenuInstaller)}.");
                }
            }
        }
        
        private void BindViews()
        {
            foreach (UIView view in _views)
            {
                Container.Bind<UIView>().FromInstance(view);
                Container.Bind(view.GetType()).FromInstance(view);
            }
        }

        private void BindPresenters()
        {
            Container
                .BindInterfacesAndSelfTo<MenuPresenter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
