using System;
using Bit.iOS.Extension.Models;
using UIKit;
using XLabs.Ioc;
using Plugin.Settings.Abstractions;
using Foundation;
using Bit.iOS.Core.Views;
using Bit.App.Resources;
using Bit.iOS.Core.Utilities;
using Bit.App.Abstractions;
using System.Linq;
using Bit.App;
using Bit.iOS.Core.Controllers;

namespace Bit.iOS.Extension
{
    public partial class LockPasswordViewController : ExtendedUITableViewController
    {
        private ISettings _settings;
        private IAuthService _authService;
        private ICryptoService _cryptoService;

        public LockPasswordViewController(IntPtr handle) : base(handle)
        { }

        public Context Context { get; set; }
        public LoadingViewController LoadingController { get; set; }
        public FormEntryTableViewCell MasterPasswordCell { get; set; } = new FormEntryTableViewCell(
            AppResources.MasterPassword, useLabelAsPlaceholder: true);

        public override void ViewWillAppear(bool animated)
        {
            UINavigationBar.Appearance.ShadowImage = new UIImage();
            UINavigationBar.Appearance.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
            base.ViewWillAppear(animated);
        }

        public override void ViewDidLoad()
        {
            _settings = Resolver.Resolve<ISettings>();
            _authService = Resolver.Resolve<IAuthService>();
            _cryptoService = Resolver.Resolve<ICryptoService>();

            View.BackgroundColor = new UIColor(red: 0.94f, green: 0.94f, blue: 0.96f, alpha: 1.0f);

            var descriptor = UIFontDescriptor.PreferredBody;

            MasterPasswordCell.TextField.SecureTextEntry = true;
            MasterPasswordCell.TextField.ReturnKeyType = UIReturnKeyType.Go;
            MasterPasswordCell.TextField.ShouldReturn += (UITextField tf) =>
            {
                CheckPassword();
                return true;
            };

            TableView.RowHeight = UITableView.AutomaticDimension;
            TableView.EstimatedRowHeight = 70;
            TableView.Source = new TableSource(this);
            TableView.AllowsSelection = true;

            base.ViewDidLoad();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            MasterPasswordCell.TextField.BecomeFirstResponder();
        }

        partial void SubmitButton_Activated(UIBarButtonItem sender)
        {
            CheckPassword();
        }

        private void CheckPassword()
        {
            if(string.IsNullOrWhiteSpace(MasterPasswordCell.TextField.Text))
            {
                var alert = Dialogs.CreateAlert(AppResources.AnErrorHasOccurred,
                    string.Format(AppResources.ValidationFieldRequired, AppResources.MasterPassword), AppResources.Ok);
                PresentViewController(alert, true, null);
                return;
            }

            var key = _cryptoService.MakeKeyFromPassword(MasterPasswordCell.TextField.Text, _authService.Email);
            if(key.SequenceEqual(_cryptoService.Key))
            {
                _settings.AddOrUpdateValue(Constants.Locked, false);
                MasterPasswordCell.TextField.ResignFirstResponder();
                LoadingController.DismissLockAndContinue();
            }
            else
            {
                // TODO: keep track of invalid attempts and logout?

                var alert = Dialogs.CreateAlert(AppResources.AnErrorHasOccurred,
                    string.Format(null, AppResources.InvalidMasterPassword), AppResources.Ok, (a) =>
                    {

                        MasterPasswordCell.TextField.Text = string.Empty;
                        MasterPasswordCell.TextField.BecomeFirstResponder();
                    });

                PresentViewController(alert, true, null);
            }
        }

        partial void CancelButton_Activated(UIBarButtonItem sender)
        {
            LoadingController.CompleteRequest(null);
        }

        public class TableSource : UITableViewSource
        {
            private LockPasswordViewController _controller;

            public TableSource(LockPasswordViewController controller)
            {
                _controller = controller;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                if(indexPath.Section == 0)
                {
                    if(indexPath.Row == 0)
                    {
                        return _controller.MasterPasswordCell;
                    }
                }

                return new UITableViewCell();
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return UITableView.AutomaticDimension;
            }

            public override nint NumberOfSections(UITableView tableView)
            {
                return 1;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                if(section == 0)
                {
                    return 1;
                }

                return 0;
            }

            public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            {
                return UITableView.AutomaticDimension;
            }

            public override string TitleForHeader(UITableView tableView, nint section)
            {
                return null;
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow(indexPath, true);
                tableView.EndEditing(true);

                var cell = tableView.CellAt(indexPath);
                if(cell == null)
                {
                    return;
                }

                var selectableCell = cell as ISelectable;
                if(selectableCell != null)
                {
                    selectableCell.Select();
                }
            }
        }
    }
}
