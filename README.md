# 关于使用OpenTK渲染ply的mesh模型

1. 首先关注ply文件的格式：
```sh
ply
format ascii 1.0                        #ascii码储存
comment Created by Open3D
element vertex 356078                   #共356078个顶点
property double x                       #x,y,z是点坐标
property double y
property double z
property double nx                      #nx,ny,nz是法向量
property double ny
property double nz
property uchar red                      #颜色
property uchar green
property uchar blue
element face 715554
property list uchar uint vertex_indices
end_header                              #文件头结束
-1.59041 1.45289 -6.10703 0.0852282 0.162595 -0.267907 151 146 149   #点信息，包括坐标，法向量，颜色
-1.67056 1.66606 -6.10703 0.0852282 0.162595 -0.267907 151 146 149
....
-1.93451 1.66606 -5.87702 0.0852282 0.162595 -0.267907 151 146 149
-1.82042 1.85091 -5.87702 0.0852282 0.162595 -0.267907 151 146 149
3 129172 128056 127647                #面信息，“3”表示面有三个顶点，后面三个整数表示对应的顶点标号！
3 129245 128056 129172
...
3 323690 323695 324324
3 324329 324324 323695
```
如果某些信息没有，也属于正常现象，比如法向量，颜色信息
<br></br>

2. OpenTK中点的数据结构
```sh
public struct VertexC4ubV3f
    {
        public byte R, G, B, A;
        public float X, Y, Z;
        public static int SizeInBytes = 16;
    }
```
最后需要一个 VertexC4ubV3f 数组作为最终渲染对象
```sh
VertexC4ubV3f[] VBO;
```
最终渲染函数为：
```sh
GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * (PointCount + 2 * LineCount + 3 * TriangleCount)), IntPtr.Zero, BufferUsageHint.StreamDraw); #清空Buffer
GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * (PointCount + 2 * LineCount + 3 * TriangleCount)), VBO, BufferUsageHint.StreamDraw); #将所有VBO储存到Buffer中
GL.DrawArrays(BeginMode.Points, 0, PointCount); #绘制点
GL.DrawArrays(BeginMode.Lines, PointCount, 2 * LineCount); #绘制线
GL.DrawArrays(BeginMode.Triangles, PointCount + 2 * LineCount, 3 * TriangleCount); #绘制三角形
```
## 注意！！
关于上述函数的一些解释：<br>
1.我们知道，渲染"点"仅仅需要一个点，渲染“线”需要两个点，渲染“三角面片”需要三个点，最终所有的点都被存在VBO数组中！也就是说，如果需要渲染的数据有n个点，m个线，k个三角面，那么最终VBO数组中一共存了n+2m+3k个元素，其中后面的2m+3k个点就是从前面n个点中选出来的，至于怎么选的，就是根据ply文件中面的信息提取出来的，比如如果一个面由第3,9,11个点为顶点渲染出来，那么就要把这三个点的数据另外取一份，接在VBO数组后面。<br>
2.最后我们可以得到这样一个数组：<br>
VBO=[v1,v2,...vn,l1,l2,...l2m,f1,f2...f3k],共n+2m+3k个结构为 VertexC4ubV3f 的点元素，然后用 GL.BufferData()函数 (如上方例子)将其存到buffer中，再通过GL.DrawArrays()函数渲染。其中第一个参数为渲染类型，第二和第三个参数为以该类型渲染的点的起止位置。