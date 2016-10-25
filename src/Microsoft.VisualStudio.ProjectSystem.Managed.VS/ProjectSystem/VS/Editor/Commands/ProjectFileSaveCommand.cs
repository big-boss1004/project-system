﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Editor;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Commands
{
    [Export(typeof(IProjectFileEditorCommandAsync))]
    internal class ProjectFileSaveCommand : IProjectFileEditorCommandAsync
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProjectLockService _projectLockService;
        private readonly IProjectThreadingService _projectThreadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsEditorAdaptersFactoryService _editorFactoryService;

        [ImportingConstructor]
        public ProjectFileSaveCommand(IFileSystem fileSystem,
            IProjectLockService projectLockService,
            IProjectThreadingService projectThreadingService,
            UnconfiguredProject unconfiguredProject,
            IVsEditorAdaptersFactoryService editorFactoryService)
        {
            _fileSystem = fileSystem;
            _projectLockService = projectLockService;
            _projectThreadingService = projectThreadingService;
            _unconfiguredProject = unconfiguredProject;
            _editorFactoryService = editorFactoryService;
        }

        public long CommandId { get; } = VisualStudioStandard97CommandId.SaveProjectItem;

        public async Task<int> Handle(IVsProject project)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                // Ensure the project file is checked out for changes
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);

                // We need read the project file from the UI thread, but after it's been read we need to come
                // back to this thread to ensure we respect the lock.
                var projectText = await ReadProjectFile(project).ConfigureAwait(true);

                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, projectText);
            }

            return VSConstants.S_OK;
        }

        private async Task<string> ReadProjectFile(IVsProject project)
        {
            await _projectThreadingService.SwitchToUIThread();
            IVsTextLines buffer;
            Verify.HResult(((IVsTextBufferProvider)project).GetTextBuffer(out buffer));
            ITextBuffer textBuffer = _editorFactoryService.GetDocumentBuffer(buffer);
            return textBuffer.CurrentSnapshot.GetText();
        }
    }
}
