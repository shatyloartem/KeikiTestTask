using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Levels;
using Core.StateMachine;
using Core.UI;
using Cysharp.Threading.Tasks;
using Runtime.States;
using UnityEngine;

namespace Runtime.UI.Menu
{
    public sealed class MenuPresenter : UIPresenter<MenuView>
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly ILevelRepository _levelRepository;
        private readonly ILevelIconProvider _iconProvider;
        private readonly ISelectedLevelService _selectedLevelService;

        private bool _transitionRequested;

        public MenuPresenter(
            MenuView view,
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelIconProvider iconProvider,
            ISelectedLevelService selectedLevelService)
            : base(view)
        {
            _stateMachine = stateMachine;
            _levelRepository = levelRepository;
            _iconProvider = iconProvider;
            _selectedLevelService = selectedLevelService;
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
            LoadLevelsAsync(View.LifetimeToken).Forget();
        }

        private void HandleLevelSelected(LevelDefinition level)
        {
            if (_transitionRequested || _stateMachine.IsTransitioning)
                return;

            _selectedLevelService.Select(level);
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
                if (View)
                    View.SetInteractionEnabled(true);
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
