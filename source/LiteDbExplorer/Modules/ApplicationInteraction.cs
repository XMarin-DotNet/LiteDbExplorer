﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using CSharpFunctionalExtensions;
using LiteDbExplorer.Controls;
using LiteDbExplorer.Core;
using LiteDbExplorer.Framework.Windows;
using LiteDbExplorer.Modules.Database;
using LiteDbExplorer.Modules.DbCollection;
using LiteDbExplorer.Modules.DbDocument;
using LiteDbExplorer.Modules.DbQuery;
using LiteDbExplorer.Modules.Help;
using LiteDbExplorer.Windows;
using LiteDbExplorer.Wpf.Framework;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LiteDbExplorer.Modules
{
    [Export(typeof(IApplicationInteraction))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ApplicationInteraction : IApplicationInteraction
    {
        private readonly IWindowManager _windowManager;
        private readonly IShellNavigationService _navigationService;

        [ImportingConstructor]
        public ApplicationInteraction(IWindowManager windowManager, IShellNavigationService navigationService)
        {
            _windowManager = windowManager;
            _navigationService = navigationService;
        }

        public bool OpenDatabaseProperties(DatabaseReference database)
        {
            var vm = IoC.Get<IDatabasePropertiesView>();
            vm.Init(database);
            
            var dialogOptions = new DialogOptions
            {
                Width = 480,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize
            };
            return _windowManager.ShowDialog(vm, null, dialogOptions.Value) == true;
        }

        public bool OpenEditDocument(DocumentReference document)
        {
            /*var vm = IoC.Get<DocumentEntryViewModel>();
            vm.Init(document);

            dynamic settings = new ExpandoObject();
            settings.Height = 600;
            settings.Width = 640;
            settings.SizeToContent = SizeToContent.Manual;

            return _windowManager.ShowDialog(vm, null, settings) == true;*/

            var windowController = new WindowController {Title = "Document Editor"};
            var control = new DocumentEntryControl(document, windowController);
            var window = new DialogWindow(control, windowController)
            {
                Height = Math.Min(Math.Max(636, SystemParameters.VirtualScreenHeight / 1.61), SystemParameters.VirtualScreenHeight)
            };
            if (document.Collection.IsFilesOrChunks)
            {
                window.Width = Math.Min(1024, SystemParameters.VirtualScreenWidth);
            }
            window.Owner = windowController.InferOwnerOf(window);

            return window.ShowDialog() == true;

            // TODO: Handle UpdateGridColumns(document.Value.LiteDocument) and UpdateDocumentPreview();
        }

        public async Task<Result> OpenQuery(RunQueryContext queryContext)
        {
            await _navigationService.Navigate<QueryViewModel>(queryContext);

            return Result.Ok();
        }


        public bool RevealInExplorer(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }

            //Clean up file path so it can be navigated OK
            filePath = System.IO.Path.GetFullPath(filePath);
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");

            return true;
        }

        public async Task<Result> ActivateDefaultCollectionView(CollectionReference collection, IEnumerable<DocumentReference> selectedDocuments = null)
        {
            if (collection == null)
            {
                return Result.Ok();
            }

            await _navigationService.Navigate<CollectionExplorerViewModel>(new CollectionReferencePayload(collection, selectedDocuments));

            return Result.Ok();
        }

        public async Task<Result> ActivateDefaultDocumentView(DocumentReference document)
        {
            if (document == null)
            {
                return Result.Ok();
            }

            await _navigationService.Navigate<DocumentPreviewViewModel>(new DocumentReferencePayload(document));

            return Result.Ok();
        }
        
        public void PutClipboardText(string text)
        {
            Clipboard.SetData(DataFormats.Text, text);
        }

        public bool ShowConfirm(string message, string title = "Are you sure?")
        {
            return MessageBox.Show(
                       message,
                       title,
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question
                   ) == MessageBoxResult.Yes;
        }

        public void ShowError(string message, string title = "")
        {
            MessageBox.Show(
                message,
                string.IsNullOrEmpty(title) ? "Error" : title,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public void ShowError(Exception exception, string message, string title = "")
        {
            var exceptionViewer = new ExceptionViewer(message, exception);
            var baseDialogWindow = new BaseDialogWindow
            {
                Title = string.IsNullOrEmpty(title) ? "Error" : title,
                Content = exceptionViewer,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResizeWithGrip,
                MinHeight = 400,
                MinWidth = 500,
                ShowMinButton = false,
                ShowMaxRestoreButton = false
            };
            baseDialogWindow.ShowDialog();
        }


        public void ShowAbout()
        {
            _windowManager.ShowDialog(IoC.Get<AboutViewModel>(), null, AboutViewModel.DefaultDialogOptions.Value);
        }

        public void ShowReleaseNotes(Version version = null)
        {
            var viewModel = IoC.Get<ReleaseNotesViewModel>();
            viewModel.FilterVersion(version);
            _windowManager.ShowDialog(viewModel, null, ReleaseNotesViewModel.DefaultDialogOptions.Value);
        }

        public void ShowIssueHelper()
        {
            var viewModel = IoC.Get<IssueHelperViewModel>();
            _windowManager.ShowDialog(viewModel, null, IssueHelperViewModel.DefaultDialogOptions.Value);
        }


        public Task<Maybe<string>> ShowSaveFileDialog(string title = "", string filter = "All files|*.*",
            string fileName = "", string initialDirectory = "", bool overwritePrompt = true)
        {
            var completionSource = new TaskCompletionSource<Maybe<string>>();

            var dialog = new SaveFileDialog
            {
                OverwritePrompt = overwritePrompt
            };

            if (!string.IsNullOrEmpty(fileName))
            {
                dialog.FileName = fileName;
            }

            if (!string.IsNullOrEmpty(filter))
            {
                dialog.Filter = filter;
            }

            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            completionSource.SetResult(dialog.ShowDialog() == true ? dialog.FileName : Maybe<string>.None);

            return completionSource.Task;
        }

        public Task<Maybe<string>> ShowOpenFileDialog(string title = "", string filter = "All files|*.*",
            string fileName = "", string initialDirectory = "")
        {
            var completionSource = new TaskCompletionSource<Maybe<string>>();

            var dialog = new OpenFileDialog
            {
                Multiselect = false
            };

            if (!string.IsNullOrEmpty(fileName))
            {
                dialog.FileName = fileName;
            }

            if (!string.IsNullOrEmpty(filter))
            {
                dialog.Filter = filter;
            }

            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            completionSource.SetResult(dialog.ShowDialog() == true ? dialog.FileName : Maybe<string>.None);

            return completionSource.Task;
        }

        public Task<Maybe<string>> ShowFolderPickerDialog(string title = "", string initialDirectory = "")
        {
            var completionSource = new TaskCompletionSource<Maybe<string>>();

            var dialog = new CommonOpenFileDialog
            {
                Multiselect = false,
                IsFolderPicker = true
            };

            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            completionSource.SetResult(
                dialog.ShowDialog() == CommonFileDialogResult.Ok
                ? dialog.FileName
                : Maybe<string>.None);

            return completionSource.Task;
        }

        public Task<Maybe<string>> ShowInputDialog(string message, string caption = "", string predefined = "")
        {
            var completionSource = new TaskCompletionSource<Maybe<string>>();

            if (InputBoxWindow.ShowDialog(message, caption, predefined, out var inputText) == true)
            {
                completionSource.SetResult(inputText);
            }
            else
            {
                completionSource.SetResult(Maybe<string>.None);
            }

            return completionSource.Task;
        }

    }
}