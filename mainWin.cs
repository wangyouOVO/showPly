using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
namespace WindowsFormsAppTest
{
    public partial class mainWin : Form
    {
        //Live view open or not
        bool bLiveViewRunning = false;
        List<VertexC4ubV3f> lAllVertices = new List<VertexC4ubV3f>();
        List<int> lAllTriangles = new List<int>();
        string fileName;
        public mainWin()
        {
            InitializeComponent();
        }

        private void ReadFilePro()
        {   //选择文件文件对话框
            lAllVertices.Clear();
            lAllTriangles.Clear();
            OpenFileDialog dialog = new OpenFileDialog();
            //是否支持多个文件的打开？
            dialog.Multiselect = false;
            //标题
            dialog.Title = "请选择模型";
            //文件类型
            dialog.Filter = "ply(*.*)|*.*";//或"图片(*.jpg;*.bmp)|*.jpg;*.bmp"
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //获取文件路径
                fileName = dialog.FileName;
            }
            //fileName = @"F:\Allproject\CsLearn\WindowsFormsAppTest\ply\mesh3.ply";
            List<VertexC4ubV3f> list = new List<VertexC4ubV3f>();
            var reader = new StreamReader(fileName);
            int vertexNum = 0;
            int faceNum = 0;
            string buf = reader.ReadLine();
            var line = buf.Split(' ');
            while(line[0] != "end_header")
            {
                if(line[0] == "element")
                {
                    if(line[1] == "vertex")
                    {
                        vertexNum = int.Parse(line[2]);
                    }else if (line[1] == "face")
                    {
                        faceNum = int.Parse(line[2]);
                    }

                }
                buf = reader.ReadLine();
                line = buf.Split(' ');
            }
            for(int i = 0; i < vertexNum; i++)
            {
                buf = reader.ReadLine();
                line = buf.Split(' ');
                if(line.Length == 3)
                {
                    VertexC4ubV3f newVertex = new VertexC4ubV3f();
                    newVertex.X = float.Parse(line[0]);
                    newVertex.Y = float.Parse(line[1]);
                    newVertex.Z = float.Parse(line[2]);
                    newVertex.R = 200;
                    newVertex.G = 200;
                    newVertex.B = 200;
                    newVertex.A = 255;
                    lAllVertices.Add(newVertex);
                }
                else if(line.Length == 6 || line.Length == 7)
                {
                    VertexC4ubV3f newVertex = new VertexC4ubV3f();
                    newVertex.X = float.Parse(line[0]);
                    newVertex.Y = float.Parse(line[1]);
                    newVertex.Z = float.Parse(line[2]);
                    newVertex.R = byte.Parse(line[3]);
                    newVertex.G = byte.Parse(line[4]);
                    newVertex.B = byte.Parse(line[5]);
                    newVertex.A = 255;
                    lAllVertices.Add(newVertex);

                }
                else if(line.Length == 9)
                {
                    VertexC4ubV3f newVertex = new VertexC4ubV3f();
                    newVertex.X = float.Parse(line[0]);
                    newVertex.Y = float.Parse(line[1]);
                    newVertex.Z = float.Parse(line[2]);
                    newVertex.R = byte.Parse(line[6]);
                    newVertex.G = byte.Parse(line[7]);
                    newVertex.B = byte.Parse(line[8]);
                    newVertex.A = 255;
                    lAllVertices.Add(newVertex);

                }
                else
                {
                    MessageBox.Show("无法解析该ply!");
                    break;
                }
            }
            for (int i = 0; i < faceNum; i++)
            {
                buf = reader.ReadLine();
                line = buf.Split(' ');
                lAllTriangles.Add(int.Parse(line[1]));
                lAllTriangles.Add(int.Parse(line[2]));
                lAllTriangles.Add(int.Parse(line[3]));
            }
            Console.WriteLine("read ply successfully!");
            reader.Close();
        }


        private void hello(object sender, EventArgs e)
        {
            ReadFilePro();
        }

        private void OpenGLWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bLiveViewRunning = true;
            OpenGLWindow oOpenGLWindow = new OpenGLWindow();
            oOpenGLWindow.vertices = lAllVertices;
            oOpenGLWindow.triangles = lAllTriangles;
            oOpenGLWindow.Run();
        }

        private void OpenGLWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bLiveViewRunning = false;
/*            updateWorker.CancelAsync();*/
        }
        private void button2_Click(object sender, EventArgs e)
        {   
            //Opens the live view window if it is not open yet.
            if (!OpenGLWorker.IsBusy)
                OpenGLWorker.RunWorkerAsync();
        }


    }
}
