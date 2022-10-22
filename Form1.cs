using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
namespace WindowsFormsAppTest
{
    public partial class Form1 : Form
    {
        //Live view open or not
        bool bLiveViewRunning = false;
        List<VertexC4ubV3f> lAllVertices = new List<VertexC4ubV3f>();
        List<int> lAllTriangles = new List<int>();
        public Form1()
        {
            InitializeComponent();
        }

        private void hello(object sender, EventArgs e)
        {
            MessageBox.Show("hello");
        }

        private void OpenGLWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bLiveViewRunning = true;
            OpenGLWindow oOpenGLWindow = new OpenGLWindow();
            //FileStream F = new FileStream("sample.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
            VertexC4ubV3f m = new VertexC4ubV3f();
            m.X = 0;m.Y = 0; m.Z = 0;m.R = 255; m.G = 255; m.B = 0; m.A = 255;
            lAllVertices.Add(m);
            m.X = 0; m.Y = 1; m.Z = 0; m.R = 255; m.G = 0; m.B = 255; m.A = 255;
            lAllVertices.Add(m);
            m.X = 0; m.Y = 0; m.Z = 1; m.R = 0; m.G = 255; m.B = 255; m.A = 255;
            lAllVertices.Add(m);
            m.X = 1; m.Y = 0; m.Z = 0; m.R = 255; m.G = 255; m.B = 255; m.A = 255;
            lAllVertices.Add(m);
            oOpenGLWindow.vertices = lAllVertices;
            lAllTriangles.Add(0);
            lAllTriangles.Add(1);
            lAllTriangles.Add(2);
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
