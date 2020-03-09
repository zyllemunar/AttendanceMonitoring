Imports System.Diagnostics
Imports Emgu.CV.Structure
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO

Public Class Form1
    Dim currentFrame As Image(Of Bgr, [Byte])
    Dim grabber As Capture
    Dim face As HaarCascade
    Dim eye As HaarCascade
    Dim font As New MCvFont(CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5, 0.5)
    Dim result As Image(Of Gray, Byte), TrainedFace As Image(Of Gray, Byte) = Nothing
    Dim gray As Image(Of Gray, Byte) = Nothing
    Dim trainingImages As New List(Of Image(Of Gray, Byte))()
    Dim labels As New List(Of String)()
    Dim NamePersons As New List(Of String)()
    Dim ContTrain As Integer, NumLabels As Integer, t As Integer
    Dim name As String, names As String = Nothing

    <Serializable()> _
    Public Class EigenObjectRecognizer
        Private _eigenImages As Image(Of Gray, [Single])()
        Private _avgImage As Image(Of Gray, [Single])
        Private _eigenValues As Matrix(Of Single)()
        Private _labels As String()
        Private _eigenDistanceThreshold As Double

  
        Public Property EigenImages() As Image(Of Gray, [Single])()
            Get
                Return _eigenImages
            End Get
            Set(ByVal value As Image(Of Gray, [Single])())
                _eigenImages = value
            End Set
        End Property

       
        Public Property Labels() As [String]()
            Get
                Return _labels
            End Get
            Set(ByVal value As [String]())
                _labels = value
            End Set
        End Property

        
        Public Property EigenDistanceThreshold() As Double
            Get
                Return _eigenDistanceThreshold
            End Get
            Set(ByVal value As Double)
                _eigenDistanceThreshold = value
            End Set
        End Property

       
        Public Property AverageImage() As Image(Of Gray, [Single])
            Get
                Return _avgImage
            End Get
            Set(ByVal value As Image(Of Gray, [Single]))
                _avgImage = value
            End Set
        End Property

       
        Public Property EigenValues() As Matrix(Of Single)()
            Get
                Return _eigenValues
            End Get
            Set(ByVal value As Matrix(Of Single)())
                _eigenValues = value
            End Set
        End Property

        Private Sub New()
        End Sub



        Public Sub New(ByVal images As Image(Of Gray, [Byte])(), ByRef termCrit As MCvTermCriteria)
            Me.New(images, GenerateLabels(images.Length), termCrit)
        End Sub

        Private Shared Function GenerateLabels(ByVal size As Integer) As [String]()
            Dim labels As [String]() = New String(size - 1) {}
            For i As Integer = 0 To size - 1
                labels(i) = i.ToString()
            Next
            Return labels
        End Function

       
        Public Sub New(ByVal images As Image(Of Gray, [Byte])(), ByVal labels As [String](), ByRef termCrit As MCvTermCriteria)
            Me.New(images, labels, 0, termCrit)
        End Sub

        
        Public Sub New(ByVal images As Image(Of Gray, [Byte])(), ByVal labels As [String](), ByVal eigenDistanceThreshold As Double, ByRef termCrit As MCvTermCriteria)
            Debug.Assert(images.Length = labels.Length, "The number of images should equals the number of labels")
            Debug.Assert(eigenDistanceThreshold >= 0.0, "Eigen-distance threshold should always >= 0.0")

            CalcEigenObjects(images, termCrit, _eigenImages, _avgImage)

           

            _eigenValues = Array.ConvertAll(Of Image(Of Gray, [Byte]), Matrix(Of Single))(images, Function(img As Image(Of Gray, [Byte])) New Matrix(Of Single)(EigenDecomposite(img, _eigenImages, _avgImage)))

            _labels = labels

            _eigenDistanceThreshold = eigenDistanceThreshold
        End Sub

