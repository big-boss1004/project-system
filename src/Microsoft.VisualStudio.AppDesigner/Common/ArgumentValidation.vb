' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    Friend Module ArgumentValidation

        ''' <summary>
        ''' Throws ArgumentNullException if the argument is Nothing
        ''' </summary>
        ''' <param name="argument"></param>
        ''' <param name="parameter"></param>
        ''' <remarks></remarks>
        Public Sub ValidateArgumentNotNothing(argument As Object, parameter As String)
            If argument Is Nothing Then
                Throw New ArgumentNullException(parameter)
            End If
        End Sub

        ''' <summary>
        ''' Throws ArgumentException if the argument is Nothing or empty string
        ''' </summary>
        ''' <param name="argument"></param>
        ''' <param name="parameter"></param>
        ''' <remarks></remarks>
        Public Sub ValidateArgumentNotNothingOrEmptyString(argument As String, parameter As String)
            If argument Is Nothing OrElse argument.Length = 0 Then
                Throw CreateArgumentException(parameter)
            End If
        End Sub


        ''' <summary>
        ''' Creates an ArgumentException based on the name of the argument that is invalid.
        ''' </summary>
        ''' <param name="argumentName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function CreateArgumentException(argumentName As String) As Exception
            Return New ArgumentException(String.Format(My.Resources.Microsoft_VisualStudio_AppDesigner_Designer.General_InvalidArgument_1Arg, argumentName))
        End Function
    End Module
End Namespace
