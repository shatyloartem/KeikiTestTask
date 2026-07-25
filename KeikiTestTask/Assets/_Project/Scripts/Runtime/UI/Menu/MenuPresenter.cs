using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.StateMachine;
using Core.UI;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Storage.Levels;
using Runtime.Services.Levels;
using Runtime.States;
using UnityEngine;

namespace Runtime.UI.Menu
{
    public sealed class MenuPresenter : UIPresenter<MenuView>
    {
        private const float FadeInTime = 0.35f;

        private readonly IGameStateMachine _stateMachine;
        private readonly ILevelRepository _levelRepository;
        private readonly ILevelIconProvider _iconProvider;
        private readonly ISelectedLevelStore _selectedLevelStore;

        private bool _transitionRequested;

        protected override bool ShowViewOnInitialize => false;

        public MenuPresenter(
            MenuView view,
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelIconProvider iconProvider,
            ISelectedLevelStore selectedLevelStore)
            : base(view)
        {
            _stateMachine = stateMachine;
            _levelRepository = levelRepository;
            _iconProvider = iconProvider;
            _selectedLevelStore = selectedLevelStore;
        }

        protected override void SubscribeToEvents()
        {
            View.LevelSelected += HandleLevelSelected;
        }

        protected override void UnsubscribeFromEvents()
        {
            View.LevelSelected -= HandleLevelSelected;
        }

        protected override void OnInitialized()
        {
            View.Hide();
            LoadLevelsAsync(View.LifetimeToken).Forget();
        }

        private void HandleLevelSelected(LevelDefinition level)
        {
            if (_transitionRequested || _stateMachine.IsTransitioning)
                return;

            _selectedLevelStore.Select(level);
            TransitionToGameAsync(View.LifetimeToken).Forget();
        }

        private async UniTask LoadLevelsAsync(CancellationToken cancellationToken)
        {
            View.SetInteractionEnabled(false);

            try
            {
                LevelCatalog catalog = await _levelRepository.LoadAsync(cancellationToken);

                string[] addresses = catalog.Categories
                    .SelectMany(category => category.Levels)
                    .Select(level => level.IconAddress)
                    .Distinct()
                    .ToArray();

                Dictionary<string, Sprite> icons = new(addresses.Length);

                foreach (string address in addresses)
                {
                    Sprite icon = await _iconProvider.LoadAsync(address, cancellationToken);
                    icons.Add(address, icon);
                }

                View.RenderCatalog(catalog, icons);
                View.Show(FadeInTime);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, View);
            }
        }

        private async UniTask TransitionToGameAsync(CancellationToken cancellationToken)
        {
            _transitionRequested = true;
            View.SetInteractionEnabled(false);

            try
            {
                await _stateMachine.EnterAsync<GameState>(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, View);
            }
            finally
            {
                _transitionRequested = false;

                if (View)
                    View.SetInteractionEnabled(true);
            }
        }
    }
}