#Region "static methods"
        ''' <summary>
        ''' Caculate the eigen images for the specific traning image
        ''' </summary>
        ''' <param name="trainingImages">The images used for training </param>
        ''' <param name="termCrit">The criteria for tranning</param>
        ''' <param name="eigenImages">The resulting eigen images</param>
        ''' <param name="avg">The resulting average image</param>
        Public Shared Sub CalcEigenObjects(ByVal trainingImages As Image(Of Gray, [Byte])(), ByRef termCrit As MCvTermCriteria, ByRef eigenImages As Image(Of Gray, [Single])(), ByRef avg As Image(Of Gray, [Single]))
            Dim width As Integer = trainingImages(0).Width
            Dim height As Integer = trainingImages(0).Height

            Dim inObjs As IntPtr() = Array.ConvertAll(Of Image(Of Gray, [Byte]), IntPtr)(trainingImages, Function(img As Image(Of Gray, [Byte])) img.Ptr)

            If termCrit.max_iter <= 0 OrElse termCrit.max_iter > trainingImages.Length Then
                termCrit.max_iter = trainingImages.Length
            End If

            Dim maxEigenObjs As Integer = termCrit.max_iter

            '#Region "initialize eigen images"
            eigenImages = New Image(Of Gray, Single)(maxEigenObjs - 1) {}
            For i As Integer = 0 To eigenImages.Length - 1
                eigenImages(i) = New Image(Of Gray, Single)(width, height)
            Next
            Dim eigObjs As IntPtr() = Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr)(eigenImages, Function(img As Image(Of Gray, [Single])) img.Ptr)
            '#End Region

            avg = New Image(Of Gray, [Single])(width, height)

            CvInvoke.cvCalcEigenObjects(inObjs, termCrit, eigObjs, Nothing, avg.Ptr)
        End Sub

        ''' <summary>
        ''' Decompose the image as eigen values, using the specific eigen vectors
        ''' </summary>
        ''' <param name="src">The image to be decomposed</param>
        ''' <param name="eigenImages">The eigen images</param>
        ''' <param name="avg">The average images</param>
        ''' <returns>Eigen values of the decomposed image</returns>
        Public Shared Function EigenDecomposite(ByVal src As Image(Of Gray, [Byte]), ByVal eigenImages As Image(Of Gray, [Single])(), ByVal avg As Image(Of Gray, [Single])) As Single()
            Return CvInvoke.cvEigenDecomposite(src.Ptr, Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr)(eigenImages, Function(img As Image(Of Gray, [Single])) img.Ptr), avg.Ptr)
        End Function
