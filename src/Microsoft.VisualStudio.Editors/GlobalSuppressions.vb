﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

' Baselined for the port, we should revisit these, see: https://github.com/dotnet/roslyn/issues/8183.
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.Common.DTEUtils.ApplyListViewThemeStyles(System.IntPtr)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.Common.DTEUtils.ApplyTreeViewThemeStyles(System.IntPtr)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.Common.Utils.FocusFirstOrLastTabItem(System.IntPtr,System.Boolean)~System.Boolean")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.DesignerFramework.DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase.OnUndoing(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.DesignerFramework.DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase.OnUndone(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.DesignerFramework.DesignerToolbarPanel.WndProc(System.Windows.Forms.Message@)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.DelayPostingMessage(System.Object)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.ImportList_ItemCheck(System.Object,System.Windows.Forms.ItemCheckEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.PostRefreshImportListMessage")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.PostRefreshReferenceListMessage")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.PostRefreshServiceReferenceListMessage")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.SetReferenceListColumnWidths(System.Windows.Forms.Control@,System.Windows.Forms.ListView@,System.Int32)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView.HandleViewHelperCommandExec(System.Guid,System.UInt32,System.Boolean@)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView.PropertyGridUpdate")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.BeginLabelEdit(Microsoft.VisualStudio.Editors.ResourceEditor.Resource)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.ClearColumnSortImage(System.Int32)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceListView.SetColumnSortImage(System.Int32,System.Boolean)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.TypeEditorHostControl.DropDownControl(System.Windows.Forms.Control)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0009:Override Object.Equals(object) when implementing IEquatable<T> ", Justification:="<Pending>", Scope:="type", Target:="~T:Microsoft.VisualStudio.Editors.PropertyPages.ImportIdentity")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.Runtime.Hosting.StrongNameHelpers.StrongNameFreeBuffer(System.IntPtr)")>
' BUG: https://github.com/dotnet/roslyn/issues/15343
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.XmlToSchema.InputXmlForm._addAsTextButton_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.XmlToSchema.InputXmlForm._addFromFileButton_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.XmlToSchema.InputXmlForm._addFromWebButton_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.XmlToSchema.InputXmlForm._listViewKeyPress(System.Object,System.Windows.Forms.KeyEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.XmlToSchema.InputXmlForm._okButtonClick(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyApplication.MyApplicationProperties._myAppDocData")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityProjectService._projectSettings")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.ApplicationPropPageVBWinForms._myApplicationPropertiesNotifyPropertyChanged")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage._projectService")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage._projectItemEvents")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.WPF.ApplicationPropPageVBWPF._applicationXamlDocData")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.WPF.ApplicationPropPageVBWPF._pageErrorControl")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.FileWatcher.DirectoryWatcher._fileSystemWatcher")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.FileWatcher.FileWatcherEntry._timer")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorRootDesigner._buildEvents")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorRootDesigner._designerHost")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorRootDesigner._undoEngine")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryAudio")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryFiles")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryIcons")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryImages")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryOther")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._categoryStrings")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceEditorView._resourceListView")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.ResourceFile._componentChangeService")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerLoader._buildEvents")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypeEditorHostControl._previewPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypeEditorHostControl._showEditorButton")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypeEditorHostControl._valueComboBox")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypeEditorHostControl._valueTextBox")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.DesignerFramework.BaseDesignerLoader.m_WindowEvents_WindowActivated(EnvDTE.Window,EnvDTE.Window)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.buttonOK_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.listViewExtensions_ColumnClick(System.Object,System.Windows.Forms.ColumnClickEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.listViewExtensions_DoubleClick(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.listViewExtensions_SelectedIndexChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.buttonYes_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityProjectService.m_ProjectSettings_ExtensionChanged")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ApplicationPropPage.iconTableLayoutPanel_Paint(System.Object,System.Windows.Forms.PaintEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.BuildPropPage.chkDefineDebug_CheckedChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.BuildPropPage.chkDefineTrace_CheckedChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.BuildPropPage.rbStartAction_CheckedChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.DebugPropPage.rbStartAction_CheckedChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.buttonAdd_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.buttonRemove_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.linklabelHelp_LinkClicked(System.Object,System.Windows.Forms.LinkLabelLinkClickedEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.listViewExtensions_AddExtension(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.listViewExtensions_ColumnClick(System.Object,System.Windows.Forms.ColumnClickEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.listViewExtensions_RemoveExtension(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.listViewExtensions_SelectedIndexChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.m_ProjectService_ExtensionChanged")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.addContextMenuStrip_Opening(System.Object,System.ComponentModel.CancelEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.referenceToolStripMenuItem_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.serviceReferenceToolStripMenuItem_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.webReferenceToolStripMenuItem_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.UnusedReferencePropPage.dialog_Close(System.Object,System.Windows.Forms.FormClosedEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.UnusedReferencePropPage.dialog_Shown(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CellBeginEdit(System.Object,System.Windows.Forms.DataGridViewCellCancelEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CellFormatting(System.Object,System.Windows.Forms.DataGridViewCellFormattingEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CellStateChanged(System.Object,System.Windows.Forms.DataGridViewCellStateChangedEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CellValidated(System.Object,System.Windows.Forms.DataGridViewCellEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CellValidating(System.Object,System.Windows.Forms.DataGridViewCellValidatingEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_CurrentCellDirtyStateChanged(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_DefaultValuesNeeded(System.Object,System.Windows.Forms.DataGridViewRowEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_OnCellClickBeginEdit(System.Object,System.ComponentModel.CancelEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_OnDataError(System.Object,System.Windows.Forms.DataGridViewDataErrorEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_OnEditingControlShowing(System.Object,System.Windows.Forms.DataGridViewEditingControlShowingEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_RowStateChanged(System.Object,System.Windows.Forms.DataGridViewRowStateChangedEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_SortCompare(System.Object,System.Windows.Forms.DataGridViewSortCompareEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_UserAddedRow(System.Object,System.Windows.Forms.DataGridViewRowEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_UserDeletedRow(System.Object,System.Windows.Forms.DataGridViewRowEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView_UserDeletingRow(System.Object,System.Windows.Forms.DataGridViewRowCancelEventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.SettingsDesigner.TypePickerDialog.m_OkButton_Click(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.DesignerFramework.BaseDesignerLoader.m_DocData")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.DesignerFramework.BaseDesignerLoader.m_WindowEvents")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.buttonCancel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.buttonOK")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.colHeaderExensionDescription")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.colHeaderExtensionName")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.colHeaderExtensionVersion")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.listViewExtensions")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.tableLayoutOKCancelButtons")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AddMyExtensionsDialog.tableLayoutOverarching")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.buttonNo")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.buttonYes")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.checkBoxOption")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.labelQuestion")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.listBoxItems")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.tableLayoutOverarching")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.MyExtensibility.AssemblyOptionDialog.tableLayoutYesNoButtons")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.buttonAdd")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.buttonRemove")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.colHeaderExtensionDescription")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.colHeaderExtensionName")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.colHeaderExtensionVersion")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.labelDescription")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.linkLabelHelp")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.listViewExtensions")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.tableLayoutAddRemoveButtons")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.MyExtensibilityPropPage.tableLayoutOverarching")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.addRemoveButtonsTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.addUserImportTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.ReferencePropPage.referenceButtonsTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.PropertyPages.UnusedReferencePropPage.m_HostDialog")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.DialogQueryName.addCancelTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.DialogQueryName.overarchingTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.alwaysCheckCheckBox")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.buttonCancel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.buttonOK")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.dialogLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.messageLabel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.ResourceEditor.OpenFileWarningDialog.messageLabel2")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsDesignerView.m_SettingsGridView")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypePickerDialog.m_CancelButton")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypePickerDialog.m_OkButton")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypePickerDialog.okCancelTableLayoutPanel")>
<Assembly: CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification:="<Pending>", Scope:="member", Target:="~P:Microsoft.VisualStudio.Editors.SettingsDesigner.TypePickerDialog.overarchingTableLayoutPanel")>
