﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' Implementation of the System.ComponentModel.Design.ITypeDiscoveryService
    ''' 
    ''' The DesignerTypeDiscoveryService differs from the "normal" VS Type Discovery service
    ''' in that it filters out types defined in the current project
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DesignerTypeDiscoveryService
        Implements ComponentModel.Design.ITypeDiscoveryService

        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _hierarchy As IVsHierarchy

        ''' <summary>
        ''' Create a new type discovery service associated with the given hierarchy
        ''' </summary>
        ''' <param name="sp"></param>
        ''' <param name="hierarchy"></param>
        ''' <remarks></remarks>
        Public Sub New(sp As IServiceProvider, hierarchy As IVsHierarchy)
            If sp Is Nothing Then Throw New ArgumentNullException(NameOf(sp))
            If hierarchy Is Nothing Then Throw New ArgumentNullException(NameOf(hierarchy))

            _serviceProvider = sp
            _hierarchy = hierarchy

        End Sub

#Region "ITypeDiscoveryService implementation"

        Private Function GetTypes(baseType As Type, excludeGlobalTypes As Boolean) As ICollection Implements ComponentModel.Design.ITypeDiscoveryService.GetTypes
            Return GetReferencedTypes(baseType, excludeGlobalTypes)
        End Function

#End Region

        ''' <summary>
        ''' Get all known types, excluding types in the current project
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function GetReferencedTypes() As ICollection(Of Type)
            Return GetReferencedTypes(GetType(Object), False)
        End Function

        ''' <summary>
        ''' Get an enumeration of all types that we know about in this project
        ''' </summary>
        ''' <param name="baseType">Only return types inheriting from this class</param>
        ''' <param name="shouldExcludeGlobalTypes">Exclude types in the GAC?</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function GetReferencedTypes(baseType As Type, shouldExcludeGlobalTypes As Boolean) As List(Of Type)
            Dim dynamicTypeService As Shell.Design.DynamicTypeService =
                DirectCast(_serviceProvider.GetService(GetType(Shell.Design.DynamicTypeService)), Shell.Design.DynamicTypeService)

            Dim trs As ComponentModel.Design.ITypeResolutionService = Nothing
            Dim tds As ComponentModel.Design.ITypeDiscoveryService = Nothing

            If dynamicTypeService IsNot Nothing Then
                tds = dynamicTypeService.GetTypeDiscoveryService(_hierarchy, VSITEMID.ROOT)
                trs = dynamicTypeService.GetTypeResolutionService(_hierarchy, VSITEMID.ROOT)
            End If
            Dim result As New List(Of Type)

            If tds IsNot Nothing AndAlso trs IsNot Nothing Then
                Dim excludedAssemblies As New Dictionary(Of System.Reflection.Assembly, Object)

                Dim outputs() As String = GetProjectOutputs(_serviceProvider, _hierarchy)
                For Each output As String In outputs
                    ' We don't want to return types defined in this project's output because that may include
                    ' the data types generated by this proxy... 
                    Dim assemblyToExclude As System.Reflection.Assembly = AssemblyFromProjectOutput(trs, output)
                    If assemblyToExclude IsNot Nothing Then
                        excludedAssemblies(assemblyToExclude) = Nothing
                    End If
                Next

                For Each t As Type In tds.GetTypes(baseType, shouldExcludeGlobalTypes)
                    If Not excludedAssemblies.ContainsKey(t.Assembly) Then
                        result.Add(t)
                    End If
                Next
            End If

            Return result
        End Function

        ''' <summary> 
        ''' Get access to the built assemblies for this project
        ''' </summary>
        ''' <param name="provider"></param>
        ''' <param name="hierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetProjectOutputs(provider As IServiceProvider, hierarchy As IVsHierarchy) As String()
            Try
                Dim buildManager As IVsSolutionBuildManager = TryCast(provider.GetService(GetType(IVsSolutionBuildManager)), IVsSolutionBuildManager)
                If buildManager Is Nothing Then Return Array.Empty(Of String)()

                Dim activeConfig(0) As IVsProjectCfg
                Dim activeConfig2 As IVsProjectCfg2

                VSErrorHandler.ThrowOnFailure(buildManager.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, hierarchy, activeConfig))

                activeConfig2 = TryCast(activeConfig(0), IVsProjectCfg2)

                If activeConfig2 IsNot Nothing Then
                    Dim outputGroup As IVsOutputGroup = Nothing
                    activeConfig2.OpenOutputGroup(BuildOutputGroup.Built, outputGroup)
                    Dim outputGroup2 As IVsOutputGroup2 = TryCast(outputGroup, IVsOutputGroup2)
                    If outputGroup2 IsNot Nothing Then
                        Dim output As IVsOutput2 = Nothing
                        VSErrorHandler.ThrowOnFailure(outputGroup2.get_KeyOutputObject(output))
                        If output IsNot Nothing Then
                            Dim url As String = Nothing
                            VSErrorHandler.ThrowOnFailure(output.get_DeploySourceURL(url))

                            If url <> "" Then
                                Return New String() {GetLocalPathUnescaped(url)}
                            End If
                        End If
                    End If
                End If
            Catch ex As System.Runtime.InteropServices.COMException
                ' We failed to get the project output paths... 
            End Try

            Return Array.Empty(Of String)
        End Function

        ''' <summary>
        ''' Given an assembly path
        ''' </summary>
        ''' <param name="typeResolutionService"></param>
        ''' <param name="projectOutput"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function AssemblyFromProjectOutput(typeResolutionService As ComponentModel.Design.ITypeResolutionService, projectOutput As String) As System.Reflection.Assembly
            If typeResolutionService Is Nothing Then Throw New ArgumentNullException(NameOf(typeResolutionService))

            If typeResolutionService IsNot Nothing Then
                Try
                    If IO.File.Exists(projectOutput) Then
                        Dim an As System.Reflection.AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(projectOutput)
                        Dim a As System.Reflection.Assembly = typeResolutionService.GetAssembly(an)
                        Return a
                    End If
                Catch ex As IO.FileNotFoundException
                    ' The assembly doesn't exist - it may not have been built yet
                Catch ex As IO.IOException
                    ' Unknown error when trying to load the file...
                Catch ex As Security.SecurityException
                    ' We didn't have permissions to load the file...
                End Try
            Else
                Debug.Fail("Huh!?")
            End If
            Return Nothing
        End Function


        ''' <devdoc>
        ''' This method takes a file URL and converts it to a local path.  The trick here is that
        ''' if there is a '#' in the path, everything after this is treated as a fragment.  So
        ''' we need to append the fragment to the end of the path.
        ''' </devdoc>
        Protected Shared Function GetLocalPath(fileName As String) As String
            Debug.Assert(fileName IsNot Nothing AndAlso fileName.Length > 0, "Cannot get local path, fileName is not valid")

            Dim uri As New Uri(fileName)
            Return uri.LocalPath & uri.Fragment
        End Function

        ' VSWhidbey 460000
        ' VSCore does not properly escape paths with the certain characters (for example, #) in 
        ' the URIs they provide.  Instead, if the path starts with file:', we will assume
        ' the rest of the string is the non-escaped path.
        Protected Shared Function GetLocalPathUnescaped(url As String) As String
            Dim filePrefix As String = "file:///"
            If url.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase) Then
                Return url.Substring(filePrefix.Length)
            Else
                Return GetLocalPath(url)
            End If
        End Function

    End Class

End Namespace