#End Region

      
        Public Function EigenProjection(ByVal eigenValue As Single()) As Image(Of Gray, [Byte])
            Dim res As Image(Of Gray, [Byte]) = New Image(Of Gray, Byte)(_avgImage.Width, _avgImage.Height)
            CvInvoke.cvEigenProjection(Array.ConvertAll(Of Image(Of Gray, [Single]), IntPtr)(_eigenImages, Function(img As Image(Of Gray, [Single])) img.Ptr), eigenValue, _avgImage.Ptr, res.Ptr)
            Return res
        End Function

       
        Public Function GetEigenDistances(ByVal image As Image(Of Gray, [Byte])) As Single()
            Using eigenValue As New Matrix(Of Single)(EigenDecomposite(image, _eigenImages, _avgImage))
                Return Array.ConvertAll(Of Matrix(Of Single), Single)(_eigenValues, Function(eigenValueI As Matrix(Of Single)) CSng(CvInvoke.cvNorm(eigenValue.Ptr, eigenValueI.Ptr, Emgu.CV.CvEnum.NORM_TYPE.CV_L2, IntPtr.Zero)))
            End Using
        End Function

        
        Public Sub FindMostSimilarObject(ByVal image As Image(Of Gray, [Byte]), ByRef index As Integer, ByRef eigenDistance As Single, ByRef label As [String])
            Dim dist As Single() = GetEigenDistances(image)

            index = 0
            eigenDistance = dist(0)
            For i As Integer = 1 To dist.Length - 1
                If dist(i) < eigenDistance Then
                    index = i
                    eigenDistance = dist(i)
                End If
            Next
            label = Labels(index)
        End Sub

      
        Public Function Recognize(ByVal image As Image(Of Gray, [Byte])) As [String]
            Dim index As Integer
            Dim eigenDistance As Single
            Dim label As [String]
            FindMostSimilarObject(image, index, eigenDistance, label)

            Return If((_eigenDistanceThreshold <= 0 OrElse eigenDistance < _eigenDistanceThreshold), _labels(index), [String].Empty)
        End Function
    End Class

    Public Sub New()
        InitializeComponent()
        face = New HaarCascade("haarcascade_frontalface_default.xml")
        Try
            
            Dim Labelsinfo As String = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt")
            Dim Labels__1 As String() = Labelsinfo.Split("%"c)
            NumLabels = Convert.ToInt16(Labels__1(0))
            ContTrain = NumLabels
            Dim LoadFaces As String

            For tf As Integer = 1 To NumLabels
                LoadFaces = "face" & tf & ".bmp"
                trainingImages.Add(New Image(Of Gray, Byte)(Application.StartupPath + "/TrainedFaces/" & LoadFaces))
                labels.Add(Labels__1(tf))

            Next
        Catch e As Exception
            
            MessageBox.Show("Empty Database", "Load Faces", MessageBoxButtons.OK, MessageBoxIcon.Information)

        End Try
    End Sub

    Private Sub button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button1.Click
        grabber = New Capture()
        grabber.QueryFrame()
        Timer1.Start()
        button1.Enabled = False
    End Sub

    Private Sub button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button2.Click
        Try
            
            ContTrain = ContTrain + 1
            gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)
            Dim facesDetected As MCvAvgComp()() = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, New Size(20, 20))
            For Each f As MCvAvgComp In facesDetected(0)
                TrainedFace = currentFrame.Copy(f.rect).Convert(Of Gray, Byte)()
                Exit For
            Next

            TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)
            trainingImages.Add(TrainedFace)
            labels.Add(textBox1.Text)

            imageBox1.Image = TrainedFace

            File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() & "%")

           
            For i As Integer = 1 To trainingImages.ToArray().Length
                trainingImages.ToArray()(i - 1).Save(Application.StartupPath + "/TrainedFaces/face" & i & ".bmp")
                File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()(i - 1) + "%")
            Next

            MessageBox.Show(textBox1.Text + "'s Face Registered", "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            textBox1.Text = ""
            imageBox1.Image = Nothing
        Catch
            MessageBox.Show("Enable the face detection first", "Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try

    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        label3.Text = "0"

        NamePersons.Add("")


       
        currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)

        gray = currentFrame.Convert(Of Gray, [Byte])()

        Dim facesDetected As MCvAvgComp()() = gray.DetectHaarCascade(face, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, New Size(20, 20))

        For Each f As MCvAvgComp In facesDetected(0)
            t = t + 1
            result = currentFrame.Copy(f.rect).Convert(Of Gray, Byte)().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)
            
            currentFrame.Draw(f.rect, New Bgr(Color.Red), 2)


            If trainingImages.ToArray().Length <> 0 Then
               
                Dim termCrit As New MCvTermCriteria(ContTrain, 0.001)

                Dim recognizer As New EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 3000, termCrit)

                name = recognizer.Recognize(result)


                currentFrame.Draw(name, font, New Point(f.rect.X - 2, f.rect.Y - 2), color:=New Bgr(Color.LightGreen))
            End If

            NamePersons(t - 1) = name
            NamePersons.Add("")

            label3.Text = facesDetected(0).Length.ToString()
        Next
        t = 0

        For nnn As Integer = 0 To facesDetected(0).Length - 1
            names = names + NamePersons(nnn) + ", "
        Next
       
        imageBoxFrameGrabber.Image = currentFrame
        label4.Text = names
        names = ""
        NamePersons.Clear()
    End Sub

End Class
