﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    // <summary>
    // ProjectReloadManager
    //
    // VS wide component which manages project reloads on behalf of projects. As projects are loaded they register with this component
    // which will add that project to its list of watched projects. When the project file changes outside of VS, this object will call into the project to
    // perform a silent reload. Using a cemtral objects allows batching of reload messages, especially when silent reloads fail due to problems with
    // the project file, or the file in memory is dirty amd requires user intervention.
    // </summary>
    [Export(typeof(IProjectReloadManager))]
    [AppliesTo("HandlesOwnReload")]
    internal class ProjectReloadManager : OnceInitializedOnceDisposedAsync, IProjectReloadManager, IVsFileChangeEvents, IVsSolutionEvents, IVsSolutionEvents4
    {
        private readonly IServiceProvider _serviceProvider;
        public IProjectThreadingService _threadHandling;

        private uint  _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
        private ITaskDelayScheduler _reloadDelayScheduler;
        private const int ReloadDelay = 1000;   // delay 1s before applying updated project contents.

        // Tracks the set of reloadable projects. The value is the file system watcher cookie
        private Dictionary<IReloadableProject, uint> _registeredProjects = new Dictionary<IReloadableProject, uint>();

        // Tracks the list of projects that have changed and need to be processed
        private List<IReloadableProject> _changedProjects = new List<IReloadableProject>();

        [ImportingConstructor]
        public ProjectReloadManager(IProjectThreadingService threadHandling, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadHandling.JoinableTaskContext)
        {
            _threadHandling = threadHandling;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Initialization occurs when the first project registers
        /// </summary>
        private Task Initialize()
        {
            return InitializeCoreAsync(CancellationToken.None);
        }

        /// <summary>
        /// Called by reloadable projects to register themselves
        /// </summary>
        public async Task RegisterProjectAsync(IReloadableProject project)
        {
            await Initialize().ConfigureAwait(false);
            await _threadHandling.SwitchToUIThread();

            RegisterProject(project);
        }

        public void RegisterProject(IReloadableProject project)
        {
            uint filechangeCookie;
            _registeredProjects.TryGetValue(project, out filechangeCookie);
            System.Diagnostics.Debug.Assert(filechangeCookie == VSConstants.VSCOOKIE_NIL);
            if (filechangeCookie == VSConstants.VSCOOKIE_NIL)
            {
                IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
                if (fileChangeService != null)
                {
                    int hr = fileChangeService.AdviseFileChange(project.ProjectFile, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size), this, out filechangeCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr) && filechangeCookie != VSConstants.VSCOOKIE_NIL);
                    _registeredProjects.Add(project, filechangeCookie);
                }
            }
        }

        /// <summary>
        /// Called by reloadable projects upon close go unregister themselves
        /// Removes the file change watch on the project file. 
        /// </summary>
        /// <returns></returns>
        public async Task UnregisterProjectAsync(IReloadableProject project)
        {
            await _threadHandling.SwitchToUIThread();
            
            UnregisterProject(project);
        }

        private void UnregisterProject(IReloadableProject project)
        {
            uint filechangeCookie;
            if (_registeredProjects.TryGetValue(project, out filechangeCookie))
            {
                // Remove watch
                IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
                if (fileChangeService != null)
                {
                    int hr = fileChangeService.UnadviseFileChange(filechangeCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr));
                }

                // Always remove the watcher from our list
                _registeredProjects.Remove(project);
            }
        }

        /// <summary>
        /// Adds a file change watcher on the project file.
        /// </summary>
        private async Task ConnectToSolutionEvents()
        {
            await _threadHandling.SwitchToUIThread();

            if (_solutionEventsCookie == VSConstants.VSCOOKIE_NIL)
            {
                IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    int hr = solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr) && _solutionEventsCookie != VSConstants.VSCOOKIE_NIL);
                }
            }
        }

        /// <summary>
        /// Removes the file change watch on the project file. 
        /// </summary>
        private async Task DisconnectFromSolutionEvents()
        {
            if(_solutionEventsCookie != VSConstants.VSCOOKIE_NIL)
            {
                await _threadHandling.SwitchToUIThread();

                IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    int hr = solution.UnadviseSolutionEvents(_solutionEventsCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr));
                    _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        /// <summary>
        /// Handles one time initialization
        /// </summary>
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _reloadDelayScheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(ReloadDelay), _threadHandling, CancellationToken.None);
            await ConnectToSolutionEvents().ConfigureAwait(false);

        }

        /// <summary>
        /// IDispoable handler. Should only be called once
        /// </summary>
        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if(_reloadDelayScheduler != null)
            {
                _reloadDelayScheduler.Dispose();
                _reloadDelayScheduler = null;
            }
            await DisconnectFromSolutionEvents().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the project file changes. In our case since we only watch one file with each cookie, the list of files
        /// should be one.
        /// </summary>
        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] grfChange)
        {
            if(cChanges == 1 && (grfChange[0] & (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time)) != 0)
            {
                lock(_changedProjects)
                {
                    var changedProject = _registeredProjects.FirstOrDefault(kv => kv.Key.ProjectFile.Equals(rgpszFile[0], StringComparison.OrdinalIgnoreCase)).Key;
                    if(changedProject != null)
                    {
                        if(!_changedProjects.Contains(changedProject))
                        {
                            _changedProjects.Add(changedProject);
                        }
                        _reloadDelayScheduler.ScheduleAsyncTask(async (ct) => 
                        {
                            // Grab the UI thread so that we block until the reload of this set of 
                            // projects completes.
                            await _threadHandling.SwitchToUIThread();

                            // Make a copy of the changed projects
                            var changedProjects = new List<IReloadableProject>();
                            lock(_changedProjects)
                            {
                                changedProjects.AddRange(_changedProjects);
                                _changedProjects.Clear();
                            }
                               
                            var failedProjects = new List<Tuple<IReloadableProject, ProjectReloadResult>>();
                            _threadHandling.ExecuteSynchronously(async () =>
                            {
                                foreach(var project in changedProjects)
                                {
                                    ProjectReloadResult result =  await project.ReloadProjectAsync().ConfigureAwait(true);

                                    if(result == ProjectReloadResult.ReloadFailed || result == ProjectReloadResult.ReloadFailedProjectDirty)
                                    {
                                        failedProjects.Add(new Tuple<IReloadableProject, ProjectReloadResult>(project, result));
                                    }
                                }
                            });

                            ProcessProjectReloadFailures(failedProjects);

                        });
                    }
                }
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Prompts once for all the projects 
        /// For each dirty project it prompts first to see what the user action meeds to be amd then
        /// reloads the project if desired. Other optioms are do nothing amd save as and then reload
        /// </summary>
        /// <param name="failedProjects"></param>
        private void ProcessProjectReloadFailures(List<Tuple<IReloadableProject, ProjectReloadResult>> failedProjects)
        {
            if(failedProjects.Count == 0)
            {
                return;
            }


            // Process each project. if the project is dirty in memory we need to prompt for each action. Non-dirty ones are
            // simply reloaded
            bool ignoreAll = false;
            bool reloadAll = false;
            foreach(var failure in failedProjects)
            {
                if(failure.Item2 == ProjectReloadResult.ReloadFailedProjectDirty)
                {
                    var buttons = new string[] {
                                                Resources.Ignore,
                                                Resources.Overwrite,
                                                Resources.Discard,
                                                Resources.SaveAs};
                    var msgText = string.Format(Resources.ConflictingModificationsPrompt, Path.GetFileNameWithoutExtension(failure.Item1.ProjectFile));
                    var dlg = new MultiChoiceMsgBox(Resources.ConflictingProjectModificationTitle, msgText, buttons);
                    var dlgResult = dlg.ShowModal();
                    if(dlgResult == true)
                    {
                        switch (dlg.SelectedAction)
                        {
                            case MultiChoiceMsgBoxResult.Cancel:
                                break;
                            case MultiChoiceMsgBoxResult.Button1:
                                break;
                            case MultiChoiceMsgBoxResult.Button2:
                                SaveProject(failure.Item1);
                                break;
                            case MultiChoiceMsgBoxResult.Button3:
                                ReloadProjectInSolution(failure.Item1);
                                break;
                            case MultiChoiceMsgBoxResult.Button4:
                                if(ErrorHandler.Succeeded(SaveAsProject(failure.Item1)))
                                {
                                    ReloadProjectInSolution(failure.Item1);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    if(ignoreAll)
                    {   
                        // do nothing
                    }
                    else if(reloadAll)
                    {
                        ReloadProjectInSolution(failure.Item1);
                    }
                    else
                    {

                        var buttons = new string[] {
                                                    Resources.IgnoreAll,
                                                    Resources.Ignore,
                                                    Resources.ReloadAll,
                                                    Resources.Reload};
                        var msgText = string.Format(Resources.ProjectModificationsPrompt, Path.GetFileNameWithoutExtension(failure.Item1.ProjectFile));
                        var dlg = new MultiChoiceMsgBox(Resources.ProjectModificationDlgTitle, msgText, buttons);
                        var dlgResult = dlg.ShowModal();
                        if(dlgResult == true)
                        {
                            switch (dlg.SelectedAction)
                            {
                                case MultiChoiceMsgBoxResult.Cancel:
                                    break;
                                case MultiChoiceMsgBoxResult.Button1:
                                    ignoreAll = true;
                                    break;
                                case MultiChoiceMsgBoxResult.Button2:
                                    break;
                                case MultiChoiceMsgBoxResult.Button3:
                                    reloadAll = true;
                                    ReloadProjectInSolution(failure.Item1);
                                    break;
                                case MultiChoiceMsgBoxResult.Button4:
                                    ReloadProjectInSolution(failure.Item1);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper to use the solution to reload the project.
        /// Reloading is managed via the ReloadItem() method of our parent hierarhcy(solution 
        /// or solution folder).  So first we get our parent hierarchy and our itemid in the parent
        /// hierarchy. 
        /// </summary>
        void ReloadProjectInSolution(IReloadableProject project)
        {
            // Get our parent hierarchy and our itemid in the parent hierarchy.
            IVsHierarchy parentHier = project.VsHierarchy.GetProperty<IVsHierarchy>(VsHierarchyPropID.ParentHierarchy, null);
            if(parentHier == null)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }
            uint parentItemid  = (uint)project.VsHierarchy.GetProperty<int>(VsHierarchyPropID.ParentHierarchyItemid, unchecked((int)VSConstants.VSITEMID_NIL));
            if(parentItemid == VSConstants.VSITEMID_NIL)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }
            
            // Now using IVsPersistHierarchyItem2 we reload the project.
            int hr = ((IVsPersistHierarchyItem2)parentHier).ReloadItem((uint)parentItemid, dwReserved: 0);
            ErrorHandler.ThrowOnFailure(hr);
        }

        int SaveProject(IReloadableProject project)
        {
            IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            __VSSLNSAVEOPTIONS saveOpts = __VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty;
            return solution.SaveSolutionElement((uint)saveOpts, project.VsHierarchy, VSConstants.VSITEMID_ROOT);
        }

        int SaveAsProject(IReloadableProject project)
        {
            // Save as needs to go through IPersistFileFormat
            var persistFileFmt = project.VsHierarchy as IPersistFileFormat;
            var uishell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();
            string newFile;
            int canceled;
            int hr = uishell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SaveCopyAs, persistFileFmt, project.ProjectFile, out newFile, out canceled);

            if (ErrorHandler.Succeeded(hr) && canceled == 1)
            {
                hr = VSConstants.E_ABORT;
            }
            return hr;
        }

        /// <summary>
        /// Callback for directory changes. Since we don't watch the folder there is nothing to do here
        /// </summary>
        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// IVsSolutionEvents4. We only care about OnAfterRenameProject. If our project file is renamed we need to
        /// stop watching the old file and start watching the new file
        /// </summary>
        public int OnAfterRenameProject(IVsHierarchy pHierarchy)
        {
            lock(_registeredProjects)
            {
                var renamedProject = _registeredProjects.FirstOrDefault(kv => kv.Key.VsHierarchy.Equals(pHierarchy)).Key;
                if(renamedProject != null)
                {
                    UnregisterProject(renamedProject);
                    RegisterProject(renamedProject);

                }
            }
            return VSConstants.S_OK;
        }

        public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// IVsSolutionEvents memebers. Needed to implement this 
        /// </summary>
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

    }
}
