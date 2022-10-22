using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Windows.Forms.Layout;
using System.Drawing;
using System.Diagnostics;

using OpenTK;
/*using OpenTK.Graphics.OpenGL;*/
using OpenTK.Graphics;
using OpenTK.Input;

enum ECameraMode
{
    CAMERA_NONE, CAMERA_TRACK, CAMERA_DOLLY, CAMERA_ORBIT
}

namespace WindowsFormsAppTest
{
    public class OpenGLWindow : GameWindow
    {
        int PointCount;
        int LineCount;
        int TriangleCount;

        VertexC4ubV3f[] VBO;
        float PointSize = 0.0f;
        ECameraMode CameraMode = ECameraMode.CAMERA_NONE;

        static float KEYBOARD_MOVE_SPEED = 0.01f;

        bool IsFullscreen = false;

        static float MOUSE_ORBIT_SPEED = 0.30f;     // 0 = SLOWEST, 1 = FASTEST
        static float MOUSE_DOLLY_SPEED = 0.2f;     // same as above...but much more sensitive
        static float MOUSE_TRACK_SPEED = 0.003f;    // same as above...but much more sensitive

        float g_heading;
        float g_pitch;
        float dx = 0.0f;
        float dy = 0.0f;

        byte brightnessModifier = 0;

        Vector2 MousePrevious = new Vector2();
        Vector2 MouseCurrent = new Vector2();
        float[] cameraPosition = new float[3];
        float[] targetPosition = new float[3];
        public List<VertexC4ubV3f> vertices = new List<VertexC4ubV3f>();
        //public List<byte> colors = new List<byte>();
        public List<int> triangles = new List<int>();

        DateTime tFPSUpdateTimer = DateTime.Now;
        int nTickCounter = 0;

        bool bDrawMarkings = false;

        // this struct is used for drawing
/*        struct VertexC4ubV3f
        {
            public byte R, G, B, A;
            public Vector3 Position;

            public static int SizeInBytes = 16;
        }*/

        uint VBOHandle;

        /// <summary>Creates a 800x600 window with the specified title.</summary>
        public OpenGLWindow()
            : base(800, 600, OpenTK.Graphics.GraphicsMode.Default, "LiveScan")
        {
            this.VSync = VSyncMode.Off;
            MouseUp += new EventHandler<MouseButtonEventArgs>(OnMouseButtonUp);
            MouseDown += new EventHandler<MouseButtonEventArgs>(OnMouseButtonDown);
            MouseMove += new EventHandler<MouseMoveEventArgs>(OnMouseMove);
            MouseWheel += new EventHandler<MouseWheelEventArgs>(OnMouseWheelChanged);

            KeyDown += new EventHandler<KeyboardKeyEventArgs>(OnKeyDown);

            cameraPosition[0] = 0;
            cameraPosition[1] = 0;
            cameraPosition[2] = 1.0f;
            targetPosition[0] = 0;
            targetPosition[1] = 0;
            targetPosition[2] = 0;
        }

        public void CloudUpdateTick()
        {
            nTickCounter++;
        }

        public void ToggleFullscreen()
        {
            if (IsFullscreen)
            {
                WindowBorder = WindowBorder.Resizable;
                WindowState = WindowState.Normal;
                ClientSize = new System.Drawing.Size(800, 600);
                CursorVisible = true;
            }
            else
            {
                CursorVisible = false;
                WindowBorder = WindowBorder.Hidden;
                WindowState = WindowState.Fullscreen;
            }
            IsFullscreen = !IsFullscreen;
        }

