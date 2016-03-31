Imports System.Xml.Serialization

' This file is generated by the tools described in %dd%\users\vbdata\xgen\readme.txt 
'

Namespace Microsoft.VisualStudio.Editors.MyApplication
    Friend Class MyApplicationDataSerializationWriter
        Inherits System.Xml.Serialization.XmlSerializationWriter
        Sub Write2_MyApplicationData(ByVal n As String, ByVal ns As String, ByVal o As Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData, ByVal isNullable As Boolean, ByVal needType As Boolean)
            If o Is Nothing Then
                If isNullable Then
                    WriteNullTagLiteral(n, ns)
                End If
                Return
            End If
            If Not needType Then
                Dim t As System.Type = o.GetType()
                If Type.Equals(t, GetType(Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData)) Then
                Else
                    Throw CreateUnknownTypeException(o)
                End If
            End If
            EscapeName = True
            WriteStartElement(n, ns, o)
            If needType Then
                WriteXsiType("MyApplicationData", "")
            End If
            WriteElementStringRaw("MySubMain", "", System.Xml.XmlConvert.ToString(CType(o.MySubMain, System.Boolean)))
            WriteElementString("MainForm", "", o.MainFormNoRootNS)
            WriteElementStringRaw("SingleInstance", "", System.Xml.XmlConvert.ToString(CType(o.SingleInstance, System.Boolean)))
            WriteElementStringRaw("ShutdownMode", "", System.Xml.XmlConvert.ToString(CType(o.ShutdownMode, System.Int32)))
            WriteElementStringRaw("EnableVisualStyles", "", System.Xml.XmlConvert.ToString(CType(o.EnableVisualStyles, System.Boolean)))
            WriteElementStringRaw("AuthenticationMode", "", System.Xml.XmlConvert.ToString(CType(o.AuthenticationMode, System.Int32)))
            WriteElementString("SplashScreen", "", o.SplashScreenNoRootNS)
            WriteElementStringRaw("SaveMySettingsOnExit", "", System.Xml.XmlConvert.ToString(CType(o.SaveMySettingsOnExit, System.Boolean)))
            WriteEndElement(o)
        End Sub 'Write2_MyApplicationData


        Protected Overrides Sub InitCallbacks()
        End Sub 'InitCallbacks


        Public Sub Write3_MyApplicationData(ByVal o As Object)
            WriteStartDocument()
            If o Is Nothing Then
                WriteNullTagLiteral("MyApplicationData", "")
                Return
            End If
            TopLevelElement()
            Write2_MyApplicationData("MyApplicationData", "", CType(o, Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData), True, False)
        End Sub 'Write3_MyApplicationData 

        Private publicMethods As System.Collections.Hashtable = Nothing
        Private hashTableLock As New Object

        Public Function GetPublicMethods() As System.Collections.Hashtable
            If publicMethods Is Nothing Then
                publicMethods = New System.Collections.Hashtable()
            End If
            Return publicMethods
        End Function 'GetPublicMethods

        Public Shared Function CanSerialize(ByVal type As System.Type) As System.Boolean
            Return False
        End Function 'CanSerialize 
    End Class 'MyApplicationDataSerializationWriter


    Friend Class MyApplicationDataSerializationReader
        Inherits System.Xml.Serialization.XmlSerializationReader

        Function Read2_MyApplicationData(ByVal isNullable As Boolean, ByVal checkType As Boolean) As Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData
            Dim xsiType As System.Xml.XmlQualifiedName = Nothing
            If (checkType) Then xsiType = GetXsiType()

            Dim isNull As Boolean = False

            If isNullable Then
                isNull = ReadNull()
            End If

            If checkType Then
                If isNull AndAlso Not (xsiType Is Nothing) Then
                    Return CType(ReadTypedNull(xsiType), Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData)
                End If
                If (xsiType Is Nothing) OrElse (CType(xsiType, System.Xml.XmlQualifiedName).Name = id1_MyApplicationData AndAlso CType(xsiType, System.Xml.XmlQualifiedName).Namespace = id2_Item) Then
                Else
                    Throw CreateUnknownTypeException(CType(xsiType, System.Xml.XmlQualifiedName))
                End If
            End If

            If isNull Then
                Return Nothing
            End If
            DecodeName = True

            Dim o As Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData
            o = New Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData()
            Dim paramsRead(8) As Boolean

            While Reader.MoveToNextAttribute()
                If Not IsXmlnsAttribute(Reader.Name) Then
                    UnknownNode(CType(o, Object))
                End If
            End While

            Reader.MoveToElement()

            If Reader.IsEmptyElement Then
                Reader.Skip()
                Return o
            End If

            Reader.ReadStartElement()
            Reader.MoveToContent()

            While Reader.NodeType <> System.Xml.XmlNodeType.EndElement AndAlso Reader.NodeType <> System.Xml.XmlNodeType.None
                If Reader.NodeType = System.Xml.XmlNodeType.Element Then
                    If Not paramsRead(0) AndAlso (Reader.LocalName = id3_MySubMain AndAlso Reader.NamespaceURI = id2_Item) Then
                        o.MySubMain = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString())
                        paramsRead(0) = True

                    ElseIf Not paramsRead(1) AndAlso (Reader.LocalName = id4_MainForm AndAlso Reader.NamespaceURI = id2_Item) Then
                        o.MainFormNoRootNS = Reader.ReadElementString()
                        paramsRead(1) = True

                    ElseIf Not paramsRead(2) AndAlso (Reader.LocalName = id5_SingleInstance AndAlso Reader.NamespaceURI = id2_Item) Then
                        o.SingleInstance = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString())
                        paramsRead(2) = True

                    ElseIf Not paramsRead(3) AndAlso (Reader.LocalName = id6_ShutdownMode AndAlso Reader.NamespaceURI = id2_Item) Then
                        o.ShutdownMode = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString())
                        paramsRead(3) = True

                    ElseIf Not paramsRead(4) AndAlso Reader.LocalName = id7_EnableVisualStyles AndAlso Reader.NamespaceURI = id2_Item Then
                        o.EnableVisualStyles = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString())
                        paramsRead(4) = True

                    ElseIf Not paramsRead(5) AndAlso Reader.LocalName = id8_AuthenticationMode AndAlso Reader.NamespaceURI = id2_Item Then
                        o.AuthenticationMode = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString())
                        paramsRead(5) = True

                    ElseIf Not paramsRead(6) AndAlso Reader.LocalName = id9_SplashScreen AndAlso Reader.NamespaceURI = id2_Item Then
                        o.SplashScreenNoRootNS = Reader.ReadElementString()
                        paramsRead(6) = True

                    ElseIf Not paramsRead(7) AndAlso Reader.LocalName = id10_ApplicationType AndAlso Reader.NamespaceURI = id2_Item Then
                        Reader.ReadElementString()
                        paramsRead(7) = True

                    ElseIf Not paramsRead(8) AndAlso Reader.LocalName = id12_SaveMySettingsOnExit AndAlso Reader.NamespaceURI = id2_Item Then
                        o.SaveMySettingsOnExit = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString())
                        paramsRead(8) = True

                    Else
                        UnknownNode(CType(o, Object))
                    End If


                Else

                    UnknownNode(CType(o, Object))
                End If

                Reader.MoveToContent()
            End While

            ReadEndElement()

            Return o
        End Function 'Read2_MyApplicationData


        Protected Overrides Sub InitCallbacks()
        End Sub 'InitCallbacks


        Public Function Read4_MyApplicationData() As Object

            Dim o As Object = Nothing
            Reader.MoveToContent()

            If Reader.NodeType = System.Xml.XmlNodeType.Element Then
                If Reader.LocalName = id1_MyApplicationData AndAlso Reader.NamespaceURI = id2_Item Then
                    o = Read2_MyApplicationData(True, True)
                Else
                    Throw CreateUnknownNodeException()
                End If
            Else
                UnknownNode(Nothing)
            End If
            Return CType(o, Object)
        End Function 'Read4_MyApplicationData 

        Private id9_SplashScreen As String '
        Private id5_SingleInstance As String '
        Private id1_MyApplicationData As String '
        Private id10_ApplicationType As String '
        Private id6_ShutdownMode As String '
        Private id4_MainForm As String '
        Private id3_MySubMain As String '
        Private id2_Item As String '
        Private id7_EnableVisualStyles As String '
        Private id8_AuthenticationMode As String
        Private id12_SaveMySettingsOnExit As String


        Protected Overrides Sub InitIDs()

            id9_SplashScreen = Reader.NameTable.Add("SplashScreen")

            id5_SingleInstance = Reader.NameTable.Add("SingleInstance")

            id1_MyApplicationData = Reader.NameTable.Add("MyApplicationData")

            id10_ApplicationType = Reader.NameTable.Add("ApplicationType")

            id6_ShutdownMode = Reader.NameTable.Add("ShutdownMode")

            id4_MainForm = Reader.NameTable.Add("MainForm")

            id3_MySubMain = Reader.NameTable.Add("MySubMain")

            id2_Item = Reader.NameTable.Add("")

            id7_EnableVisualStyles = Reader.NameTable.Add("EnableVisualStyles")

            id8_AuthenticationMode = Reader.NameTable.Add("AuthenticationMode")

            id12_SaveMySettingsOnExit = Reader.NameTable.Add("SaveMySettingsOnExit")
        End Sub 'InitIDs 

        Private publicMethods As System.Collections.Hashtable = Nothing
        private lockObject as New Object

        Public Function GetPublicMethods() As System.Collections.Hashtable
            If publicMethods Is Nothing Then
                publicMethods = New System.Collections.Hashtable()
            End If
            Return publicMethods
        End Function 'GetPublicMethods 
    End Class 'MyApplicationDataSerializationReader


    Friend Class MyApplicationDataSerializer
        Inherits XmlSerializer

        Protected Overrides Function CreateReader() As XmlSerializationReader
            Return New MyApplicationDataSerializationReader()
        End Function 'CreateReader


        Protected Overrides Function CreateWriter() As XmlSerializationWriter
            Return New MyApplicationDataSerializationWriter()
        End Function 'CreateWriter


        Public Overrides Function CanDeserialize(ByVal xmlReader As System.Xml.XmlReader) As Boolean
            Return xmlReader.IsStartElement("MyApplicationData", "")
        End Function 'CanDeserialize


        Protected Overrides Sub Serialize(ByVal objectToSerialize As [Object], ByVal writer As XmlSerializationWriter)

            If Not (objectToSerialize Is Nothing) AndAlso Not Type.Equals(objectToSerialize.GetType(), GetType(Microsoft.VisualStudio.Editors.MyApplication.MyApplicationData)) Then
                Debug.Fail("Cannot serialize object of type " + objectToSerialize.GetType().FullName + " with MyApplicationDataSerializer. Object of type " + GetType(MyApplicationDataSerializer).FullName + " expected.")
                Throw New Package.InternalException()
            End If

            CType(writer, MyApplicationDataSerializationWriter).Write3_MyApplicationData(objectToSerialize)
        End Sub 'Serialize


        Protected Overrides Function Deserialize(ByVal reader As XmlSerializationReader) As Object

            Return CType(reader, MyApplicationDataSerializationReader).Read4_MyApplicationData()
        End Function 'Deserialize 
    End Class 'MyApplicationDataSerializer
End Namespace 'Microsoft.VisualStudio.Editors.MyApplication 

