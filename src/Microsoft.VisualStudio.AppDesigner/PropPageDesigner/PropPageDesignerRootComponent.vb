﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' This class represents the root component of the Application Designer.
    ''' </summary>
    <Designer(GetType(PropPageDesignerRootDesigner), GetType(IRootDesigner))>
    Public NotInheritable Class PropPageDesignerRootComponent
        Inherits Component

        'Private cache for important data
        Private _hierarchy As IVsHierarchy
        Private _itemId As UInteger
        Private _rootDesigner As PropPageDesignerRootDesigner
        Private ReadOnly _name As String = "PropPageDesignerRootComponent"

        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property



        ''' <summary>
        '''   Returns the root designer that is associated with this component, i.e., the
        '''   designer which is showing the UI to the user which allows this component's
        '''   resx file to be edited by the user.
        ''' </summary>
        ''' <value>The associated ResourceEditorRootDesigner.</value>
        Public ReadOnly Property RootDesigner As PropPageDesignerRootDesigner
            Get
                If _rootDesigner Is Nothing Then
                    'Not yet cached - get this info from the designer host
                    Debug.Assert(Not Container Is Nothing)
                    Dim Host As IDesignerHost = CType(Container, IDesignerHost)
                    _rootDesigner = CType(Host.GetDesigner(Me), PropPageDesignerRootDesigner)
                End If

                Debug.Assert(Not _rootDesigner Is Nothing, "Don't have an associated designer?!?")
                Return _rootDesigner
            End Get
        End Property

        ''' <summary>
        ''' The IVsHierarchy associated with the AppDesigner node
        ''' </summary>
        Public Property Hierarchy As IVsHierarchy
            Get
                Return _hierarchy
            End Get
            Set
                _hierarchy = Value
            End Set
        End Property

        ''' <summary>
        ''' The ItemId associated with the AppDesigner node
        ''' </summary>
        Public Property ItemId As UInteger
            Get
                Return _itemId
            End Get
            Set
                _itemId = Value
            End Set
        End Property

    End Class

End Namespace
