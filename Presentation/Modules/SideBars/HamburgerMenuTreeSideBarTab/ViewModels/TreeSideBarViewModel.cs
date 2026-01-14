using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Unity;
using Unity;

using Aksl.Infrastructure;
using Aksl.Toolkit.Services;

namespace Aksl.Modules.HamburgerMenuTreeSideBarTab.ViewModels
{
    public class TreeSideBarViewModel : BindableBase
    {
        #region Members
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogViewService _dialogViewService;
        private readonly IMenuService _menuService;
        #endregion

        #region Constructors
        public TreeSideBarViewModel(IEventAggregator eventAggregator, IMenuService menuService)
        {
            _eventAggregator = eventAggregator;
            _dialogViewService = (PrismApplication.Current as PrismApplicationBase).Container.Resolve<IDialogViewService>();
            _menuService = menuService;

            TopTreeSideBarItems = new();
            AllTreeSideBarItems = new();

            RegisterActiveTabItemEvent();
            RegisterOnSelectedTabItemEmptyEvent();
        }
        #endregion

        #region Properties
        public ObservableCollection<TreeSideBarItemViewModel> TopTreeSideBarItems { get; }

        public ObservableCollection<TreeSideBarItemViewModel> AllTreeSideBarItems { get; }

        internal TreeSideBarItemViewModel _previewSelectedTreeSideBarItem;
        internal TreeSideBarItemViewModel PreviewSelectedTreeSideBarItem => _previewSelectedTreeSideBarItem;