        void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {

            var keyboard = e.Keyboard;
            if (keyboard[Key.Escape])
            {
                Exit();
            }
            if (keyboard[Key.Plus])
            {
                PointSize += 0.1f;
                GL.PointSize(PointSize);
            }
            if (keyboard[Key.Minus])
            {
                if (PointSize != 0)
                    PointSize -= 0.1f;
                GL.PointSize(PointSize);
            }
            if (keyboard[Key.W])
                cameraPosition[2] -= KEYBOARD_MOVE_SPEED;
            if (keyboard[Key.A])
                cameraPosition[0] -= KEYBOARD_MOVE_SPEED;
            if (keyboard[Key.S])
                cameraPosition[2] += KEYBOARD_MOVE_SPEED;
            if (keyboard[Key.D])
                cameraPosition[0] += KEYBOARD_MOVE_SPEED;
            if (keyboard[Key.F])
                ToggleFullscreen();
            if (keyboard[Key.M])
                bDrawMarkings = !bDrawMarkings;
            if (keyboard[Key.O])
                brightnessModifier = (byte)Math.Max(0, brightnessModifier - 10);
            if (keyboard[Key.P])
                brightnessModifier = (byte)Math.Min(255, brightnessModifier + 10);
        }

        /// <summary>Load resources here.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Version version = new Version(GL.GetString(StringName.Version).Substring(0, 3));
            Version target = new Version(1, 5);
            if (version < target)
            {
                throw new NotSupportedException(String.Format(
                    "OpenGL {0} is required (you only have {1}).", target, version));
            }

            GL.ClearColor(.1f, 0f, .1f, 0f);
            GL.Enable(EnableCap.DepthTest);

            // Setup parameters for Points
            GL.PointSize(PointSize);
            GL.Enable(EnableCap.PointSmooth);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

            // Setup VBO state
            GL.EnableClientState(EnableCap.ColorArray);
            GL.EnableClientState(EnableCap.VertexArray);

            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            GL.ClearColor(Color.Black);
            PointCount = 0;
            LineCount = 0;
            TriangleCount = 0;
            VBO = new VertexC4ubV3f[PointCount + 2 * LineCount + 3 * TriangleCount];
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.DeleteBuffers(1, ref VBOHandle);
        }

