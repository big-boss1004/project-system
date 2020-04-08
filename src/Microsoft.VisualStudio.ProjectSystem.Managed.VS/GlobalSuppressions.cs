﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/1432", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.EditAndContinue.EditAndContinueProvider.Dispose")]
[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/1432", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.BuildMacroInfo.Dispose")]
[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/1432", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.References.DesignTimeAssemblyResolution.Dispose")]
[assembly: SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "https://github.com/dotnet/project-system/issues/3921", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.PropertyPage.SetObjects(System.UInt32,System.Object[])")]
[assembly: SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/1654", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.TempPE.TempPEBuildManager.DisposeCoreAsync(System.Boolean)~System.Threading.Tasks.Task")]

[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.NuGet.ProjectAssetFileWatcher.ProjectAssetFileWatcherInstance.GetFileHashOrNull(System.String)~System.Byte[]")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions.InputNodeGraphActionHandlerBase.TryHandleRequest(Microsoft.VisualStudio.GraphModel.IGraphContext)~System.Boolean")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions.SearchGraphActionHandler.Search(Microsoft.VisualStudio.GraphModel.IGraphContext)")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~M:Microsoft.VisualStudio.Telemetry.VsTelemetryService.HashValue(System.String)~System.String")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~P:Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.DebugPageViewModel.BrowseDirectoryCommand")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~P:Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.DebugPageViewModel.BrowseExecutableCommand")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/2416", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.ProjectAssetFileWatcher.ProjectAssetFileWatcherInstance.GetFileHashOrNull(System.String)~System.Byte[]")]

[assembly: SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.PackageRestoreService.DisposeCoreAsync(System.Boolean)~System.Threading.Tasks.Task")]

[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "https://github.com/dotnet/roslyn/issues/41531", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.Automation.VSProject.get_Extender(System.String)~System.Object")]

[assembly: SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/3295", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.TempPE.DesignTimeInputsChangeTracker.Initialize")]
[assembly: SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/3295", Scope = "member", Target = "~M:Microsoft.VisualStudio.ProjectSystem.VS.TempPE.DesignTimeInputsFileWatcher.Initialize")]
