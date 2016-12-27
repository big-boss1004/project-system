﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners
{
    [Export(typeof(ITextBufferStateListener))]
    internal class TempFileBufferStateStateListener : OnceInitializedOnceDisposedAsync, ITextBufferStateListener
    {
        private readonly IEditorStateModel _editorState;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersService;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsShellUtilitiesHelper _shellUtilities;
        private readonly IServiceProvider _serviceProvider;

        private string _tempFilePath;
        private ITextDocument _textDoc;

        [ImportingConstructor]
        public TempFileBufferStateStateListener(
            IEditorStateModel editorState,
            IVsEditorAdaptersFactoryService editorAdaptersService,
            ITextDocumentFactoryService textDocumentFactoryService,
            IProjectThreadingService threadingService,
            IVsShellUtilitiesHelper shellUtilities,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) :
            base(threadingService.JoinableTaskContext)
        {
            _editorState = editorState;
            _editorAdaptersService = editorAdaptersService;
            _textDocumentFactoryService = textDocumentFactoryService;
            _threadingService = threadingService;
            _shellUtilities = shellUtilities;
            _serviceProvider = serviceProvider;
        }

        public Task InitializeListenerAsync(string filePath)
        {
            _tempFilePath = filePath;
            return InitializeAsync();
        }

        private void TextDocument_FileActionOccurred(object sender, TextDocumentFileActionEventArgs args)
        {
            if (args.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                _threadingService.ExecuteSynchronously(_editorState.SaveProjectFileAsync);
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized && _textDoc != null)
            {
                await _threadingService.SwitchToUIThread();
                _textDoc.FileActionOccurred -= TextDocument_FileActionOccurred;
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();

            _shellUtilities.GetRDTDocumentInfo(_serviceProvider, _tempFilePath, out IVsHierarchy unusedHier, out uint unusedId, out IVsPersistDocData docData, out uint unusedCookie);

            var textBuffer = _editorAdaptersService.GetDocumentBuffer((IVsTextBuffer)docData);
            Assumes.True(_textDocumentFactoryService.TryGetTextDocument(textBuffer, out _textDoc));
            Assumes.NotNull(_textDoc);
            _textDoc.FileActionOccurred += TextDocument_FileActionOccurred;
        }
    }
}