        /// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
        /// </summary>
        /// <param name="e">Contains information on the new Width and Size of the GameWindow.</param>
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            GL.MatrixMode(MatrixMode.Projection);
            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Width / (float)Height, 0.1f, 50.0f);
            GL.LoadMatrix(ref p);

            GL.MatrixMode(MatrixMode.Modelview);
            Matrix4 mv = Matrix4.LookAt(Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);
            GL.LoadMatrix(ref mv);
        }

        void OnMouseWheelChanged(object sender, MouseWheelEventArgs e)
        {
            dy = e.Delta * MOUSE_DOLLY_SPEED;

            cameraPosition[2] -= dy;

            //if (cameraPosition[2] < 0)
            //    cameraPosition[2] = 0;

        }

        void OnMouseMove(object sender, MouseMoveEventArgs e)
        {
            MouseCurrent.X = e.Mouse.X;
            MouseCurrent.Y = e.Mouse.Y;

            // Now use mouse_delta to move the camera

            switch (CameraMode)
            {
                case ECameraMode.CAMERA_TRACK:
                    dx = MouseCurrent.X - MousePrevious.X;
                    dx *= MOUSE_TRACK_SPEED;

                    dy = MouseCurrent.Y - MousePrevious.Y;
                    dy *= MOUSE_TRACK_SPEED;

                    cameraPosition[0] -= dx;
                    cameraPosition[1] += dy;

                    //targetPosition[0] -= dx;
                    //targetPosition[1] += dy;

                    break;

                case ECameraMode.CAMERA_DOLLY:
                    dy = MouseCurrent.Y - MousePrevious.Y;
                    dy *= MOUSE_DOLLY_SPEED;

                    cameraPosition[2] -= dy;

                    //    if (cameraPosition[2] < 0)
                    //       cameraPosition[2] = 0;

                    break;

                case ECameraMode.CAMERA_ORBIT:
                    dx = MouseCurrent.X - MousePrevious.X;
                    dx *= MOUSE_ORBIT_SPEED;

                    dy = MouseCurrent.Y - MousePrevious.Y;
                    dy *= MOUSE_ORBIT_SPEED;

                    g_heading += dx;
                    g_pitch += dy;

                    break;
            }
            MousePrevious.X = MouseCurrent.X;
            MousePrevious.Y = MouseCurrent.Y;
        }

        void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            CameraMode = ECameraMode.CAMERA_NONE;
        }

        void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    CameraMode = ECameraMode.CAMERA_ORBIT;
                    break;
                case MouseButton.Middle:
                    CameraMode = ECameraMode.CAMERA_DOLLY;
                    break;
                case MouseButton.Right:
                    CameraMode = ECameraMode.CAMERA_TRACK;
                    break;
            }
            MousePrevious.X = Mouse.X;
            MousePrevious.Y = Mouse.Y;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if ((DateTime.Now - tFPSUpdateTimer).Seconds >= 1)
            {
                double FPS = nTickCounter / (DateTime.Now - tFPSUpdateTimer).TotalSeconds;
                this.Title = "FPS: " + string.Format("{0:F}", FPS);

                tFPSUpdateTimer = DateTime.Now;
                nTickCounter = 0;
            }


            lock (vertices)
            {

                    PointCount = vertices.Count;
                    LineCount = 0;
                    TriangleCount = triangles.Count / 3;
                if (bDrawMarkings)
                    {
                        //bounding box
                        LineCount += 12;
                    }

                    VBO = new VertexC4ubV3f[PointCount + 2 * LineCount + 3 * TriangleCount];
                     VertexC4ubV3f[] verticesArray = vertices.ToArray();
                     if (PointCount > 0)
                         Array.Copy(verticesArray, VBO, PointCount);
                AddTriangles(PointCount + 2 * LineCount);
            }
        }

        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PushMatrix();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.Translate(-cameraPosition[0], -cameraPosition[1], -cameraPosition[2]);
            GL.Rotate(g_pitch, 1.0f, 0.0f, 0.0f);
            GL.Rotate(g_heading, 0.0f, 1.0f, 0.0f);

            // Tell OpenGL to discard old VBO when done drawing it and reserve memory _now_ for a new buffer.
            // without this, GL would wait until draw operations on old VBO are complete before writing to it
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * (PointCount + 2 * LineCount + 3 * TriangleCount)), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * (PointCount + 2 * LineCount + 3 * TriangleCount)), VBO, BufferUsageHint.StreamDraw);
            GL.DrawArrays(BeginMode.Points, 0, PointCount);
            GL.DrawArrays(BeginMode.Lines, PointCount, 2 * LineCount);
            GL.DrawArrays(BeginMode.Triangles, PointCount + 2 * LineCount, 3 * TriangleCount);

            GL.PopMatrix();

            SwapBuffers();
        }

        private int AddBoundingBox(int startIdx)
        {
            int nLinesBeingAdded = 12;
            //2 points per line
            int nPointsToAdd = 2 * nLinesBeingAdded;

            for (int i = startIdx; i < startIdx + nPointsToAdd; i++)
            {
                VBO[i].R = 255;
                VBO[i].G = 255;
                VBO[i].B = 0;
                VBO[i].A = 0;
            }

            int n = 0;


            return nLinesBeingAdded;
        }

        private void AddTriangles(int startIdx)
        {
            VertexC4ubV3f[] verticesArray = vertices.ToArray();
            int endIdx = startIdx + TriangleCount * 3;
            if (endIdx - startIdx == 0)
                return;

            for (int i = startIdx; i < endIdx; i++)
            {
                int v = triangles[i - startIdx];
                VBO[i] = verticesArray[v];
            }

        }

        private void AddLine(int startIdx, float x0, float y0, float z0,
            float x1, float y1, float z1)
        {
            VBO[startIdx].X = x0;
            VBO[startIdx].Y = y0;
            VBO[startIdx].Z = z0;

            VBO[startIdx + 1].X = x1;
            VBO[startIdx + 1].Y = y1;
            VBO[startIdx + 1].Z = z1;
        }
    }
}