        private TreeSideBarItemViewModel _selectedTreeSideBarItem;
        public TreeSideBarItemViewModel SelectedTreeSideBarItem
        {
            get => _selectedTreeSideBarItem;
            set
            {
                if (SetProperty(ref _selectedTreeSideBarItem, value))
                {
                    if (_selectedTreeSideBarItem is not null)
                    {
                         _selectedTreeSideBarItem.IsSelected = true;
                    }
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty<bool>(ref _isLoading, value);
        }
        #endregion

        #region Get Selected TreeSideBarItemViewModel Method
        private TreeSideBarItemViewModel GetSelectedTreeSideBarItemViewModel()
        {
            TreeSideBarItemViewModel findTreeSideBarItemViewModel = null;

            foreach (var tbi in AllTreeSideBarItems)
            {
                if (findTreeSideBarItemViewModel is null)
                {
                    RecursiveSubMenuItem(tbi);
                }
                else
                {
                    break;
                }
            }

            void RecursiveSubMenuItem(TreeSideBarItemViewModel curentTreeSideBarItemViewModel)
            {
                //if (curentTreeSideBarItemViewModel.IsLeaf && curentTreeSideBarItemViewModel.IsSelected)
                if (curentTreeSideBarItemViewModel.IsSelected)
                {
                    findTreeSideBarItemViewModel = curentTreeSideBarItemViewModel;
                    return;
                }

                if (HasChild(curentTreeSideBarItemViewModel))
                {
                    foreach (var tbvm in curentTreeSideBarItemViewModel.Children)
                    {
                        RecursiveSubMenuItem(tbvm);
                    }
                }
            }

            bool HasChild(TreeSideBarItemViewModel tivm) => (tivm is not null) && tivm.Children.Any();

            return findTreeSideBarItemViewModel;
        }

        private TreeSideBarItemViewModel FindtTreeSideBarItemViewModel(TabInformation tabInformation )
        {
            TreeSideBarItemViewModel findTreeSideBarItemViewModel = null;

            foreach (var tbi in AllTreeSideBarItems)
            {
                if (findTreeSideBarItemViewModel is null)
                {
                    RecursiveSubMenuItem(tbi);
                }
                else
                {
                    break;
                }
            }

            void RecursiveSubMenuItem(TreeSideBarItemViewModel curentTreeSideBarItemViewModel)
            {
                if (IsEqualsNameOrTitle(curentTreeSideBarItemViewModel.Name, tabInformation.Name) || IsEqualsNameOrTitle(curentTreeSideBarItemViewModel.Title, tabInformation.Title))
                {
                    findTreeSideBarItemViewModel = curentTreeSideBarItemViewModel;
                    return;
                }

                if (HasChild(curentTreeSideBarItemViewModel))
                {
                    foreach (var tbvm in curentTreeSideBarItemViewModel.Children)
                    {
                        RecursiveSubMenuItem(tbvm);
                    }
                }
            }

            bool HasChild(TreeSideBarItemViewModel tivm) => (tivm is not null) && tivm.Children.Any();

            return findTreeSideBarItemViewModel;
        }
        #endregion

        #region Register SelectedTabItem Empty Event
        private void RegisterOnSelectedTabItemEmptyEvent()
        {
            _eventAggregator.GetEvent<OnSelectedTabItemEmptyEvent>().Subscribe(async (oatie) =>
            {
                try
                {
                    var selectedTreeSideBarItem = GetSelectedTreeSideBarItemViewModel();
                    if (selectedTreeSideBarItem is not null)
                    {
                        selectedTreeSideBarItem.IsSelected = false;
                        selectedTreeSideBarItem = null;
                    }
                }
                catch (Exception ex)
                {
                    await _dialogViewService.AlertAsync(message: $"Exception : \"{ex.Message}\"", title: "Error: Selected TabItem Is Empty");
                }
            }, ThreadOption.UIThread, true);
        }
        #endregion

        #region Register Active TabItem Event
        private void RegisterActiveTabItemEvent()
        {
            _eventAggregator.GetEvent<OnActiveTabItemEvent>().Subscribe(async (oatie) =>
            {
                var currentTabItem = oatie.SelectedTabItem;

                try
                {
                    SetSelectedTreeSideBarItem();

                    #region Set Selected TreeSideBarItem Method
                    void SetSelectedTreeSideBarItem()
                    {
                        var treeSideBarItem = FindtTreeSideBarItemViewModel(currentTabItem);

                        var selectedTreeSideBarItem = GetSelectedTreeSideBarItemViewModel();
                        Debug.Assert(selectedTreeSideBarItem == _selectedTreeSideBarItem);

                        if (treeSideBarItem is not null)
                        {
                            if (treeSideBarItem != selectedTreeSideBarItem)
                            {
                                treeSideBarItem.IsSelected = true;
                                treeSideBarItem.IsExpanded = true;

                                if (selectedTreeSideBarItem is not null)
                                {
                                    selectedTreeSideBarItem.IsSelected = false;
                                }
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    await _dialogViewService.AlertAsync(message: $"Exception : \"{ex.Message}\"", title: "Error: Active TabItem");
                }
            }, ThreadOption.UIThread, true);
        }
        #endregion

        #region Reset/Clear Selected TreeSideBarItem Method
        internal void ClearSelectedTreeSideBarItem()
        {
            if (SelectedTreeSideBarItem is not null)
            {
                SelectedTreeSideBarItem.IsSelected = false;
                SelectedTreeSideBarItem = null;
                _previewSelectedTreeSideBarItem = null;
            }
        }

        internal void ResetSelectedTreeSideBarItem(TreeSideBarItemViewModel selectedTreeSideBarItem)
        {
            if (selectedTreeSideBarItem is not null)
            {
                if (_selectedTreeSideBarItem is not null)
                {
                    _selectedTreeSideBarItem.IsSelected = false;
                }

                _previewSelectedTreeSideBarItem = null;
                _selectedTreeSideBarItem = selectedTreeSideBarItem;
                _selectedTreeSideBarItem.IsSelected = true;
            }
        }
        #endregion

        #region Create TreeSideBarItem ViewModel Method
        internal async Task CreateTreeSideBarItemViewModelsAsync()
        {
            IsLoading = true;

            var rootMenuItem = await _menuService.GetMenuAsync("All");

            var subMenuItems = rootMenuItem.SubMenus;
            foreach (var smi in subMenuItems)
            {
                TreeSideBarItemViewModel treeSideBarItemViewModel = new(_eventAggregator, smi);
                TopTreeSideBarItems.Add(treeSideBarItemViewModel);

                TreeSideBarItemViewModel parent = new(_eventAggregator, smi);
                AllTreeSideBarItems.Add(parent);
                List<MenuItem> allTravelMenuItems = new();
                await GetAllTreeBarItemSubViewModelsAsync(smi, allTravelMenuItems, parent);
            }

            SetPropertyChanged();

            void SetPropertyChanged()
            {
                foreach (var tbi in AllTreeSideBarItems)
                {
                    RecursiveSubMenuItem(tbi);
                }

                void RecursiveSubMenuItem(TreeSideBarItemViewModel treeSideBarItemViewModel)
                {
                    AddPropertyChanged(treeSideBarItemViewModel);

                    foreach (var smi in treeSideBarItemViewModel.Children)
                    {
                        RecursiveSubMenuItem(smi);
                    }
                }

                void AddPropertyChanged(TreeSideBarItemViewModel treeSideBarItemViewModel)
                {
                    treeSideBarItemViewModel.PropertyChanged += (sender, e) =>
                    {
                        if (sender is TreeSideBarItemViewModel tsbivm)
                        {
                            if (e.PropertyName == nameof(TreeSideBarItemViewModel.IsSelected))
                            {
                                if (tsbivm.IsSelected)
                                {
                                    _selectedTreeSideBarItem = tsbivm;
                                }
                                else
                                {
                                    _previewSelectedTreeSideBarItem = tsbivm;
                                }
                            }
                        }
                    };
                }
            }

            IsLoading = false;
        }
        #endregion

        #region Get All TreeBarItem SubViewModels Methods
        private async Task GetAllTreeBarItemSubViewModelsAsync(MenuItem menuItem, IList<MenuItem> travelMenuItems, TreeSideBarItemViewModel currentTreeBarItemViewModel)
        {
            #region Method

            await RecursiveSubMenuItem(menuItem);

            async Task RecursiveSubMenuItem(MenuItem currentMenuItem)
            {
                if (!AnyEqualsMenuItem(travelMenuItems, currentMenuItem))
                {
                    travelMenuItems.Add(currentMenuItem);
                }

                var matchResult = FindMatchViewModel(currentTreeBarItemViewModel, currentMenuItem);
                Debug.Assert(matchResult.IsTrue);
                if (HasNavigationName(currentMenuItem) && IsLeaf(currentMenuItem) && matchResult.FindTreeBarItemViewModel.IsLeaf)
                {
                    currentMenuItem = await _menuService.GetMenuAsync(currentMenuItem.NavigationName);

                    if (HasSubMenu(currentMenuItem))
                    {
                        var parent = matchResult.FindTreeBarItemViewModel;

                        foreach (var smi in currentMenuItem.SubMenus)
                        {
                            TreeSideBarItemViewModel barItemViewModel = new(_eventAggregator, smi, parent);
                            parent.Children.Add(barItemViewModel);
                        }
                    }
                }

                if (HasSubMenu(currentMenuItem))
                {
                    foreach (var smi in currentMenuItem.SubMenus)
                    {
                        await RecursiveSubMenuItem(smi);
                    }
                }
            }
            #endregion

            bool HasSubMenu(MenuItem mi) => (mi is not null) && mi.SubMenus.Any();

            bool IsLeaf(MenuItem mi) => (mi is not null) && mi.SubMenus.Count <= 0;

            bool HasNavigationName(MenuItem mi) => (mi is not null) && !string.IsNullOrEmpty(mi.NavigationName);
        }

        private (TreeSideBarItemViewModel FindTreeBarItemViewModel, bool IsTrue) FindMatchViewModel(TreeSideBarItemViewModel treeBarItemViewModel, MenuItem menuItem)
        {
            var findViewModel = FindByNameOrTitle(treeBarItemViewModel, menuItem.Name);
            if (findViewModel is null)
            {
                findViewModel = FindByNameOrTitle(treeBarItemViewModel, menuItem.Title);
            }

            return (FindTreeBarItemViewModel: findViewModel, IsTrue: (findViewModel is not null));
        }

        private TreeSideBarItemViewModel FindByNameOrTitle(TreeSideBarItemViewModel treeBarItemViewModel, string nameOrTitle)
        {
            TreeSideBarItemViewModel findBarItemViewModel = null;

            RecursiveSubMenuItemViewModel(treeBarItemViewModel);

            void RecursiveSubMenuItemViewModel(TreeSideBarItemViewModel parent)
            {
                if (NameOrTitlelsEquals(parent, nameOrTitle))
                {
                    findBarItemViewModel = parent;
                    return;
                }

                if (HasChild(parent))
                {
                    foreach (var children in parent.Children)
                    {
                        RecursiveSubMenuItemViewModel(children);
                    }
                }
            }

            bool HasChild(TreeSideBarItemViewModel tbivm) => (tbivm is not null) && tbivm.Children.Any();

            return findBarItemViewModel;
        }
        #endregion

        #region Contain Methods
        private bool AnyEqualsMenuItem(IEnumerable<MenuItem> menuItems, MenuItem menuItem)
        {
            var isEquals = menuItems.Any(mi => IsEqualsNameOrTitle(mi.Title, menuItem.Title) || IsEqualsNameOrTitle(mi.Name, menuItem.Name));

            return isEquals;
        }
        private bool NameOrTitlelsEquals(TreeSideBarItemViewModel treeBarItemViewModel, string nameOrTitle)
        {
            var isEquals = IsEqualsNameOrTitle(treeBarItemViewModel.Name, nameOrTitle) || IsEqualsNameOrTitle(treeBarItemViewModel.Title, nameOrTitle);

            return isEquals;
        }

        private bool IsEqualsNameOrTitle(string nameOrTitle, string otherNameOrTitle)
        {
            if (string.IsNullOrEmpty(nameOrTitle) || string.IsNullOrEmpty(otherNameOrTitle))
            {
                return false;
            }

            var isEquals = (!string.IsNullOrEmpty(nameOrTitle) && otherNameOrTitle.Equals(nameOrTitle, StringComparison.InvariantCultureIgnoreCase)) ||
                           (!string.IsNullOrEmpty(nameOrTitle) && otherNameOrTitle.Equals(nameOrTitle, StringComparison.InvariantCultureIgnoreCase));

            return isEquals;
        }
        #endregion
    }
}
