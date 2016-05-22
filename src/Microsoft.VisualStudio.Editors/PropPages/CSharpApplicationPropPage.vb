' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' C#/J# application property page - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class CSharpApplicationPropPage
        Inherits ApplicationPropPage

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        ''' <remarks></remarks>
        Protected Overrides Sub PopulateStartupObject(ByVal StartUpObjectSupported As Boolean, ByVal PopulateDropdown As Boolean)
            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True

            Try

                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData("StartupObject")

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedItem = .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))
                        .Text = SR.GetString(SR.PPG_Application_StartupObjectNotSet)
                        .SelectedIndex = 0  '// Set it to NotSet
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        Me.StartupObject.Enabled = False
                        Me.StartupObjectLabel.Enabled = False
                    End If
                Else
                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc

                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()

                        ' (Not Set) should always be available in the list
                        .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))

                        If PopulateDropdown Then
                            RefreshPropertyStandardValues()

                            'Certain project types may not support standard values
                            If prop.Converter.GetStandardValuesSupported() Then
                                Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                Using New WaitCursor
                                    For Each str As String In prop.Converter.GetStandardValues()
                                        .Items.Add(str)
                                    Next
                                End Using
                            End If
                        End If

                        '(Okay to use InitialValue because we checked against IsMissing above)
                        Dim SelectedItemText As String = CStr(StartupObjectPropertyControlData.InitialValue)
                        If IsNothing(SelectedItemText) OrElse (SelectedItemText = "") Then
                            SelectedItemText = SR.GetString(SR.PPG_Application_StartupObjectNotSet)
                        End If

                        .SelectedItem = SelectedItemText
                        If .SelectedItem Is Nothing Then
                            .Items.Add(SelectedItemText)
                            'CONSIDER: Can we use the object returned by .Items.Add to set the selection?
                            .SelectedItem = SelectedItemText
                        End If
                    End With
                End If
            Finally
                m_fInsideInit = InsideInitSave
            End Try
        End Sub

        Protected Overrides Function StartupObjectGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If Not StartUpObjectSupported() Then
                value = ""
            Else
                If StartupObject.SelectedItem IsNot Nothing Then
                    Dim StartupObjectText As String = TryCast(StartupObject.SelectedItem, String)

                    If Not IsNothing(StartupObjectText) Then
                        If String.Compare(StartupObjectText, SR.GetString(SR.PPG_Application_StartupObjectNotSet)) <> 0 Then
                            value = StartupObjectText
                        Else
                            '// the value is (Not Set) so just leave it empty
                            value = ""
                        End If
                    Else
                        value = ""
                    End If
                Else
                    value = ""
                End If
            End If
            Return True
        End Function

    End Class


End Namespace
