' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'This is the C#/J# version of the Compile property page.  'CompilePropPage2.vb is the VB version.

Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSLangProj80
Imports VSLangProj110
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class BuildPropPage
        Inherits BuildPropPageBase

        Protected m_stDocumentationFile() As String
        'True when we're changing control values ourselves
        Protected m_bInsideInternalUpdate As Boolean = False

        '// Stored conditional compilation symbols. We need these to calculate the new strings
        '//   to return for the conditional compilation constants when the user changes any
        '//   of the controls related to conditional compilation symbols (the data in the
        '//   controls is not sufficient because they could be indeterminate, and we are acting
        '//   as if we have three separate properties, so we need the original property values).
        '// Array same length and indexing as the objects passed in to SetObjects.
        Protected m_stCondCompSymbols() As String
        Protected Const Const_DebugConfiguration As String = "Debug" 'Name of the debug configuration
        Protected Const Const_ReleaseConfiguration As String = "Release" 'Name of the release configuration
        Protected Const Const_CondConstantDEBUG As String = "DEBUG"
        Protected Const Const_CondConstantTRACE As String = "TRACE"

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Scale horizontally
            Me.cboPlatformTarget.Width = DpiHelper.LogicalToDeviceUnitsX(Me.cboPlatformTarget.Width)
            Me.overarchingTableLayoutPanel.Width = DpiHelper.LogicalToDeviceUnitsX(Me.overarchingTableLayoutPanel.Width)

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub


        Public Enum TreatWarningsSetting
            WARNINGS_ALL
            WARNINGS_SPECIFIC
            WARNINGS_NONE
        End Enum

        Protected Overrides Sub EnableAllControls(ByVal _enabled As Boolean)
            MyBase.EnableAllControls(_enabled)
        End Sub

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants, "DefineConstants", Me.txtConditionalCompilationSymbols, AddressOf ConditionalCompilationSet, AddressOf ConditionalCompilationGet, ControlDataFlags.None, New Control() {Me.txtConditionalCompilationSymbols, Me.chkDefineDebug, Me.chkDefineTrace, Me.lblConditionalCompilationSymbols}),
                     New PropertyControlData(VsProjPropId80.VBPROJPROPID_PlatformTarget, "PlatformTarget", Me.cboPlatformTarget, AddressOf PlatformTargetSet, AddressOf PlatformTargetGet, ControlDataFlags.None, New Control() {Me.lblPlatformTarget}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_AllowUnsafeBlocks, "AllowUnsafeBlocks", Me.chkAllowUnsafeCode),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_Optimize, "Optimize", Me.chkOptimizeCode),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_WarningLevel, "WarningLevel", Me.cboWarningLevel, AddressOf WarningLevelSet, AddressOf WarningLevelGet, ControlDataFlags.None, New Control() {lblWarningLevel}),
                     New PropertyControlData(VsProjPropId2.VBPROJPROPID_NoWarn, "NoWarn", Me.txtSupressWarnings, New Control() {Me.lblSupressWarnings}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors, "TreatWarningsAsErrors", Me.rbWarningAll, AddressOf TreatWarningsInit, AddressOf TreatWarningsGet),
                     New PropertyControlData(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, "TreatSpecificWarningsAsErrors", Me.txtSpecificWarnings, AddressOf TreatSpecificWarningsInit, AddressOf TreatSpecificWarningsGet),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_OutputPath, "OutputPath", Me.txtOutputPath, New Control() {Me.lblOutputPath}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile, "DocumentationFile", Me.txtXMLDocumentationFile, AddressOf Me.XMLDocumentationFileInit, AddressOf Me.XMLDocumentationFileGet, ControlDataFlags.None, New Control() {Me.txtXMLDocumentationFile, Me.chkXMLDocumentationFile}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop, "RegisterForComInterop", Me.chkRegisterForCOM, AddressOf Me.RegisterForCOMInteropSet, AddressOf Me.RegisterForCOMInteropGet),
                     New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, "OutputTypeEx", Nothing, AddressOf Me.OutputTypeSet, Nothing),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_GenerateSerializationAssemblies, "GenerateSerializationAssemblies", Me.cboSGenOption, New Control() {Me.lblSGenOption}),
                     New PropertyControlData(VsProjPropId110.VBPROJPROPID_Prefer32Bit, "Prefer32Bit", Me.chkPrefer32Bit, AddressOf Prefer32BitSet, AddressOf Prefer32BitGet)
                     }
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again.
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            MyBase.PreInitPage()

            cboPlatformTarget.Items.Clear()

            Dim PlatformEntries As New List(Of String)

            ' Let's try to sniff the supported platforms from our hiearchy (if any)
            If Me.ProjectHierarchy IsNot Nothing Then
                Dim oCfgProv As Object = Nothing
                Dim hr As Integer
                hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, oCfgProv)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim cfgProv As IVsCfgProvider2 = TryCast(oCfgProv, IVsCfgProvider2)
                    If cfgProv IsNot Nothing Then
                        Dim actualPlatformCount(0) As UInteger
                        hr = cfgProv.GetSupportedPlatformNames(0, Nothing, actualPlatformCount)
                        If VSErrorHandler.Succeeded(hr) Then
                            Dim platformCount As UInteger = actualPlatformCount(0)
                            Dim platforms(CInt(platformCount)) As String
                            hr = cfgProv.GetSupportedPlatformNames(platformCount, platforms, actualPlatformCount)
                            If VSErrorHandler.Succeeded(hr) Then
                                For platformNo As Integer = 0 To CInt(platformCount - 1)
                                    PlatformEntries.Add(platforms(platformNo))
                                Next
                            End If
                        End If
                    End If
                End If
            End If

            ' ...and if we couldn't get 'em from the project system, let's add a hard-coded list of platforms...
            If PlatformEntries.Count = 0 Then
                Debug.Fail("Unable to get platform list from configuration manager")
                PlatformEntries.AddRange(New String() {"Any CPU", "x86", "x64", "Itanium"})
            End If
            If VSProductSKU.ProductSKU < VSProductSKU.VSASKUEdition.Enterprise Then
                'For everything lower than VSTS (SKU# = Enterprise), don't target Itanium
                PlatformEntries.Remove("Itanium")
            End If

            ' ... Finally, add the entries to the combobox
            Me.cboPlatformTarget.Items.AddRange(PlatformEntries.ToArray())
        End Sub

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again.
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()

            'OutputPath browse button should only be enabled when the text box is enabled and Not ReadOnly
            Me.btnOutputPathBrowse.Enabled = (Me.txtOutputPath.Enabled AndAlso Not Me.txtOutputPath.ReadOnly)

            Me.rbWarningNone.Enabled = Me.rbWarningAll.Enabled
            Me.rbWarningSpecific.Enabled = Me.rbWarningAll.Enabled

            RefreshEnabledStatusForPrefer32Bit(Me.chkPrefer32Bit)

        End Sub

        Private Sub AdvancedButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAdvanced.Click
            ShowChildPage(SR.GetString(SR.PPG_AdvancedBuildSettings_Title), GetType(AdvBuildSettingsPropPage), HelpKeywords.CSProjPropAdvancedCompile)
        End Sub

        Private Function ShouldEnableRegisterForCOM() As Boolean

            Dim obj As Object = Nothing
            Dim outputType As UInteger

            Try
                If GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, obj) Then
                    outputType = CUInt(obj)
                Else
                    Return True
                End If
            Catch exc As InvalidCastException
                Return True
            Catch exc As NullReferenceException
                Return True
            Catch ex As TargetInvocationException
                Return True
            End Try

            ' Only supported for libraries
            Return outputType = VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Library

        End Function

        Private Function OutputTypeSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If Not ShouldEnableRegisterForCOM() Then
                Me.chkRegisterForCOM.Enabled = False
            Else
                EnableControl(Me.chkRegisterForCOM, True)
            End If

            If Not m_fInsideInit AndAlso Not m_bInsideInternalUpdate Then
                ' Changes to the OutputType may affect whether Prefer32Bit is enabled
                RefreshEnabledStatusForPrefer32Bit(Me.chkPrefer32Bit)
            End If

            Return True
        End Function

        Private Function RegisterForCOMInteropSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                Dim bRegisterForCOM As Boolean = False
                Dim propRegisterForCOM As PropertyDescriptor
                Dim obj As Object

                propRegisterForCOM = GetPropertyDescriptor("RegisterForComInterop")
                obj = TryGetNonCommonPropertyValue(propRegisterForCOM)

                If Not (obj Is PropertyControlData.MissingProperty) Then
                    If Not (obj Is PropertyControlData.Indeterminate) Then
                        bRegisterForCOM = CType(obj, Boolean)
                    End If

                    Me.chkRegisterForCOM.Checked = bRegisterForCOM

                    '// Checkbox is only enabled for DLL projects
                    If Not ShouldEnableRegisterForCOM() Then
                        Me.chkRegisterForCOM.Enabled = False
                    Else
                        EnableControl(Me.chkRegisterForCOM, True)
                    End If

                    Return True
                Else
                    Me.chkRegisterForCOM.Enabled = False
                    Me.chkRegisterForCOM.CheckState = CheckState.Indeterminate
                    Return True
                End If
            Else
                Me.chkRegisterForCOM.CheckState = CheckState.Indeterminate
                Return True
            End If
        End Function

        Private Function RegisterForCOMInteropGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.chkRegisterForCOM.CheckState <> CheckState.Indeterminate) Then
                value = Me.chkRegisterForCOM.Checked
                Return True
            Else
                Return False   '// Let the framework handle it since its indeterminate
            End If
        End Function

        Private Sub OutputPathBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnOutputPathBrowse.Click
            Dim DirName As String = Nothing
            If GetDirectoryViaBrowseRelativeToProject(Me.txtOutputPath.Text, SR.GetString(SR.PPG_SelectOutputPathTitle), DirName) Then
                txtOutputPath.Text = DirName
                SetDirty(True) ' vswhidbey 276000 - textchanged events do not commit, lostfocus does
                ' this code path should commit the change if the user selected a new outputpath via the picker
            Else
                'User cancelled out of dialog
            End If
        End Sub

        Private Function TreatSpecificWarningsInit(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            m_bInsideInternalUpdate = True

            Try
                Dim bIndeterminateState As Boolean = False
                Dim warnings As TreatWarningsSetting

                If (Not (PropertyControlData.IsSpecialValue(value))) Then
                    Dim stSpecificWarnings As String

                    stSpecificWarnings = CType(value, String)
                    If (stSpecificWarnings <> "") Then
                        warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                        Me.txtSpecificWarnings.Text = stSpecificWarnings

                        bIndeterminateState = False
                    Else
                        Dim propTreatAllWarnings As PropertyDescriptor
                        Dim obj As Object
                        Dim bTreatAllWarningsAsErrors As Boolean = False

                        propTreatAllWarnings = GetPropertyDescriptor("TreatWarningsAsErrors")

                        obj = TryGetNonCommonPropertyValue(propTreatAllWarnings)

                        If Not (PropertyControlData.IsSpecialValue(obj)) Then
                            Me.txtSpecificWarnings.Text = ""
                            bTreatAllWarningsAsErrors = CType(obj, Boolean)
                            If (bTreatAllWarningsAsErrors) Then
                                warnings = TreatWarningsSetting.WARNINGS_ALL
                            Else
                                warnings = TreatWarningsSetting.WARNINGS_NONE
                            End If

                            bIndeterminateState = False
                        Else
                            '// Since TreadAllWarnings is indeterminate we should be too
                            bIndeterminateState = True
                        End If
                    End If
                Else
                    '// Indeterminate. Leave all the radio buttons unchecled
                    bIndeterminateState = True
                End If

                If (Not bIndeterminateState) Then
                    Me.rbWarningAll.Checked = (warnings = TreatWarningsSetting.WARNINGS_ALL)
                    Me.rbWarningSpecific.Checked = (warnings = TreatWarningsSetting.WARNINGS_SPECIFIC)
                    Me.txtSpecificWarnings.Enabled = (warnings = TreatWarningsSetting.WARNINGS_SPECIFIC)
                    Me.rbWarningNone.Checked = (warnings = TreatWarningsSetting.WARNINGS_NONE)
                Else
                    Me.rbWarningAll.Checked = False
                    Me.rbWarningSpecific.Checked = False
                    Me.txtSpecificWarnings.Enabled = False
                    Me.txtSpecificWarnings.Text = ""
                    Me.rbWarningNone.Checked = False
                End If
            Finally
                m_bInsideInternalUpdate = False
            End Try

            Return True
        End Function

        Private Function TreatSpecificWarningsGetValue() As TreatWarningsSetting
            Dim warnings As TreatWarningsSetting

            If Me.rbWarningAll.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_ALL
            ElseIf Me.rbWarningSpecific.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
            ElseIf Me.rbWarningNone.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_NONE
            Else
                warnings = TreatWarningsSetting.WARNINGS_NONE
            End If

            Return warnings
        End Function

        Private Function TreatSpecificWarningsGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim bRetVal As Boolean = True

            If Me.rbWarningAll.Checked Then
                value = ""
                bRetVal = True
            ElseIf Me.rbWarningSpecific.Checked Then
                value = Me.txtSpecificWarnings.Text
                bRetVal = True
            ElseIf Me.rbWarningNone.Checked Then
                value = ""
                bRetVal = True
            Else
                '// We're in the indeterminate state. Let the architecture handle it
                bRetVal = False
            End If

            Return bRetVal
        End Function

        Private Function TreatWarningsInit(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            '// Don't need to do anything here (it's done in TreatSpecificWarningsInit)
            Return True
        End Function

        Private Function TreatWarningsGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim bRetVal As Boolean = True

            If Me.rbWarningAll.Checked Then
                value = Me.rbWarningAll.Checked
                bRetVal = True
            ElseIf Me.rbWarningSpecific.Checked Then
                value = False
                bRetVal = True
            ElseIf Me.rbWarningNone.Checked Then
                value = Not (Me.rbWarningNone.Checked)    '// If none is checked we want value to be false
                bRetVal = True
            Else
                '// We're in the indeterminate state. Let the architecture handle it.
                bRetVal = False
            End If

            Return bRetVal
        End Function

        Private Sub rbStartAction_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles rbWarningAll.CheckedChanged, rbWarningSpecific.CheckedChanged, rbWarningNone.CheckedChanged
            If (Not m_bInsideInternalUpdate) Then
                Dim warnings As TreatWarningsSetting = TreatSpecificWarningsGetValue()
                Me.rbWarningAll.Checked = (warnings = TreatWarningsSetting.WARNINGS_ALL)
                Me.rbWarningSpecific.Checked = (warnings = TreatWarningsSetting.WARNINGS_SPECIFIC)
                Me.txtSpecificWarnings.Enabled = (warnings = TreatWarningsSetting.WARNINGS_SPECIFIC)
                Me.rbWarningNone.Checked = (warnings = TreatWarningsSetting.WARNINGS_NONE)
                IsDirty = True

                '// Dirty both of the properties since either one could have changed
                SetDirty(Me.rbWarningAll)
                SetDirty(Me.txtSpecificWarnings)
            End If
        End Sub

        Private Function XMLDocumentationFileInit(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal values() As Object) As Boolean
            Dim bOriginalState As Boolean = m_bInsideInternalUpdate

            m_bInsideInternalUpdate = True
            ReDim m_stDocumentationFile(values.Length - 1)
            values.CopyTo(m_stDocumentationFile, 0)

            Dim objDocumentationFile As Object
            objDocumentationFile = PropertyControlData.GetValueOrIndeterminateFromArray(m_stDocumentationFile)

            If (Not (PropertyControlData.IsSpecialValue(objDocumentationFile))) Then
                If (Trim(TryCast(objDocumentationFile, String)) <> "") Then
                    Me.txtXMLDocumentationFile.Text = Trim(TryCast(objDocumentationFile, String))
                    Me.chkXMLDocumentationFile.Checked = True
                    Me.txtXMLDocumentationFile.Enabled = True
                Else
                    Me.chkXMLDocumentationFile.Checked = False
                    Me.txtXMLDocumentationFile.Enabled = False
                    Me.txtXMLDocumentationFile.Text = ""
                End If
            Else
                Me.chkXMLDocumentationFile.CheckState = CheckState.Indeterminate
                Me.txtXMLDocumentationFile.Text = ""
                Me.txtXMLDocumentationFile.Enabled = False
            End If

            '// Reset value
            m_bInsideInternalUpdate = bOriginalState
            Return True
        End Function

        Private Function XMLDocumentationFileGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef values() As Object) As Boolean
            Debug.Assert(m_stDocumentationFile IsNot Nothing)
            ReDim values(m_stDocumentationFile.Length - 1)
            m_stDocumentationFile.CopyTo(values, 0)
            Return True
        End Function

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsJSProject() Then
                Return HelpKeywords.JSProjPropBuild
            Else
                Debug.Assert(IsCSProject, "Unknown project type")
                Return HelpKeywords.CSProjPropBuild
            End If
        End Function

        Private Function WarningLevelSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                Me.cboWarningLevel.SelectedIndex = CType(value, Integer)
                Return True
            Else
                '// Indeterminate. Let the architecture handle
                Me.cboWarningLevel.SelectedIndex = -1
                Return True
            End If
        End Function

        Private Function WarningLevelGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = CType(Me.cboWarningLevel.SelectedIndex, Integer)
            Return True
        End Function

        Private Function PlatformTargetSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                If (IsNothing(TryCast(value, String)) OrElse TryCast(value, String) = "") Then
                    Me.cboPlatformTarget.SelectedIndex = 0     '// AnyCPU
                Else
                    Dim strPlatform As String = TryCast(value, String)

                    '// vswhidbey 474635: For Undo, we may get called to set the value
                    '// to AnyCpu (no space but the one we display in the combobox has a space so
                    '// convert to the one with the space for this specific case

                    '// Convert the no-space to one with a space
                    If (String.Compare(strPlatform, "AnyCPU", StringComparison.Ordinal) = 0) Then
                        strPlatform = "Any CPU"
                    End If

                    Me.cboPlatformTarget.SelectedItem = strPlatform

                    If (Me.cboPlatformTarget.SelectedIndex = -1) Then   '// If we can't find a match
                        If (VSProductSKU.IsStandard) Then
                            '// For the standard SKU, we do not include Itanium in the list. However,
                            '// if the property is already set to Itanium (most likely from the project file set from
                            '// a non-Standard SKU then add it to the list so we do not report the wrong
                            '// platform target to the user.

                            Dim stValue As String = TryCast(value, String)
                            If (String.Compare(Trim(stValue), "Itanium", StringComparison.Ordinal) = 0) Then
                                Me.cboPlatformTarget.Items.Add("Itanium")
                                Me.cboPlatformTarget.SelectedItem = stValue
                            Else
                                '// Note that the project system will return "AnyCPU" (no space) but in the UI we want to show the one with a space
                                Me.cboPlatformTarget.SelectedItem = "Any CPU"
                            End If
                        Else
                            '// Note that the project system will return "AnyCPU" (no space) but in the UI we want to show the one with a space
                            Me.cboPlatformTarget.SelectedItem = "Any CPU"
                        End If
                    End If
                End If
                Return True
            Else
                '// Indeterminate - allow the architecture to handle
                Return False
            End If
        End Function

        Private Function PlatformTargetGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            ' SelectedItem may be Nothing if the PlatformTarget property isn't supported
            If Me.cboPlatformTarget.SelectedItem Is Nothing Then
                Return False
            End If

            If (Me.cboPlatformTarget.SelectedItem.ToString() <> "AnyCPU") And (Me.cboPlatformTarget.SelectedItem.ToString() <> "Any CPU") Then
                value = Me.cboPlatformTarget.SelectedItem
            Else
                '// Return to the project system the one without a space
                value = "AnyCPU"
            End If

            Return True
        End Function

        Private Sub XMLDocumentationEnable_CheckStateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkXMLDocumentationFile.CheckStateChanged
            Const XML_FILE_EXTENSION As String = ".xml"

            If Me.chkXMLDocumentationFile.Checked Then

                '// Enable the textbox
                Me.txtXMLDocumentationFile.Enabled = True

                If Trim(Me.txtXMLDocumentationFile.Text) = "" Then
                    '// The textbox is empty so initialize it
                    Dim stOutputPath As String
                    Dim stAssemblyName As String
                    Dim obj As Object = Nothing

                    '// Get OutputPath for all configs. We're going to calcuate the documentation file
                    '// for each config (and the value is dependant on the OutputPath

                    Dim RawDocFiles() As Object = RawPropertiesObjects(GetPropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile))
                    Dim OutputPathData() As Object
                    Dim cLen As Integer = RawDocFiles.Length

                    ReDim OutputPathData(cLen)

                    Dim p As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath)
                    For i As Integer = 0 To cLen - 1
                        OutputPathData(i) = p.GetPropertyValueNative(RawDocFiles(i))
                    Next i

                    GetCurrentProperty(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", obj)
                    stAssemblyName = TryCast(obj, String)

                    GetCurrentProperty(VsProjPropId.VBPROJPROPID_AbsoluteProjectDirectory, "AbsoluteProjectDirectory", obj)
                    Dim stProjectDirectory As String = TryCast(obj, String)
                    If Microsoft.VisualBasic.Right(stProjectDirectory, 1) <> "\" Then
                        stProjectDirectory &= "\"
                    End If

                    If (Not IsNothing(m_stDocumentationFile)) Then
                        '// Loop through each config and calculate what we think the output path should be
                        Dim i As Integer

                        For i = 0 To m_stDocumentationFile.Length - 1

                            If (Not IsNothing(OutputPathData)) Then
                                stOutputPath = TryCast(OutputPathData(i), String)
                            Else
                                GetProperty(VsProjPropId.VBPROJPROPID_OutputPath, obj)
                                stOutputPath = CType(obj, String)
                            End If

                            If (Not IsNothing(stOutputPath)) Then
                                If Microsoft.VisualBasic.Right(stOutputPath, 1) <> "\" Then
                                    stOutputPath &= "\"
                                End If

                                If (Path.IsPathRooted(stOutputPath)) Then
                                    '// stOutputPath is an Absolute path so check to see if its within the project path

                                    If (String.Compare(Path.GetFullPath(stProjectDirectory),
                                                       Microsoft.VisualBasic.Left(Path.GetFullPath(stOutputPath), Len(stProjectDirectory)),
                                                       StringComparison.Ordinal) = 0) Then

                                        '// The output path is within the project so suggest the output directory (or suggest just the filename
                                        '// which will put it in the default location

                                        m_stDocumentationFile(i) = stOutputPath & stAssemblyName & XML_FILE_EXTENSION

                                    Else

                                        '// The output path is outside the project so just suggest the project directory.
                                        m_stDocumentationFile(i) = stProjectDirectory & stAssemblyName & XML_FILE_EXTENSION

                                    End If

                                Else
                                    '// OutputPath is a Relative path so it will be based on the project directory. use
                                    '// the OutputPath to suggest a location for the documentation file
                                    m_stDocumentationFile(i) = stOutputPath & stAssemblyName & XML_FILE_EXTENSION
                                End If

                            End If
                        Next

                        '// Now if all the values are the same then set the textbox text
                        Dim objDocumentationFile As Object
                        objDocumentationFile = PropertyControlData.GetValueOrIndeterminateFromArray(m_stDocumentationFile)

                        If (Not (PropertyControlData.IsSpecialValue(objDocumentationFile))) Then
                            Me.txtXMLDocumentationFile.Text = TryCast(objDocumentationFile, String)
                        End If
                    End If
                End If

                Me.txtXMLDocumentationFile.Focus()
            Else
                '// Disable the checkbox
                Me.txtXMLDocumentationFile.Enabled = False
                Me.txtXMLDocumentationFile.Text = ""

                '// Clear the values
                Dim i As Integer
                For i = 0 To m_stDocumentationFile.Length - 1
                    m_stDocumentationFile(i) = ""
                Next
            End If

            If Not m_bInsideInternalUpdate Then
                SetDirty(Me.txtXMLDocumentationFile)
            End If
        End Sub


        ''' <summary>
        ''' Fired when the conditional compilations contants textbox has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values
        ''' </summary>
        Private Sub DocumentationFile_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtXMLDocumentationFile.TextChanged
            If Not m_bInsideInternalUpdate Then
                Debug.Assert(m_stDocumentationFile IsNot Nothing)
                For i As Integer = 0 To m_stDocumentationFile.Length - 1
                    'store it
                    m_stDocumentationFile(i) = txtXMLDocumentationFile.Text
                Next

                'No need to mark the property dirty - the property page architecture hooks up the FormControl automatically
                '  to TextChanged and will mark it dirty, and will make sure it's persisted on LostFocus.
            End If
        End Sub

        Private Sub PlatformTarget_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboPlatformTarget.SelectionChangeCommitted
            If m_fInsideInit OrElse m_bInsideInternalUpdate Then
                Return
            End If

            ' Changes to the PlatformTarget may affect whether Prefer32Bit is enabled
            RefreshEnabledStatusForPrefer32Bit(Me.chkPrefer32Bit)
        End Sub

#Region "Special handling of the conditional compilation constants textbox and the Define DEBUG/TRACE checkboxes"

        'Intended behavior:
        '  For simplified configurations mode ("Tools.Options.Projects and Solutions.Show Advanced Configurations" is off),
        '    we want the display to show only the release value for the DEBUG constant, and keep DEBUG defined always for
        '    the Debug configuration.  If the user changes the DEBUG constant checkbox in simplified mode, then the change
        '    should only affect the Debug configuration.
        '    For the TRACE constant checkbox, we want the normal behavior (show indeterminate if they're different, but they
        '    won't be for the default templates in simplified configs mode).
        '    The conditional compilation textbox likewise should show indetermine if the debug and release values differ, but
        '    for the default templates they won't.
        '    This behavior is not easy to get, because the DEBUG/TRACE checkboxes are not actual properties in C# like they
        '    are in VB, but are rather parsed from the conditional compilation value.  The conditional compilation textbox
        '    then shows any remaining constants that the user defines besides DEBUG and TRACE>
        '  For advanced configurations, we still parse the conditional compilation constants into DEBUG, TRACE, and everything
        '    else, but we should use normal indeterminate behavior for all of these controls if the values differ in any of the
        '    selected configurations.
        '
        'Note: a minor disadvantage with the current implementation is that the property page architecture doesn't know about
        '  the virtual "DEBUG" and "TRACE" properties that we've created, so the undo/redo descriptions for changes to these
        '  properties will always just say "DefineConstants"




        ''' <summary>
        ''' Fired when the conditional compilations contants textbox has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values
        ''' </summary>
        Private Sub DefineConstants_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtConditionalCompilationSymbols.TextChanged
            If Not m_bInsideInternalUpdate Then
                Debug.Assert(m_stCondCompSymbols IsNot Nothing)
                For i As Integer = 0 To m_stCondCompSymbols.Length - 1
                    'Parse the original compilation constants value for this configuration (we need to do this
                    '  to get the original DEBUG/TRACE values for these configurations - we can't rely on the
                    '  current control values for these two because they might be indeterminate)
                    Dim OriginalOtherConstants As String = ""
                    Dim DebugDefined, TraceDefined As Boolean
                    ParseConditionalCompilationConstants(m_stCondCompSymbols(i), DebugDefined, TraceDefined, OriginalOtherConstants)

                    'Now build the new string based off of the old DEBUG/TRACE values and the new string the user entered for any
                    '  other constants
                    Dim NewOtherConstants As String = txtConditionalCompilationSymbols.Text
                    Dim NewCondCompSymbols As String = NewOtherConstants
                    If DebugDefined Then
                        NewCondCompSymbols = AddSymbol(NewCondCompSymbols, Const_CondConstantDEBUG)
                    End If
                    If TraceDefined Then
                        NewCondCompSymbols = AddSymbol(NewCondCompSymbols, Const_CondConstantTRACE)
                    End If

                    '... and store it
                    m_stCondCompSymbols(i) = NewCondCompSymbols
                Next

                'No need to mark the property dirty - the property page architecture hooks up the FormControl automatically
                '  to TextChanged and will mark it dirty, and will make sure it's persisted on LostFocus.
            End If
        End Sub


        ''' <summary>
        ''' Fired when the "Define DEBUG Constant" check has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values.
        ''' </summary>
        Private Sub chkDefineDebug_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkDefineDebug.CheckedChanged
            If Not m_bInsideInternalUpdate Then
                Dim DebugIndexDoNotChange As Integer 'Index to avoid changing, if in simplified configs mode
                If IsSimplifiedConfigs() Then
                    'In simplified configs mode, we do not want to change the value of the DEBUG constant
                    '  in the Debug configuration, but rather only in the Release configuration
                    Debug.Assert(m_stCondCompSymbols.Length = 2, "In simplified configs, we should only have two configurations")
                    DebugIndexDoNotChange = GetIndexOfConfiguration(Const_DebugConfiguration)
                Else
                    DebugIndexDoNotChange = -1 'Go ahead and make changes in all selected configurations
                End If

                For i As Integer = 0 To m_stCondCompSymbols.Length - 1
                    If i <> DebugIndexDoNotChange Then
                        Select Case chkDefineDebug.CheckState
                            Case CheckState.Checked
                                'Make sure DEBUG is present in the configuration
                                m_stCondCompSymbols(i) = AddSymbol(m_stCondCompSymbols(i), Const_CondConstantDEBUG)
                            Case CheckState.Unchecked
                                'Remove DEBUG from the configuration
                                m_stCondCompSymbols(i) = RemoveSymbol(m_stCondCompSymbols(i), Const_CondConstantDEBUG)
                            Case Else
                                Debug.Fail("If the user changed the checked state, it should be checked or unchecked")
                        End Select
                    End If
                Next

                SetDirty(VsProjPropId.VBPROJPROPID_DefineConstants, True)
            End If
        End Sub


        ''' <summary>
        ''' Fired when the "Define DEBUG Constant" check has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values.
        ''' </summary>
        Private Sub chkDefineTrace_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkDefineTrace.CheckedChanged
            If Not m_bInsideInternalUpdate Then
                For i As Integer = 0 To m_stCondCompSymbols.Length - 1
                    Select Case chkDefineTrace.CheckState
                        Case CheckState.Checked
                            'Make sure TRACE is present in the configuration
                            m_stCondCompSymbols(i) = AddSymbol(m_stCondCompSymbols(i), Const_CondConstantTRACE)
                        Case CheckState.Unchecked
                            'Remove TRACE from the configuration
                            m_stCondCompSymbols(i) = RemoveSymbol(m_stCondCompSymbols(i), Const_CondConstantTRACE)
                        Case Else
                            Debug.Fail("If the user changed the checked state, it should be checked or unchecked")
                    End Select
                Next

                SetDirty(VsProjPropId.VBPROJPROPID_DefineConstants, True)
            End If
        End Sub


        ''' <summary>
        ''' Given DefineConstants string, parse it into a DEBUG value, a TRACE value, and everything else
        ''' </summary>
        Private Sub ParseConditionalCompilationConstants(ByVal DefineConstantsFullValue As String, ByRef DebugDefined As Boolean, ByRef TraceDefined As Boolean, ByRef OtherConstants As String)
            'Start out with the full set of defined constants
            OtherConstants = DefineConstantsFullValue

            'Check for DEBUG
            If (FindSymbol(OtherConstants, Const_CondConstantDEBUG)) Then
                DebugDefined = True

                'Strip it out
                OtherConstants = RemoveSymbol(OtherConstants, Const_CondConstantDEBUG)
            Else
                DebugDefined = False
            End If

            'Check for TRACE
            If (FindSymbol(OtherConstants, Const_CondConstantTRACE)) Then
                TraceDefined = True

                'Strip it out
                OtherConstants = RemoveSymbol(OtherConstants, Const_CondConstantTRACE)
            Else
                TraceDefined = False
            End If
        End Sub


        ''' <summary>
        ''' Multi-value setter for the conditional compilation constants value.  We parse the values and determine
        '''   what to display in the textbox and checkboxes.
        ''' </summary>
        Private Function ConditionalCompilationSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal values() As Object) As Boolean
            Debug.Assert(values IsNot Nothing)
#If DEBUG Then
            For i As Integer = 0 To values.Length - 1
                Debug.Assert(values(i) IsNot Nothing)
                Debug.Assert(Not PropertyControlData.IsSpecialValue(values(i)))
            Next

            If Switches.PDProperties.TraceInfo Then
                Switches.TracePDProperties(TraceLevel.Info, "ConditionalCompilationSet: Initial Values:")
                For i As Integer = 0 To values.Length - 1
                    Switches.TracePDProperties(TraceLevel.Info, "  Value #" & i & ": " & Common.DebugToString(values(i)))
                Next
            End If
#End If

            'Store off the conditional full (unparsed) compilation strings, we'll need this in the getter (because the
            '  values displayed in the controls are lossy when there are indeterminate values).
            ReDim m_stCondCompSymbols(values.Length - 1)
            values.CopyTo(m_stCondCompSymbols, 0)

            m_bInsideInternalUpdate = True
            Try
                Dim DebugDefinedValues(values.Length - 1) As Object 'Defined as object so we can use GetValueOrIndeterminateFromArray
                Dim TraceDefinedValues(values.Length - 1) As Object
                Dim OtherConstantsValues(values.Length - 1) As String

                'Parse out each individual set of DefineConstants values from the project
                For i As Integer = 0 To values.Length - 1
                    Dim FullDefineConstantsValue As String = DirectCast(values(i), String)
                    Dim DebugDefinedValue, TraceDefinedValue As Boolean
                    Dim OtherConstantsValue As String = ""

                    ParseConditionalCompilationConstants(FullDefineConstantsValue, DebugDefinedValue, TraceDefinedValue, OtherConstantsValue)
                    DebugDefinedValues(i) = DebugDefinedValue
                    TraceDefinedValues(i) = TraceDefinedValue
                    OtherConstantsValues(i) = OtherConstantsValue
                Next

                'Figure out whether the values each configuration are the same or different.  For each
                '  of these properties, get either the value which is the same across all of the values,
                '  or get a value of Inderminate.
                Dim DebugDefined As Object = PropertyControlData.GetValueOrIndeterminateFromArray(DebugDefinedValues)
                Dim TraceDefined As Object = PropertyControlData.GetValueOrIndeterminateFromArray(TraceDefinedValues)
                Dim OtherConstants As Object = PropertyControlData.GetValueOrIndeterminateFromArray(OtherConstantsValues)

                If IsSimplifiedConfigs() Then
                    'Special behavior for simplified configurations - we want to only display the
                    '  release value of the DEBUG checkbox.
                    Dim ReleaseIndex As Integer = GetIndexOfConfiguration(Const_ReleaseConfiguration)
                    If ReleaseIndex >= 0 Then
                        DebugDefined = DebugDefinedValues(ReleaseIndex) 'Get the release-config value for DEBUG constant
                    End If
                End If

                'Finally, set the control values to their calculated state
                If PropertyControlData.IsSpecialValue(DebugDefined) Then
                    chkDefineDebug.CheckState = CheckState.Indeterminate
                Else
                    SetCheckboxDeterminateState(chkDefineDebug, CBool(DebugDefined))
                End If
                If PropertyControlData.IsSpecialValue(TraceDefined) Then
                    chkDefineTrace.CheckState = CheckState.Indeterminate
                Else
                    SetCheckboxDeterminateState(chkDefineTrace, CBool(TraceDefined))
                End If
                If PropertyControlData.IsSpecialValue(OtherConstants) Then
                    txtConditionalCompilationSymbols.Text = ""
                Else
                    txtConditionalCompilationSymbols.Text = DirectCast(OtherConstants, String)
                End If

            Finally
                m_bInsideInternalUpdate = False
            End Try

            Return True
        End Function


        ''' <summary>
        ''' Multi-value getter for the conditional compilation constants values.
        ''' </summary>
        Private Function ConditionalCompilationGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef values() As Object) As Boolean
            'Fetch the original values we stored in the setter (the values stored in the controls are lossy when there are indeterminate values)
            Debug.Assert(m_stCondCompSymbols IsNot Nothing)
            ReDim values(m_stCondCompSymbols.Length - 1)
            m_stCondCompSymbols.CopyTo(values, 0)
            Return True
        End Function


        ''' <summary>
        ''' Searches in the RawPropertiesObjects for a configuration object whose name matches the name passed in,
        '''   and returns the index to it.
        ''' </summary>
        ''' <param name="ConfigurationName"></param>
        ''' <returns>The index of the found configuration, or -1 if it was not found.</returns>
        ''' <remarks>
        ''' We're only guaranteed to find the "Debug" or "Release" configurations when in
        '''   simplified configuration mode.
        ''' </remarks>
        Private Function GetIndexOfConfiguration(ByVal ConfigurationName As String) As Integer
            Debug.Assert(IsSimplifiedConfigs, "Shouldn't be calling this in advanced configs mode - not guaranteed to have Debug/Release configurations")

            Dim DefineConstantsData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants)
            Debug.Assert(DefineConstantsData IsNot Nothing)

            Dim Objects() As Object = RawPropertiesObjects(DefineConstantsData)
            Dim Index As Integer = 0
            For Each Obj As Object In Objects
                Debug.Assert(Obj IsNot Nothing, "Why was Nothing passed in as a config object?")
                Dim Config As IVsCfg = TryCast(Obj, IVsCfg)
                Debug.Assert(Config IsNot Nothing, "Object was not IVsCfg")
                If Config IsNot Nothing Then
                    Dim ConfigName As String = Nothing
                    Dim PlatformName As String = Nothing
                    Common.ShellUtil.GetConfigAndPlatformFromIVsCfg(Config, ConfigName, PlatformName)
                    If ConfigurationName.Equals(ConfigName, StringComparison.CurrentCultureIgnoreCase) Then
                        'Found it - return the index to it
                        Return Index
                    End If
                End If
                Index += 1
            Next

            Debug.Fail("Unable to find the configuration '" & ConfigurationName & "'")
            Return -1
        End Function


        ''' <summary>
        ''' Returns whether or not we're in simplified config mode for this project, which means that
        '''   we hide the configuration/platform comboboxes.
        ''' </summary>
        Public Function IsSimplifiedConfigs() As Boolean
            Return Common.ShellUtil.GetIsSimplifiedConfigMode(ProjectHierarchy)
        End Function


        ''' <summary>
        ''' Given a string containing conditional compilation constants, adds the given constant to it, if it
        '''   doesn't already exist.
        ''' </summary>
        Public Function AddSymbol(ByVal stOldCondCompConstants As String, ByVal stSymbol As String) As String
            '// See if we find it
            Dim rgConstants() As String
            Dim bFound As Boolean = False
            Dim stNewConstants As String = ""

            If (Not (IsNothing(stOldCondCompConstants))) Then
                rgConstants = stOldCondCompConstants.Split(New [Char]() {";"c})

                Dim stTemp As String

                If (Not (IsNothing(rgConstants))) Then
                    For Each stTemp In rgConstants
                        If (String.Compare(Trim(stTemp), stSymbol, StringComparison.Ordinal) = 0) Then
                            bFound = True
                            Exit For
                        End If
                    Next
                End If
            End If

            If (Not bFound) Then
                '// Add it to the beginning
                stNewConstants = stSymbol

                If stOldCondCompConstants <> "" Then
                    stNewConstants += ";"
                End If
                stNewConstants += stOldCondCompConstants

                Return stNewConstants
            Else
                Return stOldCondCompConstants
            End If
        End Function

        ''' <summary>
        ''' Given a string containing conditional compilation constants, determines if the given constant is defined in it
        ''' </summary>
        Public Function FindSymbol(ByVal stOldCondCompConstants As String, ByVal stSymbol As String) As Boolean
            '// See if we find it
            Dim rgConstants() As String

            If (Not (IsNothing(stOldCondCompConstants))) Then
                rgConstants = stOldCondCompConstants.Split(New [Char]() {";"c})

                Dim stTemp As String

                If (Not (IsNothing(rgConstants))) Then
                    For Each stTemp In rgConstants
                        If (String.Compare(Trim(stTemp), stSymbol, StringComparison.Ordinal) = 0) Then
                            Return True
                        End If
                    Next
                End If
            End If
            Return False
        End Function


        ''' <summary>
        ''' Given a string containing conditional compilation constants, removes the given constant from it, if it
        '''   is in the list.
        ''' </summary>
        Public Function RemoveSymbol(ByVal stOldCondCompConstants As String, ByVal stSymbol As String) As String
            '// Look for the DEBUG constant
            Dim rgConstants() As String
            Dim stNewConstants As String = ""

            If (Not (IsNothing(stOldCondCompConstants))) Then
                rgConstants = stOldCondCompConstants.Split(New [Char]() {";"c})

                Dim stTemp As String

                If (Not (IsNothing(rgConstants))) Then
                    For Each stTemp In rgConstants
                        If (String.Compare(Trim(stTemp), stSymbol, StringComparison.Ordinal) <> 0) Then
                            If (stNewConstants <> "") Then
                                stNewConstants += ";"
                            End If

                            stNewConstants += stTemp
                        End If
                    Next
                End If
            Else
                stNewConstants = ""
            End If

            Return stNewConstants
        End Function

#End Region

    End Class

End Namespace
